using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Infrastructure.Notes;

namespace MyCancerTeam.Tests;

public sealed class MarkdownNoteStoreTests
{
    [Fact]
    public async Task NoteStore_ShouldReadAndWriteSharedAndAgentNotes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-notes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var config = new AppConfiguration
        {
            LocalWorkingFolderPath = root,
            OurNotesFolderPath = Path.Combine(root, "our-notes")
        };

        try
        {
            var store = new MarkdownNoteStore(config);
            await store.WriteSharedNotesAsync("hello notes");
            var shared = await store.ReadSharedNotesAsync();

            await store.WriteAgentNotesAsync("research-oncology.md", "agent content");
            var agentPath = Path.Combine(config.OurNotesFolderPath, "research-oncology.md");

            Assert.Equal("hello notes", shared);
            Assert.True(File.Exists(agentPath));
            Assert.Equal("agent content", await File.ReadAllTextAsync(agentPath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task NoteStore_WriteSummaryAsync_ShouldWriteSummaryAtLocalRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-summary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var config = new AppConfiguration
        {
            LocalWorkingFolderPath = root,
            OurNotesFolderPath = Path.Combine(root, "our-notes")
        };

        try
        {
            var store = new MarkdownNoteStore(config);
            await store.WriteSummaryAsync("# Summary\n\nLatest update.");

            Assert.True(File.Exists(config.SummaryFilePath));
            Assert.Equal("# Summary\n\nLatest update.", await File.ReadAllTextAsync(config.SummaryFilePath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task NoteStore_WriteSummaryAsync_ShouldOverwritePreviousSummary()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-summary-overwrite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var config = new AppConfiguration
        {
            LocalWorkingFolderPath = root,
            OurNotesFolderPath = Path.Combine(root, "our-notes")
        };

        try
        {
            var store = new MarkdownNoteStore(config);
            await store.WriteSummaryAsync("first summary");
            await store.WriteSummaryAsync("second summary");

            Assert.Equal("second summary", await File.ReadAllTextAsync(config.SummaryFilePath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
