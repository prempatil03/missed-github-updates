using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MissedGitHubUpdates.Models;
using MissedGitHubUpdates.Services;
using WpfColor = System.Windows.Media.Color;

namespace MissedGitHubUpdates.Windows;

/// <summary>
/// Settings window — user enters their GitHub PAT, username, and polling interval.
/// On Save: token → Credential Manager, username + interval → prefs.json.
/// Opened from the system tray context menu.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        LoadExistingSettings();
    }

    // ── Load saved settings into the UI fields on open ────────────────────────

    private void LoadExistingSettings()
    {
        // Load non-sensitive prefs from JSON
        var prefs = PreferencesService.Load();
        UsernameBox.Text = prefs.GitHubUsername;
        IntervalBox.Text = prefs.PollingIntervalSeconds.ToString();

        // Load PAT from Credential Manager (show masked placeholder if it exists)
        var existingToken = CredentialService.LoadToken();
        if (!string.IsNullOrEmpty(existingToken))
        {
            PATBox.Password = existingToken;
            ShowStatus("Token loaded from Credential Manager.", success: true);
        }
    }

    // ── Test Connection button ────────────────────────────────────────────────

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        var pat      = PATBox.Password.Trim();
        var username = UsernameBox.Text.Trim();

        if (string.IsNullOrEmpty(pat) || string.IsNullOrEmpty(username))
        {
            ShowStatus("Please enter both a token and a username before testing.", success: false);
            return;
        }

        TestButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        ShowStatus("Testing connection…", success: null);

        var service = new GitHubService();
        var (success, message) = await service.TestConnectionAsync(pat, username);

        ShowStatus(success ? $"✅ {message}" : $"❌ {message}", success: success);

        TestButton.IsEnabled = true;
        SaveButton.IsEnabled = true;
    }

    // ── Save button ───────────────────────────────────────────────────────────

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var pat          = PATBox.Password.Trim();
        var username     = UsernameBox.Text.Trim();
        var intervalText = IntervalBox.Text.Trim();

        // ── Validation ──────────────────────────────────────────────────────
        if (string.IsNullOrEmpty(pat))
        {
            ShowStatus("Token cannot be empty.", success: false);
            PATBox.Focus();
            return;
        }

        if (string.IsNullOrEmpty(username))
        {
            ShowStatus("Username cannot be empty.", success: false);
            UsernameBox.Focus();
            return;
        }

        if (!int.TryParse(intervalText, out int interval) || interval < 30)
        {
            ShowStatus("Polling interval must be a number, minimum 30 seconds.", success: false);
            IntervalBox.Focus();
            return;
        }

        // ── Save PAT → Windows Credential Manager ───────────────────────────
        try
        {
            CredentialService.SaveToken(pat);
        }
        catch (Exception ex)
        {
            ShowStatus($"❌ Failed to save token: {ex.Message}", success: false);
            return;
        }

        // ── Save username + interval → prefs.json ───────────────────────────
        var prefs = PreferencesService.Load(); // preserve LastSeenEventId if it exists
        prefs.GitHubUsername         = username;
        prefs.PollingIntervalSeconds = interval;
        PreferencesService.Save(prefs);

        ShowStatus("✅ Settings saved.", success: true);

        // Close after a short delay so the user sees the confirmation
        Task.Delay(900).ContinueWith(_ => Dispatcher.Invoke(Close));
    }

    // ── Custom title bar: drag to move ───────────────────────────────────────
    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    // ── Custom close button ───────────────────────────────────────────────────
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // ── Helper: coloured status feedback ─────────────────────────────────────

    /// <param name="success">true = green, false = red, null = grey (in-progress)</param>
    private void ShowStatus(string message, bool? success)
    {
        StatusLabel.Text = message;
        StatusLabel.Foreground = success switch
        {
            true  => new SolidColorBrush(WpfColor.FromRgb(0x1A, 0x7F, 0x37)), // green
            false => new SolidColorBrush(WpfColor.FromRgb(0xCF, 0x22, 0x2E)), // red
            null  => new SolidColorBrush(WpfColor.FromRgb(0x66, 0x66, 0x66)), // grey
        };
    }
}
