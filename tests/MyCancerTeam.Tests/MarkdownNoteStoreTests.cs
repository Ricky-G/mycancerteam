using MyCancerTeam.Core.Agents;
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

    [Fact]
    public async Task NoteStore_ReadSummaryAsync_ShouldReturnEmptyWhenMissingAndRoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-summary-read-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var config = new AppConfiguration
        {
            LocalWorkingFolderPath = root,
            OurNotesFolderPath = Path.Combine(root, "our-notes")
        };

        try
        {
            var store = new MarkdownNoteStore(config);

            Assert.Equal(string.Empty, await store.ReadSummaryAsync());

            await store.WriteSummaryAsync("# Summary\n\nState.");
            Assert.Equal("# Summary\n\nState.", await store.ReadSummaryAsync());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task NoteStore_ReadMdtStateAsync_ShouldReturnNullWhenMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-mdt-state-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var config = new AppConfiguration
        {
            LocalWorkingFolderPath = root,
            OurNotesFolderPath = Path.Combine(root, "our-notes")
        };

        try
        {
            var store = new MarkdownNoteStore(config);

            Assert.Null(await store.ReadMdtStateAsync());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task NoteStore_WriteMdtStateAsync_ShouldRoundTripStructuredState()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-mdt-roundtrip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var config = new AppConfiguration
        {
            LocalWorkingFolderPath = root,
            OurNotesFolderPath = Path.Combine(root, "our-notes")
        };

        try
        {
            var store = new MarkdownNoteStore(config);

            var state = new MdtState
            {
                CurrentDiagnosis = "Stage II breast cancer.",
                CurrentTreatment = "Adjuvant chemotherapy planned.",
                NextSteps = ["Confirm pathology.", "Review options."],
                EngagedAgents = ["Patient Owner Agent", "Medical Oncologist Agent"]
            };

            await store.WriteMdtStateAsync(state);
            var read = await store.ReadMdtStateAsync();

            Assert.NotNull(read);
            Assert.Equal("Stage II breast cancer.", read.CurrentDiagnosis);
            Assert.Equal("Adjuvant chemotherapy planned.", read.CurrentTreatment);
            Assert.Equal(["Confirm pathology.", "Review options."], read.NextSteps);
            Assert.Equal(["Patient Owner Agent", "Medical Oncologist Agent"], read.EngagedAgents);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task NoteStore_WriteMdtStateAsync_ShouldOverwritePreviousState()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-mdt-overwrite-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var config = new AppConfiguration
        {
            LocalWorkingFolderPath = root,
            OurNotesFolderPath = Path.Combine(root, "our-notes")
        };

        try
        {
            var store = new MarkdownNoteStore(config);

            await store.WriteMdtStateAsync(new MdtState { CurrentDiagnosis = "First state." });
            await store.WriteMdtStateAsync(new MdtState { CurrentDiagnosis = "Second state." });

            var read = await store.ReadMdtStateAsync();
            Assert.Equal("Second state.", read?.CurrentDiagnosis);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
