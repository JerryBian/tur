using System;
using System.IO;
using System.Text.Json;

namespace Tur.Util;

public static class ConfigUtil
{
    public static string GetDefaultPath()
    {
        var fromEnv = Environment.GetEnvironmentVariable("TUR_CONFIG");
        if (!string.IsNullOrEmpty(fromEnv))
        {
            return Path.GetFullPath(fromEnv);
        }

        var home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(string.IsNullOrEmpty(home) ? "." : home, ".tur.json");
    }

    public static TurConfig Load()
    {
        return Load(GetDefaultPath());
    }

    /// <summary>
    /// Loads the config file. A missing file yields an empty config; a malformed
    /// file yields an empty config plus a warning on stderr (never fatal).
    /// </summary>
    public static TurConfig Load(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new TurConfig();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TurConfig>(json) ?? new TurConfig();
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine($"Warning: failed to read config '{path}': {ex.Message}");
            }
            catch (Exception) { }

            return new TurConfig();
        }
    }

    /// <summary>
    /// Merges a boolean option: explicit CLI value wins when the flag was provided,
    /// otherwise the config default applies, otherwise the CLI default (false).
    /// </summary>
    public static bool MergeBool(bool provided, bool cliValue, bool? configValue)
    {
        return provided ? cliValue : configValue ?? cliValue;
    }
}
