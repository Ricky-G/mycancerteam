namespace MyCancerTeam.Core.Drafts;

public interface IMarkdownDraftExporter
{
    Task<string> ExportAsync(string draftType, string markdownContent, CancellationToken cancellationToken = default);
}
