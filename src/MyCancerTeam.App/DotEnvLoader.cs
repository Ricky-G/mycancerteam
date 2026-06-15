using System;
using System.IO;

namespace MyCancerTeam.App;

/// <summary>
/// Minimal, dependency-free loader for a local <c>.env</c> file. Each non-comment line is
/// parsed as <c>KEY=VALUE</c> and promoted to a process environment variable so the existing
/// <see cref="MyCancerTeam.Infrastructure.Configuration.ConfigurationLoader"/> environment
/// overrides are honoured during local development.
/// </summary>
/// <remarks>
/// .NET does not read <c>.env</c> files automatically (unlike Node.js or Docker Compose),
/// so without this step the values would never reach <see cref="Environment.GetEnvironmentVariable(string)"/>.
/// Variables already present in the real environment take precedence and are never overwritten.
/// </remarks>
internal static class DotEnvLoader
{
    public static void Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();

            // Skip blank lines and comments.
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            // Support optional "export KEY=VALUE" syntax.
            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue; // No key, or no '=' present.
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            // Strip a single pair of matching surrounding quotes, if present.
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            // Let real environment variables win over the local .env file.
            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
