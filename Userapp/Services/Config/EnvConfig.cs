using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Maui.Storage;

namespace CGM.PatientApp.Services.Config;

/// <summary>
/// Loads and exposes configuration values from .env files or packaged mobile assets.
/// </summary>
public static class EnvConfig
{
    private static readonly Dictionary<string, string> Values = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isInitialized;

    public static void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        // 1. Check local file paths (desktop/Windows/test environments)
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Userapp", ".env")
        ];

        foreach (var path in candidates)
        {
            try
            {
                if (File.Exists(path))
                {
                    using var stream = File.OpenRead(path);
                    using var reader = new StreamReader(stream);
                    LoadFromReader(reader);
                    break;
                }
            }
            catch
            {
                // Fallback to mobile package asset
            }
        }

        // 2. Load from mobile bundled asset (app.env) on Android / iOS
        try
        {
            using var assetStream = FileSystem.OpenAppPackageFileAsync("app.env").GetAwaiter().GetResult();
            using var assetReader = new StreamReader(assetStream);
            LoadFromReader(assetReader);
        }
        catch
        {
            // Bundled asset may not exist in non-packaged test runners
        }
    }

    private static void LoadFromReader(TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0) continue;

            var key = line[..separatorIndex].Trim();
            var val = line[(separatorIndex + 1)..].Trim();

            // Strip surrounding single or double quotes
            if (val.Length >= 2 && ((val.StartsWith('"') && val.EndsWith('"')) || (val.StartsWith('\'') && val.EndsWith('\''))))
            {
                val = val[1..^1].Trim();
            }

            if (!string.IsNullOrEmpty(key))
            {
                Values[key] = val;
            }
        }
    }

    /// <summary>
    /// Retrieves a configuration value by key, checking in-memory .env, then system environment variables, then defaultValue.
    /// </summary>
    public static string Get(string key, string defaultValue = "")
    {
        Initialize();

        if (Values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var sysEnv = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(sysEnv))
        {
            return sysEnv;
        }

        return defaultValue;
    }

    /// <summary>
    /// Retrieves a boolean configuration value by key.
    /// </summary>
    public static bool GetBool(string key, bool defaultValue = false)
    {
        var val = Get(key);
        if (string.IsNullOrWhiteSpace(val)) return defaultValue;
        return bool.TryParse(val, out var result) ? result : defaultValue;
    }
}
