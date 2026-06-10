using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Notes;

namespace MyCancerTeam.Infrastructure.Notes;

/// <summary>
/// Scans the input content folders (medical notes, non-medical notes and research) for
/// newly added text note files. The folder the application itself writes to (our notes:
/// shared notes, agent memory and drafts) is deliberately excluded to avoid feedback loops.
/// </summary>
public sealed class FolderNoteScanner : IFolderNoteScanner
{
    private static readonly string[] NoteExtensions = [".md", ".markdown", ".txt"];

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

                string content;
                try
                {
                    content = await File.ReadAllTextAsync(file, cancellationToken);
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
                    Content = content
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
}
