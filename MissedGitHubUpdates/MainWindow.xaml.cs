using System.Windows;

namespace MissedGitHubUpdates;

/// <summary>
/// Hidden host window — keeps the WPF message loop alive.
/// All user-facing UI is handled via the system tray icon in App.xaml.cs.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
