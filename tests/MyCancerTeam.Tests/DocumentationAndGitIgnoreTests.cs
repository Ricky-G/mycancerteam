using MyCancerTeam.Tests.Helpers;

namespace MyCancerTeam.Tests;

public sealed class DocumentationAndGitIgnoreTests
{
    [Fact]
    public void ReadmeAndGitIgnore_ShouldCoverLocalOnlySensitiveFolders()
    {
        var readme = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "README.md"));
        var gitignore = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, ".gitignore"));

        Assert.Contains(".local/clinical-notes/", readme);
        Assert.Contains(".local/notes/notes.md", readme);

        Assert.Contains(".local/", gitignore);
        Assert.Contains("**/clinical-notes/", gitignore);
        Assert.Contains("**/insurance-documents/", gitignore);
        Assert.Contains(".env", gitignore);
    }
}
