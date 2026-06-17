namespace MyCancerTeam.Infrastructure.Configuration;

public sealed class EnvironmentFileLoader
{
    public void Load(string repositoryRootPath)
    {
        var envFilePath = Path.Combine(repositoryRootPath, ".env");
        if (!File.Exists(envFilePath))
        {
            return;
        }

        var lines = File.ReadAllLines(envFilePath);
        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i].Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line[7..].Trim();
            }

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
            {
                throw new FormatException($"Invalid .env entry on line {lineNumber}: '{lines[i]}'");
            }

            var key = line[..equalsIndex].Trim();
            var value = line[(equalsIndex + 1)..].Trim();

            if (key.Length == 0)
            {
                throw new FormatException($"Invalid .env entry on line {lineNumber}: '{lines[i]}'");
            }

            value = UnwrapQuotedValue(value);

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string UnwrapQuotedValue(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var first = value[0];
        var last = value[^1];
        if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
        {
            return value[1..^1];
        }

        return value;
    }
}
