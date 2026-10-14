using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Helpers;
using Pidar.Models;
using Pidar.Models.Submissions;
using OntologyEntity = Pidar.Models.Ontology.Ontology;

namespace Pidar.Services.Submissions
{
    /// <summary>One problem found when checking a submission before it is sent.</summary>
    public sealed record SubmissionIssue(string Path, int StepIndex, string StepTitle, string Label, string Message);

    /// <summary>How far a step is filled in: required (visible) fields answered / total.</summary>
    public sealed record StepProgress(int Filled, int Required)
    {
        public bool Complete => Filled >= Required;
    }

    /// <summary>
    /// Everything the Contribute pages do with a <see cref="DatasetSubmission"/>:
    /// reading/saving the answers, checking them, and turning an approved submission into a Dataset.
    /// </summary>
    public sealed class SubmissionService
    {
        private readonly PidarDbContext _db;
        private readonly OntologyIndexService _ontologyIndex;
        private readonly SubmissionHistory _history;
        private readonly ILogger<SubmissionService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        /// <summary>
        /// Hidden form value posted by the wizard: JSON { "Section.Property": "assistant" | "import" } for answers
        /// that were filled by the assistant or the Excel import and not changed by hand since.
        /// Only used to label the history; it never changes what is stored.
        /// </summary>
        public const string SourcesKey = "_sources";

        public SubmissionService(PidarDbContext db, OntologyIndexService ontologyIndex, SubmissionHistory history, ILogger<SubmissionService> logger)
        {
            _db = db;
            _ontologyIndex = ontologyIndex;
            _history = history;
            _logger = logger;
        }

        // ============================================================ answers (payload)

