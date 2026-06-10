namespace MyCancerTeam.Core.Notes;

/// <summary>
/// Watches the local content folders for newly added note files. Tracks which files
/// have already been seen for the lifetime of the instance (per-session, in-memory).
/// </summary>
public interface IFolderNoteScanner
{
    /// <summary>The folders being watched for new notes.</summary>
    IReadOnlyList<string> WatchedFolders { get; }

    /// <summary>
    /// Marks every note file that currently exists in the watched folders as already seen
    /// so that only files added afterwards are reported as new.
    /// </summary>
    /// <returns>The number of pre-existing note files that were marked as seen.</returns>
    int MarkExistingNotesAsSeen();

    /// <summary>
    /// Scans the watched folders and returns any note files that have not been reported yet.
    /// Reported files are remembered so they are not returned again.
    /// </summary>
    Task<IReadOnlyList<DetectedNote>> ScanForNewNotesAsync(CancellationToken cancellationToken = default);
}
