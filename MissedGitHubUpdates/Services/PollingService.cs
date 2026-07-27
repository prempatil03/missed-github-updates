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
        // Skip this tick if the previous one is still running (slow network etc.)
        if (_isBusy) return;
        _isBusy = true;

        try
        {
            var prefs      = PreferencesService.Load();
            var lastSeenId = prefs.LastSeenEventId;

            var newEvents = await _gitHub.PollNewEventsAsync(lastSeenId);

            if (newEvents.Count == 0)
                return;

            // Fire a toast for each new event (newest first from the API,
            // so reverse to show oldest-first in the notification stack)
            foreach (var evt in Enumerable.Reverse(newEvents))
                NotificationService.Show(evt);

            // Persist the most recent event ID (first item — newest)
            PreferencesService.UpdateLastSeenEventId(newEvents[0].EventId);
        }
        catch
        {
            // Swallow all errors — the timer will try again next tick
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