        public static Dictionary<string, string> ReadAnswers(DatasetSubmission s)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(s.PayloadJson ?? "{}", JsonOptions)
                       ?? new Dictionary<string, string>();
            }
            catch (JsonException)
            {
                return new Dictionary<string, string>();
            }
        }

        private static void WriteAnswers(DatasetSubmission s, Dictionary<string, string> answers)
        {
            // empty answers are not stored, so the JSON only holds what was filled in
            var clean = answers.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                               .ToDictionary(kv => kv.Key, kv => kv.Value);
            s.PayloadJson = JsonSerializer.Serialize(clean, JsonOptions);
        }

        /// <summary>A short name for lists: the study name, or "Untitled dataset".</summary>
        public static string Title(DatasetSubmission s)
        {
            var a = ReadAnswers(s);
            return a.TryGetValue("StudyDesign.StudyDescription", out var t) && !string.IsNullOrWhiteSpace(t)
                ? (t.Length > 120 ? t[..117] + "…" : t)
                : "Untitled dataset";
        }

        // ============================================================ create / save

        public async Task<DatasetSubmission> CreateDraftAsync(string ownerUserId, string ownerEmail, DatasetSubmission? copyFrom = null)
        {
            var now = DateTime.UtcNow;
            var s = new DatasetSubmission
            {
                OwnerUserId = ownerUserId,
                OwnerEmail = ownerEmail,
                Status = SubmissionStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now,
            };

            Dictionary<string, string> answers;
            if (copyFrom != null)
            {
                // "Start from a previous submission": same lab, PI, funding and access conditions are common.
                // The study name is marked as a copy so the two are not confused.
                answers = ReadAnswers(copyFrom);
                answers.Remove(ContributionForm.CommentKey);
                if (answers.TryGetValue("StudyDesign.StudyDescription", out var name) && !string.IsNullOrWhiteSpace(name))
                    answers["StudyDesign.StudyDescription"] = "Copy of " + name;
            }
            else
            {
                answers = new Dictionary<string, string>
                {
                    ["DatasetInfo.ContactPerson"] = ownerEmail,
                    ["StudyDesign.StudyType"] = "preclinical",
                };
            }
            WriteAnswers(s, answers);

            _db.DatasetSubmissions.Add(s);
            await _db.SaveChangesAsync();

            // the id exists only now
            await _history.LogEventAsync(s.Id, new Actor(ownerUserId, ownerEmail), SubmissionChangeKind.Created,
                copyFrom != null ? $"copy of submission {copyFrom.Id}" : null);
            return s;
        }

        /// <summary>
        /// Stores the posted answers. Only names of the form are read (anything else posted is ignored),
        /// values are trimmed and cut to <see cref="ContributionForm.MaxValueLength"/>.
        /// A field that is posted empty is cleared; a field that is not posted is left as it was.
        /// Every changed answer is recorded in the history with <paramref name="actor"/> and its source.
        /// </summary>
        public async Task SaveAsync(DatasetSubmission s, IFormCollection form, int? step, Actor actor)
        {
            var answers = ReadAnswers(s);
            var before = new Dictionary<string, string>(answers, StringComparer.Ordinal);

            foreach (var path in ContributionForm.FieldsByPath.Keys.Append(ContributionForm.CommentKey))
            {
                if (!form.TryGetValue(path, out var posted)) continue;
                var value = Clean(posted.ToString());
                if (value.Length == 0) answers.Remove(path);
                else answers[path] = value;
            }

            WriteAnswers(s, answers);
            if (step is >= 0 && step <= ContributionForm.Steps.Count) s.CurrentStep = step.Value;
            s.UpdatedAt = DateTime.UtcNow;

            await _history.RecordEditsAsync(s.Id, actor, before, ReadAnswers(s), ReadSources(form));
            await _db.SaveChangesAsync();
        }

        /// <summary>The posted <see cref="SourcesKey"/>: only known fields and the sources a browser may claim.</summary>
        private static Dictionary<string, string> ReadSources(IFormCollection form)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!form.TryGetValue(SourcesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return result;
            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(raw.ToString(), JsonOptions);
                if (map == null) return result;
                foreach (var (path, source) in map)
                {
                    if (ContributionForm.FieldsByPath.ContainsKey(path) && ValueSource.IsClientClaimable(source))
                        result[path] = source;
                }
            }
            catch (JsonException)
            {
                // a broken value only loses the labels, never the answers
            }
            return result;
        }

        private static string Clean(string? value)
        {
            var v = (value ?? "").Replace("\r\n", "\n").Trim();
            return v.Length > ContributionForm.MaxValueLength ? v[..ContributionForm.MaxValueLength] : v;
        }

        // ============================================================ checking

        private static readonly Regex OrcidPattern = new(@"^(https?://orcid\.org/)?\d{4}-\d{4}-\d{4}-\d{3}[\dX]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex YearPattern = new(@"^(19|20)\d{2}$", RegexOptions.Compiled);
        private static readonly Regex NumberPattern = new(@"^\d+$", RegexOptions.Compiled);

        /// <summary>All problems that stop a submission from being sent: missing required answers and badly formatted values.</summary>
        public static List<SubmissionIssue> Validate(IReadOnlyDictionary<string, string> answers)
        {
            var issues = new List<SubmissionIssue>();
            for (var i = 0; i < ContributionForm.Steps.Count; i++)
            {
                var step = ContributionForm.Steps[i];
                foreach (var f in step.Fields)
                {
                    if (!ContributionForm.IsVisible(f, answers)) continue;
                    answers.TryGetValue(f.Path, out var raw);
                    var v = (raw ?? "").Trim();

                    if (v.Length == 0)
                    {
                        if (f.Required) issues.Add(new(f.Path, i, step.Title, f.Label, "This answer is required."));
                        continue;
                    }

                    var problem = FormatProblem(f.Kind, v);
                    if (problem != null) issues.Add(new(f.Path, i, step.Title, f.Label, problem));
                }
            }
            return issues;
        }

        private static string? FormatProblem(FieldKind kind, string v)
        {
            // "N/A" is accepted everywhere, as the guidelines allow for metadata that does not apply
            if (v.Equals("N/A", StringComparison.OrdinalIgnoreCase)) return null;
            switch (kind)
            {
                case FieldKind.Email:
                    return MailAddress.TryCreate(v, out _) ? null : "Please enter a valid email address.";
                case FieldKind.Orcid:
                    return OrcidPattern.IsMatch(v) ? null : "An ORCID iD looks like 0000-0002-1825-0097.";
                case FieldKind.Year:
                    return YearPattern.IsMatch(v) ? null : "Please enter a four-digit year.";
                case FieldKind.Number:
                    return NumberPattern.IsMatch(v) ? null : "Please enter a whole number.";
                case FieldKind.Url:
                    return Uri.TryCreate(v, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps)
                        ? null : "Please enter a full link starting with https://";
                default:
                    return null;
            }
        }

        /// <summary>Per step: how many of the required (visible) questions are answered.</summary>
        public static List<StepProgress> Progress(IReadOnlyDictionary<string, string> answers)
        {
            var list = new List<StepProgress>();
            foreach (var step in ContributionForm.Steps)
            {
                int req = 0, filled = 0;
                foreach (var f in step.Fields.Where(f => f.Required && ContributionForm.IsVisible(f, answers)))
                {
                    req++;
                    if (answers.TryGetValue(f.Path, out var v) && !string.IsNullOrWhiteSpace(v)) filled++;
                }
                list.Add(new StepProgress(filled, req));
            }
            return list;
        }

        // ============================================================ approval

        /// <summary>
        /// Creates the public Dataset from an approved submission, the same way Datasets/Create does
        /// (dataset row + the 11 one-to-one tables, display ids renumbered, ontology search index rebuilt),
        /// and marks the submission Approved. Only visible answers of the form are copied.
        /// Returns the new dataset id.
        /// </summary>
        public async Task<int> ApproveAsync(DatasetSubmission s, string reviewerEmail)
        {
            var answers = ReadAnswers(s);
            var dataset = BuildDataset(answers);

            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                dataset.DisplayId = (await _db.Datasets.MaxAsync(x => (int?)x.DisplayId) ?? 0) + 1;
                _db.Datasets.Add(dataset);
                await _db.SaveChangesAsync();

                s.Status = SubmissionStatus.Approved;
                s.ApprovedDatasetId = dataset.DatasetId;
                s.ReviewedBy = reviewerEmail;
                s.ReviewedAt = DateTime.UtcNow;
                s.UpdatedAt = s.ReviewedAt.Value;
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
            });

            // Same follow-up as DatasetsController.Create
            await RegenerateDisplayIdsAsync();
            try
            {
                await _ontologyIndex.RebuildAsync(dataset.DatasetId);
            }
            catch (Exception ex)
            {
                // The dataset exists; the index is rebuilt again on the next edit
                _logger.LogError(ex, "Ontology index rebuild failed for dataset {Id}", dataset.DatasetId);
            }

            _logger.LogInformation("Submission {SubmissionId} approved by {Reviewer} as dataset {DatasetId}",
                s.Id, reviewerEmail, dataset.DatasetId);
            return dataset.DatasetId;
        }

        /// <summary>Builds the Dataset with all 11 child records from the answers (not saved).</summary>
        public static Dataset BuildDataset(IReadOnlyDictionary<string, string> answers)
        {
            var dataset = new Dataset
            {
                StudyDesign = new StudyDesign(),
                Publication = new Publication(),
                StudyComponent = new StudyComponent(),
                DatasetInfo = new DatasetInfo(),
                InVivo = new InVivo(),
                Procedures = new Procedures(),
                ImageAcquisition = new ImageAcquisition(),
                ImageData = new ImageData(),
                ImageCorrelation = new ImageCorrelation(),
                Analyzed = new Analyzed(),
                Ontology = new OntologyEntity(),
            };

            foreach (var field in ContributionForm.FieldsByPath.Values)
            {
                if (!ContributionForm.IsVisible(field, answers)) continue;
                if (!answers.TryGetValue(field.Path, out var value) || string.IsNullOrWhiteSpace(value)) continue;

                var parts = field.Path.Split('.');
                var child = typeof(Dataset).GetProperty(parts[0])?.GetValue(dataset);
                var prop = child?.GetType().GetProperty(parts[1]);
                if (prop == null || prop.PropertyType != typeof(string) || !prop.CanWrite)
                    continue; // the form and the model are checked against each other in development; skip safely

                prop.SetValue(child, value.Trim());
            }

            // Same normalisation as the Create form
            dataset.StudyComponent!.ImagingModality = ImagingModalityGroups.ToStoredValue(dataset.StudyComponent.ImagingModality);

            // The Disease Category DOID follows the chosen category (as disease-category.js does on Create)
            var category = DiseaseCategories.Find(dataset.InVivo!.DiseaseCategory);
            if (category != null)
            {
                dataset.InVivo.DiseaseCategory = category.Label;
                dataset.Ontology!.DoidDiseaseCategory = category.Code;
            }

            dataset.Analyzed!.UpdatedYear = DateTime.UtcNow.Year.ToString();

            return dataset;
        }

        // Same as DatasetsController.RegenerateDisplayIdsAsync: display ids are always 1..N
        private async Task RegenerateDisplayIdsAsync()
        {
            var all = await _db.Datasets.OrderBy(x => x.DatasetId).ToListAsync();
            var next = 1;
            foreach (var ds in all) ds.DisplayId = next++;
            await _db.SaveChangesAsync();
        }
    }
}
