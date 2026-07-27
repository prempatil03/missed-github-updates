using System.Windows;
using System.Drawing;
using System.Windows.Forms;
using MissedGitHubUpdates.Services;
using MissedGitHubUpdates.Windows;
using Application = System.Windows.Application;

namespace MissedGitHubUpdates;

/// <summary>
/// App entry point. Owns the system tray icon and the polling engine.
/// No main window is ever shown — the app lives entirely in the system tray.
///
/// Startup flow:
///   1. Initialise toast notifications
///   2. Build tray icon + context menu
///   3. Load saved token + prefs
///   4. If no token saved yet → open Settings window automatically
///   5. Start the background polling timer
/// </summary>
public partial class App : Application
{
    private NotifyIcon?    _trayIcon;
    private GitHubService  _gitHubService  = new();
    private PollingService? _pollingService;

    // ── Startup ───────────────────────────────────────────────────────────────

    private void App_Startup(object sender, StartupEventArgs e)
    {
        NotificationService.Initialise();
        InitialiseTrayIcon();
        StartPollingIfConfigured();
    }

    // ── Tray Icon ─────────────────────────────────────────────────────────────

    private void InitialiseTrayIcon()
    {
        var contextMenu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += OnSettingsClicked;

        var separatorItem = new ToolStripSeparator();

        var quitItem = new ToolStripMenuItem("Quit");
        quitItem.Click += OnQuitClicked;

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(separatorItem);
        contextMenu.Items.Add(quitItem);

        _trayIcon = new NotifyIcon
        {
            Icon             = SystemIcons.Application, // placeholder — custom icon in next step
            Text             = "Missed GitHub Updates — Running",
            ContextMenuStrip = contextMenu,
            Visible          = true
        };

        _trayIcon.DoubleClick += OnSettingsClicked;
    }

    // ── Polling startup ───────────────────────────────────────────────────────

    private void StartPollingIfConfigured()
    {
        var pat   = CredentialService.LoadToken();
        var prefs = PreferencesService.Load();

        if (string.IsNullOrEmpty(pat) || string.IsNullOrEmpty(prefs.GitHubUsername))
        {
            // First run — no token saved yet. Open Settings so the user can configure.
            OpenSettingsWindow();
            return;
        }

        // Token and username exist — initialise the GitHub client and start polling
        _gitHubService.Initialise(pat, prefs.GitHubUsername);

        _pollingService = new PollingService(_gitHubService);
        _pollingService.Start();
    }

    // ── Context Menu Handlers ─────────────────────────────────────────────────

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        OpenSettingsWindow();
    }

    private void OnQuitClicked(object? sender, EventArgs e)
    {
        Shutdown();
    }

    // ── Settings window helper ────────────────────────────────────────────────

    private void OpenSettingsWindow()
    {
        // Bring to front if already open — don't open a second copy
        var existing = Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        var win = new SettingsWindow();

        // When the user saves settings, restart the polling engine with the new values
        win.Closed += (_, _) => RestartPollingAfterSettingsChange();

        win.Show();
    }

    private void RestartPollingAfterSettingsChange()
    {
        if (_pollingService == null)
        {
            // Polling hadn't started yet (first run). Create and start it now.
            var pat   = CredentialService.LoadToken();
            var prefs = PreferencesService.Load();

            if (!string.IsNullOrEmpty(pat) && !string.IsNullOrEmpty(prefs.GitHubUsername))
            {
                _gitHubService.Initialise(pat, prefs.GitHubUsername);
                _pollingService = new PollingService(_gitHubService);
                _pollingService.Start();
            }
        }
        else
        {
            // Already running — restart with updated token/interval
            _pollingService.Restart();
        }
    }

    // ── Exit ──────────────────────────────────────────────────────────────────

    private void App_Exit(object sender, ExitEventArgs e)
    {
        _pollingService?.Dispose();
        NotificationService.Cleanup();
        _trayIcon?.Dispose();
    }
}
