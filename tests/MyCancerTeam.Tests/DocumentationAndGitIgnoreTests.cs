using MyCancerTeam.Tests.Helpers;

namespace MyCancerTeam.Tests;

public sealed class DocumentationAndGitIgnoreTests
{
    [Fact]
    public void ReadmeAndGitIgnore_ShouldCoverLocalOnlySensitiveFolders()
    {
        var readme = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "README.md"));
        var gitignore = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, ".gitignore"));

        Assert.Contains(".local/medical-notes/", readme);
        Assert.Contains(".local/our-notes/", readme);

        Assert.Contains(".local/", gitignore);
        Assert.Contains("**/medical-notes/", gitignore);
        Assert.Contains("**/non-medical-notes/", gitignore);
        Assert.Contains(".env", gitignore);
    }
}
