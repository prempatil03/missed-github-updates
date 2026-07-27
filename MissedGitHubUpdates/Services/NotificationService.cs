using Microsoft.Toolkit.Uwp.Notifications;
using MissedGitHubUpdates.Models;
using System.Diagnostics;

namespace MissedGitHubUpdates.Services;

/// <summary>
/// Fires native Windows 10/11 toast notifications for GitHub events.
/// Clicking a notification opens the relevant GitHub URL in the default browser.
/// </summary>
public static class NotificationService
{
    // Called once at app startup so Windows knows which app owns the toasts
    public static void Initialise()
    {
        // Register a COM activator app ID so toast clicks work correctly.
        // For a simple tray app we just subscribe to the activation callback.
        ToastNotificationManagerCompat.OnActivated += OnToastActivated;
    }

    // ── Public: dispatch the right toast for any event ────────────────────────

    public static void Show(GitHubEventResult evt)
    {
        if (evt.EventType == GitHubEventType.Push)
            ShowPushNotification(evt);
        else
            ShowPullRequestNotification(evt);
    }

    // ── Push toast ────────────────────────────────────────────────────────────

    private static void ShowPushNotification(GitHubEventResult evt)
    {
        var commitWord = evt.CommitCount == 1 ? "commit" : "commits";

        new ToastContentBuilder()
            .AddArgument("url", evt.GitHubUrl)           // passed back on click
            .AddText($"🔔 Push — {evt.RepoName}")
            .AddText($"{evt.Actor} pushed {evt.CommitCount} {commitWord} to {evt.BranchName}")
            .Show();
    }

    // ── Pull Request toast ────────────────────────────────────────────────────

    private static void ShowPullRequestNotification(GitHubEventResult evt)
    {
        var action = evt.PullRequestAction ?? "updated";

        new ToastContentBuilder()
            .AddArgument("url", evt.GitHubUrl)
            .AddText($"🔔 Pull Request — {evt.RepoName}")
            .AddText($"{evt.Actor} {action} \"{evt.PullRequestTitle}\"")
            .Show();
    }

    // ── Toast click handler ───────────────────────────────────────────────────

    private static void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        var args = ToastArguments.Parse(e.Argument);

        if (args.TryGetValue("url", out string url) && !string.IsNullOrEmpty(url))
        {
            // Open the GitHub URL in the user's default browser
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    public static void Cleanup()
    {
        // Unregister the notification activator on app exit
        ToastNotificationManagerCompat.Uninstall();
    }
}
