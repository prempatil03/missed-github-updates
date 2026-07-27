namespace MissedGitHubUpdates.Models;

/// <summary>Type of GitHub event we care about.</summary>
public enum GitHubEventType
{
    Push,
    PullRequest
}

/// <summary>
/// A parsed, notification-ready summary of a single GitHub event.
/// Produced by GitHubService, consumed by NotificationService.
/// </summary>
public class GitHubEventResult
{
    /// <summary>Unique GitHub event ID — used to track LastSeenEventId.</summary>
    public string EventId { get; set; } = string.Empty;

    public GitHubEventType EventType { get; set; }

    /// <summary>GitHub user who triggered the event (e.g. "johndoe").</summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>Repository full name (e.g. "octocat/my-repo").</summary>
    public string RepoName { get; set; } = string.Empty;

    // ── Push-specific ──────────────────────────────────────────────────────

    /// <summary>Branch name for push events (e.g. "main").</summary>
    public string? BranchName { get; set; }

    /// <summary>Number of commits in the push.</summary>
    public int CommitCount { get; set; }

    // ── Pull Request-specific ─────────────────────────────────────────────

    /// <summary>PR title (e.g. "Fix login bug").</summary>
    public string? PullRequestTitle { get; set; }

    /// <summary>PR action: opened / closed / merged.</summary>
    public string? PullRequestAction { get; set; }

    /// <summary>Direct URL to the PR or commit on GitHub — opened when user clicks the toast.</summary>
    public string GitHubUrl { get; set; } = string.Empty;
}
