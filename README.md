# Missed GitHub Updates

> A lightweight Windows system tray app that delivers native toast notifications for GitHub push events and pull requests — so you never miss a teammate's commit again...

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Language](https://img.shields.io/badge/language-C%23-green)
![Status](https://img.shields.io/badge/status-In%20Development-orange)

---

## The Problem

Developers working in teams need visibility into what their coworkers are doing on GitHub. Manually checking the site breaks focus. Email notifications are noisy and delayed. There's no native Windows desktop solution that quietly surfaces the updates that matter.

## The Solution

**Missed GitHub Updates** runs silently in your Windows system tray, polls the GitHub API every 60 seconds, and fires native Windows 10/11 toast pop-ups the moment someone on your team pushes code or opens a pull request.

No browser. No email. Just a quick pop-up and back to work.

---

## Features

- **System tray app** — runs silently in the background, zero UI clutter
- **Real-time-ish notifications** — polls every 60 seconds (within GitHub API limits)
- **Push notifications** — alerts you when someone pushes commits to any watched repo
- **Pull request notifications** — alerts you when a PR is opened, closed, or merged
- **Secure token storage** — your GitHub PAT is stored in Windows Credential Manager, never in plain text
- **Persistent state** — remembers the last seen event across restarts, no duplicate notifications
- **Configurable** — set your token, username, and polling interval from a clean settings window

---

## Tech Stack

| Technology | Role |
|---|---|
| C# 12 / .NET 8 | Primary language and runtime |
| WPF (Windows Presentation Foundation) | Desktop app shell and settings UI |
| [Octokit.net](https://github.com/octokit/octokit.net) | Official GitHub API client |
| Microsoft.Toolkit.Uwp.Notifications | Windows 10/11 native toast notifications |
| Windows Credential Manager | Encrypted PAT storage |
| JSON (Newtonsoft.Json) | Local preferences persistence |

---

## How It Works

```
App starts silently
       │
       ▼
Loads GitHub PAT from Windows Credential Manager
       │
       ├── No token? → Opens Settings window
       │
       ▼
Registers system tray icon
       │
       ▼
Starts background polling timer (every 60s)
       │
       ▼
Calls GitHub API: GET /users/{username}/events
       │
       ▼
Filters for PushEvent and PullRequestEvent
       │
       ▼
Compares against last-seen event ID
       │
       ▼
New events → fires Windows toast notification
       │
       ▼
Saves new last-seen event ID to prefs.json
```

---

## Getting Started

### Prerequisites

- Windows 10 (build 1903+) or Windows 11
- .NET 8 SDK (for building from source)
- A GitHub Personal Access Token with `repo` and `read:user` scopes

### Generate a GitHub PAT

1. Go to [GitHub → Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens)
2. Click **Generate new token (classic)**
3. Give it a name like `missed-github-updates`
4. Select scopes: `repo` and `read:user`
5. Copy the generated token — you'll need it during first setup

### Build from Source

```bash
# Clone the repo
git clone https://github.com/your-username/missed-github-updates.git
cd missed-github-updates

# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Run
dotnet run
```

### Publish as Self-Contained Executable

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output will be a single `.exe` in `bin/Release/net8.0/win-x64/publish/` — no .NET runtime required on the target machine.

---

## First Run

1. Launch `MissedGitHubUpdates.exe`
2. The app will detect no token is saved and automatically open the **Settings** window
3. Enter your **GitHub username** and **Personal Access Token**
4. Click **Test Connection** to verify
5. Click **Save** — the token is encrypted and stored in Windows Credential Manager
6. The app will minimize to the system tray and start polling

That's it. You'll see a toast notification the next time a push or PR happens.

---

## Settings Window

Right-click the tray icon → **Settings** to open the configuration window.

| Field | Description |
|---|---|
| GitHub PAT | Your Personal Access Token |
| GitHub Username | Your GitHub username (used for the events API) |
| Polling Interval | How often to check for new events (default: 60s) |

---

## Project Structure

```
MissedGitHubUpdates/
├── App.xaml                    # WPF entry point (no startup window)
├── App.xaml.cs                 # App lifecycle, tray icon initialization
├── Windows/
│   └── SettingsWindow.xaml     # Settings UI
│   └── SettingsWindow.xaml.cs  # Settings logic
├── Services/
│   └── GitHubService.cs        # Octokit polling + event parsing
│   └── CredentialService.cs    # Windows Credential Manager wrapper
│   └── NotificationService.cs  # Toast notification dispatch
│   └── PreferencesService.cs   # JSON prefs read/write
├── Models/
│   └── AppPreferences.cs       # Preferences data model
│   └── GitHubEventResult.cs    # Parsed event data model
├── Helpers/
│   └── TrayIconHelper.cs       # NotifyIcon setup and context menu
├── Assets/
│   └── tray-icon.ico           # System tray icon
└── MissedGitHubUpdates.csproj
```

---

## NuGet Dependencies

```xml
<PackageReference Include="Octokit" Version="9.1.0" />
<PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.1.3" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

---

## GitHub API Details

- **Endpoint used:** `GET /users/{username}/events`
- **Auth:** Bearer token (GitHub PAT via Octokit)
- **Poll rate:** Every 60 seconds = ~1,440 requests/day
- **Rate limit:** 5,000 requests/hour for authenticated users — well within budget
- **Events tracked:** `PushEvent`, `PullRequestEvent`

---

## Security

- Your GitHub PAT is **never** stored in plain text
- Token is saved to and retrieved from **Windows Credential Manager** only
- The token never appears in logs, config files, or error messages
- Minimum required scopes: `repo`, `read:user`

---

## Roadmap

- [x] Core polling engine
- [x] PushEvent notifications
- [x] PullRequestEvent notifications
- [x] Secure credential storage
- [ ] Notification history window
- [ ] Per-repo filtering
- [ ] Additional event types (issues, reviews, CI status)
- [ ] Auto-start on Windows login
- [ ] Multi-account support

---

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you'd like to change.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes (`git commit -m 'Add your feature'`)
4. Push to the branch (`git push origin feature/your-feature`)
5. Open a Pull Request

---

## License

[MIT](LICENSE)

---

*Built with C# and .NET 8 — because developers deserve better tooling.*
