using MissedGitHubUpdates.Models;
using Octokit;

namespace MissedGitHubUpdates.Services;

/// <summary>
/// Wraps Octokit to:
///   1. Validate a GitHub PAT + username combination.
///   2. Poll /users/{username}/events and return only new, relevant events.
/// </summary>
public class GitHubService
{
    private GitHubClient? _client;
    private string _username = string.Empty;

    // ── Initialise / re-initialise with a token ───────────────────────────────

    /// <summary>
    /// Call this once on startup (and again if the user changes their token in Settings).
    /// </summary>
    public void Initialise(string pat, string username)
    {
        _username = username;
        _client = new GitHubClient(new ProductHeaderValue("MissedGitHubUpdates"))
        {
            Credentials = new Credentials(pat)
        };
    }

    // ── Token validation ──────────────────────────────────────────────────────

    /// <summary>
    /// Tests whether the supplied PAT + username are valid.
    /// Returns (true, "username") on success, or (false, error message) on failure.
    /// Used by the Settings window's "Test Connection" button.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(string pat, string username)
    {
        try
        {
            var testClient = new GitHubClient(new ProductHeaderValue("MissedGitHubUpdates-Test"))
            {
                Credentials = new Credentials(pat)
            };

            // Fetch the authenticated user — cheapest valid API call
            var user = await testClient.User.Get(username);
            return (true, $"Connected as: {user.Login}");
        }
        catch (AuthorizationException)
        {
            return (false, "Invalid token. Check your PAT and try again.");
        }
        catch (NotFoundException)
        {
            return (false, $"Username \"{username}\" not found on GitHub.");
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    // ── Polling ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the user's public event feed and returns only events newer than
    /// <paramref name="lastSeenEventId"/> that are of type PushEvent or PullRequestEvent.
    ///
    /// Returns an empty list if:
    ///   - The client hasn't been initialised yet.
    ///   - There are no new events.
    ///   - The API call fails (error is swallowed — polling just waits for next tick).
    /// </summary>
    public async Task<List<GitHubEventResult>> PollNewEventsAsync(string lastSeenEventId)
    {
        if (_client == null || string.IsNullOrEmpty(_username))
            return [];

        try
        {
            var activities = await _client.Activity.Events.GetAllUserPerformed(_username);
            var results = new List<GitHubEventResult>();

            foreach (var activity in activities)
            {
                // Stop as soon as we hit something we've already processed
                if (activity.Id == lastSeenEventId)
                    break;

                var parsed = ParseEvent(activity);
                if (parsed != null)
                    results.Add(parsed);
            }

            return results;
        }
        catch (RateLimitExceededException)
        {
            // Back off — polling timer will retry on next interval
            return [];
        }
        catch (AuthorizationException)
        {
            // Token expired or revoked — surface this separately later
            return [];
        }
        catch
        {
            // Network error etc. — silent retry next tick
            return [];
        }
    }

    // ── Event parsing ─────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a raw Octokit Activity object into our own GitHubEventResult model.
    /// Returns null for event types we don't care about.
    /// </summary>
    private static GitHubEventResult? ParseEvent(Activity activity)
    {
        switch (activity.Type)
        {
            case "PushEvent":
            {
                // The payload is a dynamic object from Octokit
                var payload = activity.Payload as PushEventPayload;
                if (payload == null) return null;

                // Ref format: "refs/heads/main" — strip the prefix
                var branch = activity.Payload is PushEventPayload p
                    ? p.Ref?.Replace("refs/heads/", "") ?? "unknown"
                    : "unknown";

                return new GitHubEventResult
                {
                    EventId     = activity.Id,
                    EventType   = GitHubEventType.Push,
                    Actor       = activity.Actor?.Login ?? "unknown",
                    RepoName    = activity.Repo?.Name ?? "unknown",
                    BranchName  = branch,
                    CommitCount = payload.Commits?.Count ?? 1,
                    GitHubUrl   = $"https://github.com/{activity.Repo?.Name}/commits/{branch}"
                };
            }

            case "PullRequestEvent":
            {
                var payload = activity.Payload as PullRequestEventPayload;
                if (payload == null) return null;

                // "closed" + merged = true means it was actually merged
                var action = payload.Action == "closed" && (payload.PullRequest?.Merged ?? false)
                    ? "merged"
                    : payload.Action ?? "unknown";

                return new GitHubEventResult
                {
                    EventId            = activity.Id,
                    EventType          = GitHubEventType.PullRequest,
                    Actor              = activity.Actor?.Login ?? "unknown",
                    RepoName           = activity.Repo?.Name ?? "unknown",
                    PullRequestTitle   = payload.PullRequest?.Title ?? "Untitled PR",
                    PullRequestAction  = action,
                    GitHubUrl          = payload.PullRequest?.HtmlUrl ?? $"https://github.com/{activity.Repo?.Name}/pulls"
                };
            }

            default:
                return null; // ignore all other event types
        }
    }
}
