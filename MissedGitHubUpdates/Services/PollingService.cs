using MissedGitHubUpdates.Models;

namespace MissedGitHubUpdates.Services;

/// <summary>
/// Owns the background polling timer.
/// Every tick it: polls GitHub → filters new events → fires toasts → saves last-seen ID.
///
/// Lifecycle:
///   Call Start() once after settings are loaded.
///   Call Stop() on app exit.
///   Call Restart() when the user saves new settings (new token / interval).
/// </summary>
public class PollingService : IDisposable
{
    private readonly GitHubService _gitHub;
    private System.Threading.Timer? _timer;
    private bool _isBusy;   // guard against overlapping ticks

    public PollingService(GitHubService gitHub)
    {
        _gitHub = gitHub;
    }

    // ── Start / Stop / Restart ────────────────────────────────────────────────

    /// <summary>Starts polling at the interval stored in prefs.json.</summary>
    public void Start()
    {
        var prefs    = PreferencesService.Load();
        var interval = TimeSpan.FromSeconds(
            Math.Max(30, prefs.PollingIntervalSeconds)); // enforce minimum 30s

        // Fire first tick after one full interval (give the app a moment to settle)
        _timer = new System.Threading.Timer(
            callback: _ => _ = TickAsync(),
            state:    null,
            dueTime:  interval,
            period:   interval);
    }

    /// <summary>Stops the timer cleanly.</summary>
    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Call this when the user saves new settings.
    /// Stops the current timer and starts a fresh one with the updated interval.
    /// Also re-initialises the GitHubService with the new token/username.
    /// </summary>
    public void Restart()
    {
        Stop();

        var prefs = PreferencesService.Load();
        var pat   = CredentialService.LoadToken();

        if (!string.IsNullOrEmpty(pat) && !string.IsNullOrEmpty(prefs.GitHubUsername))
            _gitHub.Initialise(pat, prefs.GitHubUsername);

        Start();
    }

    // ── Tick ──────────────────────────────────────────────────────────────────

    private async Task TickAsync()
    {
        if (_isBusy) return;
        _isBusy = true;

        try
        {
            var prefs      = PreferencesService.Load();
            var lastSeenId = prefs.LastSeenEventId;

            System.Diagnostics.Debug.WriteLine($"[Polling] Tick — lastSeenId: '{lastSeenId}'");

            var newEvents = await _gitHub.PollNewEventsAsync(lastSeenId);

            System.Diagnostics.Debug.WriteLine($"[Polling] New events found: {newEvents.Count}");

            if (newEvents.Count == 0)
                return;

            foreach (var evt in Enumerable.Reverse(newEvents))
            {
                System.Diagnostics.Debug.WriteLine($"[Polling] Firing notification: {evt.EventType} — {evt.RepoName}");
                NotificationService.Show(evt);
            }

            PreferencesService.UpdateLastSeenEventId(newEvents[0].EventId);
            System.Diagnostics.Debug.WriteLine($"[Polling] Saved new lastSeenId: {newEvents[0].EventId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Polling] Tick error: {ex.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
