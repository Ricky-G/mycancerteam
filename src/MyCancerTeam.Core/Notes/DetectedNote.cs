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

    /// <summary>
    /// True when the file is a document (e.g. a scanned/image-only PDF) from which no text
    /// could be extracted, indicating that OCR is required before its contents can be used.
    /// </summary>
    public bool RequiresOcr { get; init; }
}
