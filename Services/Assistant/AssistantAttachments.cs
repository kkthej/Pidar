using UglyToad.PdfPig;

namespace Pidar.Services.Assistant
{
    /// <summary>
    /// A file attached to one message to the assistant: a PDF (paper, protocol, report) or a picture
    /// (e.g. a photo of handwritten lab notes). Held in memory for that one request only: it is sent to the
    /// model with the message and then dropped. Only its name (and page count) is kept in the conversation.
    /// </summary>
    public sealed record AssistantAttachment(string FileName, string MediaType, byte[] Data, int? Pages)
    {
        public bool IsPdf => MediaType == "application/pdf";

        /// <summary>"methods.pdf (6 pages)" / "notes.jpg (image)" — what the conversation remembers.</summary>
        public string Describe() => IsPdf
            ? $"{FileName} ({Pages} page{(Pages == 1 ? "" : "s")})"
            : $"{FileName} (image)";
    }

    /// <summary>Checks uploaded files before they go anywhere near the model.</summary>
    public static class AssistantAttachments
    {
        public const string Pdf = "application/pdf";

        /// <summary>For the file picker's accept="…".</summary>
        public const string AcceptAttribute = ".pdf,.jpg,.jpeg,.png,.webp,.gif,application/pdf,image/jpeg,image/png,image/webp,image/gif";

        /// <summary>
        /// Reads and checks one upload: real type from the first bytes (never the name or the browser's claim),
        /// size, and for PDFs that they open and are not longer than the page limit.
        /// Returns the attachment, or an error message for the person.
        /// </summary>
        public static async Task<(AssistantAttachment? File, string? Error)> ReadAsync(IFormFile upload, AssistantOptions o, CancellationToken ct)
        {
            var name = SafeName(upload.FileName);

            if (upload.Length == 0) return (null, $"{name} is empty.");
            if (upload.Length > o.MaxFileBytes)
                return (null, $"{name} is too large ({Mb(upload.Length)}). The limit is {Mb(o.MaxFileBytes)} per file.");

            byte[] data;
            using (var ms = new MemoryStream((int)upload.Length))
            {
                await upload.CopyToAsync(ms, ct);
                data = ms.ToArray();
            }

            var type = Sniff(data);
            if (type == null)
                return (null, $"{name}: only PDF files and pictures (JPEG, PNG, WebP, GIF) can be read. For an iPhone photo, export it as JPEG first.");

            if (type == Pdf)
            {
                int pages;
                try
                {
                    using var doc = PdfDocument.Open(data);
                    pages = doc.NumberOfPages;
                }
                catch (Exception)
                {
                    // damaged, or protected with a password
                    return (null, $"{name} could not be opened. If it is password-protected, save an unprotected copy first.");
                }
                if (pages > o.MaxPdfPages)
                    return (null, $"{name} has {pages} pages; at most {o.MaxPdfPages} can be read at once. Attach only the pages you need (e.g. the methods section).");
                return (new AssistantAttachment(name, type, data, pages), null);
            }

            if (data.Length > o.MaxImageBytes)
                return (null, $"{name} is too large for a picture ({Mb(data.Length)}, limit {Mb(o.MaxImageBytes)}). Please use a smaller photo.");
            return (new AssistantAttachment(name, type, data, null), null);
        }

        /// <summary>Media type from the file signature, or null when it is not a supported kind.</summary>
        public static string? Sniff(byte[] d)
        {
            static bool Starts(byte[] d, params byte[] sig) => d.Length >= sig.Length && sig.Select((b, i) => d[i] == b).All(x => x);

            if (Starts(d, 0x25, 0x50, 0x44, 0x46, 0x2D)) return Pdf;                          // %PDF-
            if (Starts(d, 0xFF, 0xD8, 0xFF)) return "image/jpeg";
            if (Starts(d, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)) return "image/png";
            if (Starts(d, 0x47, 0x49, 0x46, 0x38)) return "image/gif";                        // GIF8
            if (d.Length >= 12 && Starts(d, 0x52, 0x49, 0x46, 0x46)                           // RIFF....WEBP
                && d[8] == 0x57 && d[9] == 0x45 && d[10] == 0x42 && d[11] == 0x50) return "image/webp";
            return null;
        }

        /// <summary>
        /// Text of a PDF with a text layer (not scans), used only by test mode. Words are joined with spaces,
        /// pages with blank lines. Empty for scanned PDFs.
        /// </summary>
        public static string ExtractPdfText(byte[] data, int maxPages)
        {
            try
            {
                using var doc = PdfDocument.Open(data);
                var pages = doc.GetPages().Take(maxPages)
                    .Select(p => string.Join(" ", p.GetWords().Select(w => w.Text)));
                return string.Join("\n\n", pages).Trim();
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string SafeName(string? fileName)
        {
            var n = Path.GetFileName(fileName ?? "").Trim();
            n = new string(n.Where(c => !char.IsControl(c)).ToArray());
            if (n.Length == 0) n = "file";
            return n.Length > 120 ? n[..117] + "…" : n;
        }

        private static string Mb(long bytes) => $"{bytes / 1024d / 1024d:0.#} MB";
    }
}
