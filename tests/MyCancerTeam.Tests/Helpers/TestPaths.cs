namespace MyCancerTeam.Tests.Helpers;

internal static class TestPaths
{
    public static string RepoRoot
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
