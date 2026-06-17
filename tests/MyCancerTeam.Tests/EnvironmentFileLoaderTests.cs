using MyCancerTeam.Infrastructure.Configuration;

namespace MyCancerTeam.Tests;

[Collection("Environment variable tests")]
public sealed class EnvironmentFileLoaderTests
{
    [Fact]
    public void Load_ShouldImportDotEnvValuesAndPreserveExistingEnvironmentVariables()
    {
        var root = Path.Combine(Path.GetTempPath(), $"mycancerteam-dotenv-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        File.WriteAllText(
            Path.Combine(root, ".env"),
            """
            MYCANCERTEAM_ENVIRONMENT=dev
            MYCANCERTEAM_AZURE_OPENAI_ENDPOINT=https://from-dotenv.openai.azure.com/
            MYCANCERTEAM_AZURE_OPENAI_DEPLOYMENT=dotenv-deployment
            """);

        var previousEnvironment = Environment.GetEnvironmentVariable("MYCANCERTEAM_ENVIRONMENT");
        var previousEndpoint = Environment.GetEnvironmentVariable("MYCANCERTEAM_AZURE_OPENAI_ENDPOINT");
        var previousDeployment = Environment.GetEnvironmentVariable("MYCANCERTEAM_AZURE_OPENAI_DEPLOYMENT");

        Environment.SetEnvironmentVariable("MYCANCERTEAM_AZURE_OPENAI_DEPLOYMENT", "env-deployment");

        try
        {
            var loader = new EnvironmentFileLoader();
            loader.Load(root);

            Assert.Equal("dev", Environment.GetEnvironmentVariable("MYCANCERTEAM_ENVIRONMENT"));
            Assert.Equal("https://from-dotenv.openai.azure.com/", Environment.GetEnvironmentVariable("MYCANCERTEAM_AZURE_OPENAI_ENDPOINT"));
            Assert.Equal("env-deployment", Environment.GetEnvironmentVariable("MYCANCERTEAM_AZURE_OPENAI_DEPLOYMENT"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MYCANCERTEAM_ENVIRONMENT", previousEnvironment);
            Environment.SetEnvironmentVariable("MYCANCERTEAM_AZURE_OPENAI_ENDPOINT", previousEndpoint);
            Environment.SetEnvironmentVariable("MYCANCERTEAM_AZURE_OPENAI_DEPLOYMENT", previousDeployment);
            Directory.Delete(root, true);
        }
    }
}
