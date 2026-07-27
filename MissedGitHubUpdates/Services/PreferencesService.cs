using System.IO;
using MissedGitHubUpdates.Models;
using Newtonsoft.Json;

namespace MissedGitHubUpdates.Services;

/// <summary>
/// Reads and writes the app's non-sensitive preferences to a local JSON file.
/// File location: %APPDATA%\MissedGitHubUpdates\prefs.json
/// Thread-safe for reads and writes.
/// </summary>
public static class PreferencesService
{
    // ── File path ─────────────────────────────────────────────────────────────

    private static readonly string AppDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "MissedGitHubUpdates");

    private static readonly string PrefsFilePath =
        Path.Combine(AppDataFolder, "prefs.json");

    private static readonly object _lock = new();

    // ── Load ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads preferences from disk.
    /// Returns a default AppPreferences object if the file doesn't exist yet
    /// (i.e. first run).
    /// </summary>
    public static AppPreferences Load()
    {
        lock (_lock)
        {
            if (!File.Exists(PrefsFilePath))
                return new AppPreferences();

            try
            {
                var json = File.ReadAllText(PrefsFilePath);
                return JsonConvert.DeserializeObject<AppPreferences>(json)
                       ?? new AppPreferences();
            }
            catch
            {
                // Corrupted file — return defaults and let a future Save overwrite it
                return new AppPreferences();
            }
        }
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves the given preferences to disk.
    /// Creates the AppData folder if it doesn't exist.
    /// </summary>
    public static void Save(AppPreferences prefs)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(AppDataFolder); // no-op if already exists
            var json = JsonConvert.SerializeObject(prefs, Formatting.Indented);
            File.WriteAllText(PrefsFilePath, json);
        }
    }

    // ── Convenience: update only the LastSeenEventId ─────────────────────────

    /// <summary>
    /// Called after every successful poll to persist the latest event ID.
    /// Loads, updates the single field, and saves — so nothing else is lost.
    /// </summary>
    public static void UpdateLastSeenEventId(string eventId)
    {
        var prefs = Load();
        prefs.LastSeenEventId = eventId;
        Save(prefs);
    }
}
