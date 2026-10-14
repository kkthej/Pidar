using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Pidar.Services.Assistant
{
    /// <summary>
    /// A chat model with tool use, spoken to in the Anthropic Messages API format:
    /// the request is { system, messages, tools, max_tokens } and the response has
    /// content blocks ("text" / "tool_use"), stop_reason and usage.
    /// Both the real model and the offline test model use this format, so the assistant's tool loop
    /// (ContributionAssistant) is exactly the same in test mode and in production.
    /// </summary>
    public interface IAssistantModel
    {
        /// <summary>Model name stored with each reply ("mock" in test mode).</summary>
        string Name { get; }

        Task<JsonObject> SendAsync(JsonObject request, CancellationToken ct);
    }

    /// <summary>Raised when the model cannot be reached or answers with an error.</summary>
    public sealed class AssistantUnavailableException : Exception
    {
        public AssistantUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    }

    // =====================================================================================
    // Claude through the Anthropic Messages API (POST /v1/messages)
    // =====================================================================================
    public sealed class AnthropicAssistantModel : IAssistantModel
    {
        private const string ApiVersion = "2023-06-01";

        private readonly HttpClient _http;
        private readonly AssistantSettings _settings;
        private readonly ILogger<AnthropicAssistantModel> _logger;

        public AnthropicAssistantModel(HttpClient http, AssistantSettings settings, ILogger<AnthropicAssistantModel> logger)
        {
            _http = http;
            _settings = settings;
            _logger = logger;
        }

        public string Name => _settings.Options.Model;

        public async Task<JsonObject> SendAsync(JsonObject request, CancellationToken ct)
        {
            request["model"] = _settings.Options.Model;
            request["max_tokens"] = _settings.Options.MaxTokens;

            using var msg = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
            {
                Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            msg.Headers.Add("x-api-key", _settings.Options.ApiKey);
            msg.Headers.Add("anthropic-version", ApiVersion);
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(msg, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Assistant: the Anthropic API could not be reached");
                throw new AssistantUnavailableException("The AI service could not be reached.", ex);
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    // The body holds the error type and message, never the API key
                    _logger.LogWarning("Assistant: Anthropic API returned {Status}: {Body}", (int)response.StatusCode,
                        body.Length > 500 ? body[..500] : body);
                    throw new AssistantUnavailableException((int)response.StatusCode == 429
                        ? "The AI service is busy right now."
                        : "The AI service returned an error.");
                }

                try
                {
                    return JsonNode.Parse(body)?.AsObject() ?? throw new JsonException("empty response");
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Assistant: unreadable response from the Anthropic API");
                    throw new AssistantUnavailableException("The AI service returned an unreadable answer.", ex);
                }
            }
        }
    }

    // =====================================================================================
    // Test mode: no AI. Recognises a few common facts with regular expressions and proposes them through
    // the same propose_answers tool a real model would use, so suggestions, Accept, the amber marks,
    // autosave and the history can all be tried locally without an API key.
    // =====================================================================================
    public sealed class MockAssistantModel : IAssistantModel
    {
        public string Name => "mock";

        public Task<JsonObject> SendAsync(JsonObject request, CancellationToken ct)
        {
            var messages = request["messages"]?.AsArray() ?? new JsonArray();
            var last = messages.Count > 0 ? messages[^1]?.AsObject() : null;

            // Second round: the tool ran, now say what happened
            if (last?["content"] is JsonArray blocks && blocks.Any(b => (string?)b?["type"] == "tool_result"))
            {
                var result = blocks.First(b => (string?)b?["type"] == "tool_result");
                var accepted = 0;
                var rejected = new List<string>();
                try
                {
                    var parsed = JsonNode.Parse((string?)result?["content"] ?? "{}");
                    accepted = parsed?["accepted"]?.AsArray().Count ?? 0;
                    rejected = parsed?["rejected"]?.AsArray()
                        .Select(r => $"{(string?)r?["path"]}: {(string?)r?["reason"]}").ToList() ?? new();
                }
                catch (JsonException) { /* keep defaults */ }

                var sb = new StringBuilder();
                sb.Append("(Test mode — no AI model is connected.) ");
                sb.Append(accepted == 1 ? "I found 1 answer. " : $"I found {accepted} answers. ");
                sb.Append("Check the suggestions below and click **Accept** to put them in the form.");
                if (rejected.Count > 0) sb.Append("\n\nI could not use: " + string.Join("; ", rejected));

                // The message that started this turn (the one before the tool call), to mention unread files
                var started = messages.Count >= 3 ? messages[^3]?.AsObject() : null;
                var (_, _, scannedNow, imagesNow) = ReadAttachments(started);
                var notRead = new List<string>();
                if (imagesNow > 0) notRead.Add(imagesNow == 1 ? "1 picture" : $"{imagesNow} pictures");
                if (scannedNow > 0) notRead.Add(scannedNow == 1 ? "1 scanned PDF" : $"{scannedNow} scanned PDFs");
                if (notRead.Count > 0)
                    sb.Append($"\n\nI could not read {string.Join(" and ", notRead)}: reading pictures, handwriting and scans needs the real AI model (an API key).");
                return Task.FromResult(TextResponse(sb.ToString()));
            }

            var text = LastUserText(last);

            // Attached files: test mode reads the text layer of PDFs; pictures and scans need a real model
            var (pdfText, pdfCount, scannedPdfs, images) = ReadAttachments(last);
            if (pdfText.Length > 0) text += "\n\n" + pdfText;
            var unreadable = new List<string>();
            if (images > 0) unreadable.Add(images == 1 ? "1 picture" : $"{images} pictures");
            if (scannedPdfs > 0) unreadable.Add(scannedPdfs == 1 ? "1 scanned PDF" : $"{scannedPdfs} scanned PDFs");
            var unreadableNote = unreadable.Count == 0 ? "" :
                $" I could not read {string.Join(" and ", unreadable)}: reading pictures, handwriting and scans needs the real AI model (an API key).";

            var proposals = Extract(text);
            if (proposals.Count == 0)
            {
                return Task.FromResult(TextResponse(
                    "(Test mode — no AI model is connected.)" + unreadableNote +
                    (pdfCount > scannedPdfs ? " I read the text of your PDF but found none of the simple facts I recognise in test mode." : "") +
                    " Tell me about your animals and imaging, for example: " +
                    "\"We used 24 female C57BL/6 mice, 8 weeks old, in 3 groups, imaged with MRI of the brain\", " +
                    "or paste your methods section. In test mode I only recognise a few simple facts."));
            }

            var input = new JsonObject
            {
                ["proposals"] = new JsonArray(proposals.Select(p => (JsonNode)new JsonObject
                {
                    ["path"] = p.Path,
                    ["value"] = p.Value,
                    ["reason"] = p.Reason,
                }).ToArray())
            };
            var response = new JsonObject
            {
                ["id"] = "mock",
                ["type"] = "message",
                ["role"] = "assistant",
                ["stop_reason"] = "tool_use",
                ["content"] = new JsonArray(
                    new JsonObject { ["type"] = "text", ["text"] = "Let me fill in what you told me." },
                    new JsonObject
                    {
                        ["type"] = "tool_use",
                        ["id"] = "toolu_mock_" + Guid.NewGuid().ToString("N")[..12],
                        ["name"] = "propose_answers",
                        ["input"] = input,
                    }),
                ["usage"] = new JsonObject { ["input_tokens"] = 0, ["output_tokens"] = 0 },
            };
            return Task.FromResult(response);
        }

        private static JsonObject TextResponse(string text) => new()
        {
            ["id"] = "mock",
            ["type"] = "message",
            ["role"] = "assistant",
            ["stop_reason"] = "end_turn",
            ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text }),
            ["usage"] = new JsonObject { ["input_tokens"] = 0, ["output_tokens"] = 0 },
        };

        /// <summary>Text of the attached PDFs (text layer only) and how many files could not be read.</summary>
        private static (string Text, int Pdfs, int ScannedPdfs, int Images) ReadAttachments(JsonObject? message)
        {
            if (message?["content"] is not JsonArray blocks) return ("", 0, 0, 0);
            var texts = new List<string>();
            int pdfs = 0, scanned = 0, images = 0;
            foreach (var b in blocks)
            {
                switch ((string?)b?["type"])
                {
                    case "document":
                        pdfs++;
                        var data = (string?)b?["source"]?["data"];
                        var t = data == null ? "" : AssistantAttachments.ExtractPdfText(Convert.FromBase64String(data), maxPages: 100);
                        if (t.Length == 0) scanned++;
                        else texts.Add(t);
                        break;
                    case "image":
                        images++;
                        break;
                }
            }
            return (string.Join("\n\n", texts), pdfs, scanned, images);
        }

        private static string LastUserText(JsonObject? message)
        {
            var content = message?["content"];
            if (content is JsonValue v) return v.GetValue<string>();
            if (content is JsonArray a)
                return string.Join("\n", a.Where(b => (string?)b?["type"] == "text").Select(b => (string?)b?["text"] ?? ""));
            return "";
        }

        private sealed record Found(string Path, string Value, string Reason);

        private static readonly Dictionary<string, string> NumberWords = new(StringComparer.OrdinalIgnoreCase)
        {
            ["one"] = "1", ["two"] = "2", ["three"] = "3", ["four"] = "4", ["five"] = "5", ["six"] = "6",
            ["seven"] = "7", ["eight"] = "8", ["nine"] = "9", ["ten"] = "10",
        };

        private static List<Found> Extract(string text)
        {
            var found = new List<Found>();
            if (string.IsNullOrWhiteSpace(text)) return found;

            void Add(string path, string value, string reason) => found.Add(new Found(path, value, reason));

            // Species
            if (Rx(@"\b(mice|mouse|murine)\b").IsMatch(text)) Add("InVivo.Species", "Mouse", "You mentioned mice.");
            else if (Rx(@"\brats?\b").IsMatch(text)) Add("InVivo.Species", "Rat", "You mentioned rats.");
            else if (Rx(@"\bzebrafish\b").IsMatch(text)) Add("InVivo.Species", "Zebrafish", "You mentioned zebrafish.");

            // Strain and the immune status it implies
            var strain = Rx(@"\b(C57BL/?6[JN]?|BALB/?c|NSG|NOD[- ]?scid|athymic nude|nude|Sprague[- ]Dawley|Wistar|CD-?1|FVB)\b").Match(text);
            if (strain.Success)
            {
                var s = strain.Value;
                Add("InVivo.Strain", s, $"You wrote \"{s}\".");
                var immunodeficient = Rx(@"NSG|scid|nude").IsMatch(s);
                Add("InVivo.ImmuneStatus", immunodeficient ? "immunodeficient" : "immunocompetent",
                    $"{s} is usually {(immunodeficient ? "immunodeficient" : "immunocompetent")}; please confirm.");
            }

            // Sex
            var female = Rx(@"\bfemales?\b").IsMatch(text);
            var male = Rx(@"\bmales?\b").IsMatch(text);
            if (female || male)
                Add("InVivo.Sex", female && male ? "male, female" : female ? "female" : "male", "From the sex you mentioned.");

            // Age and weight
            var age = Rx(@"\b(\d{1,3})[- ]?(weeks?|wk|days?|months?)[- ]old\b").Match(text);
            if (age.Success) Add("InVivo.Age", $"{age.Groups[1].Value} {age.Groups[2].Value.TrimEnd('s')}s old".Replace("wks", "weeks"), "From the age you mentioned.");
            var weight = Rx(@"\b(\d{1,3}(?:\s*[-–]\s*\d{1,3})?)\s*(g|grams)\b").Match(text);
            if (weight.Success) Add("InVivo.Weight", weight.Groups[1].Value.Replace(" ", "") + " g", "From the weight you mentioned.");

            // Numbers of animals and groups
            // "24 female C57BL/6 mice": up to three words (strain names included) between number and animals
            var n = Rx(@"\b(\d{1,4})\s+(?:[^\s,;.()]+\s+){0,3}(mice|rats|animals|zebrafish)\b").Match(text);
            var nEq = Rx(@"\bn\s*=\s*(\d{1,4})\b").Match(text);
            if (n.Success) Add("InVivo.OverallSampleSize", n.Groups[1].Value, $"You wrote \"{n.Value}\".");
            else if (nEq.Success) Add("InVivo.OverallSampleSize", nEq.Groups[1].Value, $"You wrote \"{nEq.Value}\".");
            var groups = Rx(@"\b(\d{1,2}|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?:\w+\s+)?groups\b").Match(text);
            if (groups.Success)
            {
                var g = groups.Groups[1].Value;
                Add("InVivo.NumberOfGroups", NumberWords.TryGetValue(g, out var d) ? d : g, $"You wrote \"{groups.Value}\".");
            }

            // Organ
            var organ = Rx(@"\b(brain|liver|heart|lungs?|kidneys?|pancreas|prostate|mammary gland|breast|spleen|bone)\b").Match(text);
            if (organ.Success) Add("InVivo.OrganOrTissue", organ.Value.ToLowerInvariant(), $"You mentioned the {organ.Value.ToLowerInvariant()}.");

            // Disease model
            if (Rx(@"\b(tumou?rs?|cancer|carcinoma|glioma|glioblastoma|melanoma|lymphoma|xenograft)\b").IsMatch(text))
            {
                Add("InVivo.AnimalCondition", "disease model", "You mentioned a tumour model.");
                Add("InVivo.DiseaseCategory", "Disease of cellular proliferation", "Tumour models belong to this category.");
            }
            else if (Rx(@"\bhealthy\b").IsMatch(text))
            {
                Add("InVivo.AnimalCondition", "healthy animals", "You mentioned healthy animals.");
            }

            // Imaging modality
            var modalities = new List<string>();
            if (Rx(@"\bPET[/ -]?CT\b").IsMatch(text)) modalities.Add("PET/CT");
            else
            {
                if (Rx(@"\bPET\b|positron emission").IsMatch(text)) modalities.Add("PET");
                if (Rx(@"\b(micro-?)?CT\b|computed tomography").IsMatch(text)) modalities.Add("CT");
            }
            if (Rx(@"\bMRI\b|\bMR imaging\b|magnetic resonance").IsMatch(text)) modalities.Add("MRI");
            if (Rx(@"\bultrasound\b|\bultrasonography\b").IsMatch(text)) modalities.Add("US");
            if (Rx(@"\bphotoacoustic|optoacoustic").IsMatch(text)) modalities.Add("OA");
            else if (Rx(@"\boptical imaging|bioluminescence|fluorescence imaging").IsMatch(text)) modalities.Add("OI");
            if (modalities.Count > 0)
                Add("StudyComponent.ImagingModality", string.Join(", ", modalities), "From the imaging technique you mentioned.");

            return found;
        }

        private static Regex Rx(string pattern) => new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
