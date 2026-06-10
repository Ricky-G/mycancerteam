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
            LatestSharedNotesPath = Path.Combine(root, "notes", "notes.md"),
            AgentMemoryFolderPath = Path.Combine(root, "agent-memory")
        };

        try
        {
            var store = new MarkdownNoteStore(config);
            await store.WriteSharedNotesAsync("hello notes");
            var shared = await store.ReadSharedNotesAsync();

            await store.WriteAgentNotesAsync("research-oncology.md", "agent content");
            var agentPath = Path.Combine(config.AgentMemoryFolderPath, "research-oncology.md");

            Assert.Equal("hello notes", shared);
            Assert.True(File.Exists(agentPath));
            Assert.Equal("agent content", await File.ReadAllTextAsync(agentPath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
