namespace MissedGitHubUpdates.Models;

/// <summary>
/// Stored in prefs.json (AppData\Roaming\MissedGitHubUpdates\prefs.json).
/// Contains non-sensitive user settings and internal polling state.
/// The GitHub PAT is NOT stored here — it lives in Windows Credential Manager.
/// </summary>
public class AppPreferences
{
    /// <summary>GitHub username — used to build the /users/{username}/events API URL.</summary>
    public string GitHubUsername { get; set; } = string.Empty;

    /// <summary>How often (in seconds) to poll GitHub. Minimum 30.</summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// The ID of the most recently processed GitHub event.
    /// On each poll we only look at events newer than this.
    /// Updated automatically after every successful poll — user never touches this.
    /// </summary>
    public string LastSeenEventId { get; set; } = string.Empty;
}
