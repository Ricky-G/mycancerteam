using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Notes;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace MyCancerTeam.Infrastructure.Notes;

/// <summary>
/// Scans the input content folders (medical notes, non-medical notes and research) for newly
/// added note files. Plain-text (.md/.markdown/.txt), PDF (.pdf) and Word (.docx) documents are
/// supported; PDF and Word text is extracted locally, and scanned/image-only PDFs with no
/// extractable text are flagged via <see cref="DetectedNote.RequiresOcr"/>. The folder the
/// application itself writes to (our notes) is deliberately excluded to avoid feedback loops.
/// </summary>
public sealed class FolderNoteScanner : IFolderNoteScanner
{
    private static readonly string[] NoteExtensions = [".md", ".markdown", ".txt", ".pdf", ".docx"];
    private static readonly string[] PlainTextExtensions = [".md", ".markdown", ".txt"];

    private readonly IReadOnlyList<string> _watchedFolders;
    private readonly HashSet<string> _seenFiles = new(StringComparer.OrdinalIgnoreCase);

    public FolderNoteScanner(AppConfiguration configuration)
    {
        _watchedFolders =
        [
            configuration.MedicalNotesFolderPath,
            configuration.NonMedicalNotesFolderPath,
            configuration.ResearchFolderPath
        ];
    }

    public IReadOnlyList<string> WatchedFolders => _watchedFolders;

    public int MarkExistingNotesAsSeen()
    {
        var count = 0;
        foreach (var file in EnumerateNoteFiles())
        {
            if (_seenFiles.Add(file))
            {
                count++;
            }
        }

        return count;
    }

    public async Task<IReadOnlyList<DetectedNote>> ScanForNewNotesAsync(CancellationToken cancellationToken = default)
    {
        List<DetectedNote>? detected = null;

        foreach (var folder in _watchedFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var file in EnumerateNoteFiles(folder))
            {
                if (_seenFiles.Contains(file))
                {
                    continue;
                }

                NoteReadResult readResult;
                try
                {
                    readResult = await ReadNoteContentAsync(file, cancellationToken);
                }
                catch (IOException)
                {
                    // The file may still be in the middle of being written/copied.
                    // Leave it unseen so it is retried on the next scan.
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                _seenFiles.Add(file);
                detected ??= [];
                detected.Add(new DetectedNote
                {
                    FilePath = file,
                    FolderPath = folder,
                    Content = readResult.Content,
                    RequiresOcr = readResult.RequiresOcr
                });
            }
        }

        return (IReadOnlyList<DetectedNote>?)detected ?? [];
    }

    private IEnumerable<string> EnumerateNoteFiles()
    {
        foreach (var folder in _watchedFolders)
        {
            foreach (var file in EnumerateNoteFiles(folder))
            {
                yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateNoteFiles(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(static file => NoteExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
    }

    private static async Task<NoteReadResult> ReadNoteContentAsync(string file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file);

        if (PlainTextExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken);
            return new NoteReadResult(text, RequiresOcr: false);
        }

        // Binary documents: read the raw bytes first so transient IOExceptions (file still
        // being copied) propagate and the file is retried, then parse the bytes in memory.
        var bytes = await File.ReadAllBytesAsync(file, cancellationToken);

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractPdfText(bytes);
        }

        if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractDocxText(bytes);
        }

        // Unreachable in practice: EnumerateNoteFiles only yields the extensions above.
        return new NoteReadResult(string.Empty, RequiresOcr: false);
    }

    private static NoteReadResult ExtractPdfText(byte[] bytes)
    {
        try
        {
            using var pdf = PdfDocument.Open(bytes);

            var builder = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                var pageText = ContentOrderTextExtractor.GetText(page);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    builder.AppendLine(pageText);
                }
            }

            var text = builder.ToString().Trim();

            // No extractable text almost always means a scanned/image-only PDF, which needs OCR.
            return new NoteReadResult(text, RequiresOcr: string.IsNullOrWhiteSpace(text));
        }
        catch (Exception)
        {
            // Encrypted, corrupt or otherwise unreadable PDF: flag for OCR/manual handling
            // rather than letting one bad file crash the background watcher loop.
            return new NoteReadResult(string.Empty, RequiresOcr: true);
        }
    }

    private static NoteReadResult ExtractDocxText(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var document = WordprocessingDocument.Open(stream, false);

            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return new NoteReadResult(string.Empty, RequiresOcr: false);
            }

            var paragraphs = body
                .Descendants<Paragraph>()
                .Select(static paragraph => paragraph.InnerText);

            var text = string.Join(Environment.NewLine, paragraphs).Trim();
            return new NoteReadResult(text, RequiresOcr: false);
        }
        catch (Exception)
        {
            // Corrupt or unreadable Word document: surface as empty rather than crashing.
            return new NoteReadResult(string.Empty, RequiresOcr: false);
        }
    }

    private readonly record struct NoteReadResult(string Content, bool RequiresOcr);
}
