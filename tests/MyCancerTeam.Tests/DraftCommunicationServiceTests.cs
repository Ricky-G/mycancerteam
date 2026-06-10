using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Drafts;
using MyCancerTeam.Infrastructure.Drafts;

namespace MyCancerTeam.Tests;

public sealed class DraftCommunicationServiceTests
{
    [Fact]
    public async Task DraftService_ShouldGenerateMarkdownAndPersistFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-drafts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var config = new AppConfiguration
            {
                DraftCommunicationsFolderPath = root
            };

            var exporter = new MarkdownDraftExporter(config);
            var service = new DraftCommunicationService(exporter);

            var result = await service.CreateDraftAsync(new DraftCommunicationRequest
            {
                DraftType = "emails",
                RecipientType = "clinician",
                Subject = "Second-opinion request",
                PatientContextSummary = "Patient requests review of current treatment options.",
                AdditionalDetails = "Attach latest pathology report and timeline."
            });

            Assert.True(File.Exists(result.FilePath));
            Assert.Contains("Second-opinion request", result.MarkdownContent);
            Assert.Contains("Safety and Validation Note", result.MarkdownContent);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
