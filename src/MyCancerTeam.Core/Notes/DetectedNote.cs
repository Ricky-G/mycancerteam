namespace MyCancerTeam.Core.Notes;

/// <summary>
/// Represents a note file discovered by an <see cref="IFolderNoteScanner"/> while
/// watching the local content folders.
/// </summary>
public sealed record DetectedNote
{
    /// <summary>Absolute path of the detected file.</summary>
    public required string FilePath { get; init; }

    /// <summary>Absolute path of the watched folder the file was found in.</summary>
    public required string FolderPath { get; init; }

    /// <summary>Text content of the detected file.</summary>
    public required string Content { get; init; }
}
