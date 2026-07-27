# Product Requirements Document (PRD)
## Missed GitHub Updates — Windows System Tray Notifier

**Version:** 1.0  
**Date:** July 27, 2026  
**Status:** Draft  

---

## 1. Overview

### 1.1 Problem Statement
Developers working in teams need to know when coworkers push code or open pull requests. Currently, they must either manually check GitHub or rely on email alerts — both of which cause missed updates or broken focus.

### 1.2 Solution
A lightweight Windows system tray application that polls the GitHub API and surfaces activity (pushes, pull requests) as native Windows toast notifications — no browser, no email, no context switching.

### 1.3 Goal
Deliver a silent background app that keeps developers aware of team GitHub activity in real time, with zero friction.

---

## 2. Target Users

- Software developers working on shared GitHub repositories
- Team leads who want visibility into active development activity
- Any Windows user collaborating on GitHub projects

---

## 3. Scope

### In Scope
- Monitor GitHub push events (`PushEvent`)
- Monitor GitHub pull request events (`PullRequestEvent`)
- Windows native toast notifications for each event
- Secure GitHub Personal Access Token (PAT) storage
- Settings window for token configuration
- System tray icon with context menu
- Persistent state (last-seen event ID) across app restarts

### Out of Scope (v1.0)
- macOS / Linux support
- GitHub issue comments, reviews, or other event types
- Webhook-based real-time delivery (requires public server)
- Multi-account GitHub support
- In-app notification history log

---

## 4. Functional Requirements

### FR-01: System Tray Presence
- The app must launch silently with no visible main window
- A system tray icon must appear in the Windows taskbar notification area
- Right-clicking the icon must show a context menu with:
  - **Settings** — opens the Settings window
  - **Quit** — exits the application cleanly

### FR-02: Settings Window
- A Settings window must allow the user to:
  - Enter a GitHub Personal Access Token (PAT)
  - Save the token securely
  - Optionally test the token connection before saving
- The window must show feedback (success/error) after a save attempt
- The window must be accessible from the tray context menu

### FR-03: Secure Token Storage
- The GitHub PAT must be stored using **Windows Credential Manager**
- The token must never be written to plain text files or app config files
- On app start, the token must be retrieved from Credential Manager automatically

### FR-04: GitHub Polling Engine
- A background timer must poll the GitHub API every **60–120 seconds**
- The endpoint used: `GET /users/{username}/events`
- Polling must be authenticated using the stored PAT via Octokit
- The app must track the **ID of the last processed event** in a local JSON preferences file
- On each poll, only events **newer than the last-seen ID** must be processed
- The last-seen event ID must be persisted across app restarts

### FR-05: Event Filtering
- The app must filter the event stream for:
  - `PushEvent` — a user pushed commits to a branch
  - `PullRequestEvent` — a PR was opened, closed, or merged
- All other event types must be silently ignored

### FR-06: Windows Toast Notifications
- For each matching event, a native Windows 10/11 toast notification must appear
- **PushEvent notification must include:**
  - Repository name
  - Actor (who pushed)
  - Branch name
  - Number of commits
- **PullRequestEvent notification must include:**
  - Repository name
  - Actor (who opened/closed/merged)
  - PR title
  - PR action (opened / closed / merged)
- Clicking the notification should open the relevant GitHub URL in the default browser

### FR-07: Error Handling
- If the token is invalid or expired, the app must show a tray balloon tip or toast indicating authentication failure
- If the GitHub API is unreachable (no internet), polling must silently retry on the next interval without crashing
- Rate limit errors (HTTP 403/429) must be handled gracefully by backing off

---

## 5. Non-Functional Requirements

### NFR-01: Performance
- The app must consume less than 50 MB of RAM during normal operation
- CPU usage must remain near 0% between polling intervals

### NFR-02: Reliability
- The app must recover automatically from transient network errors
- The last-seen event ID must be saved immediately after each successful poll

### NFR-03: Security
- The PAT must never appear in logs, error messages, or config files
- Only the minimum required GitHub OAuth scopes must be requested (`repo`, `read:user`)

### NFR-04: Usability
- First-time setup must take under 2 minutes (install → enter token → running)
- Notifications must be non-intrusive and auto-dismiss

### NFR-05: Compatibility
- Must run on Windows 10 (build 1903+) and Windows 11
- Target runtime: .NET 8

---

## 6. Technical Architecture

### 6.1 Tech Stack

| Layer | Technology | Purpose |
|---|---|---|
| Language | C# 12 | Primary development language |
| Framework | .NET 8 WPF | Desktop app shell and UI rendering |
| GitHub API | Octokit.net | Typed GitHub API client |
| Notifications | Microsoft.Toolkit.Uwp.Notifications | Windows 10/11 toast pop-ups |
| Secure Storage | Windows Credential Manager API | Encrypted PAT storage |
| Preferences | Local JSON file (`prefs.json`) | Last-seen event ID, settings |

### 6.2 Project Structure

```
MissedGitHubUpdates/
├── App.xaml                    # WPF app entry point (no startup window)
├── App.xaml.cs                 # App lifecycle, tray icon init
├── MainWindow.xaml             # Hidden (used as WPF host only)
├── Windows/
│   └── SettingsWindow.xaml     # Settings UI
│   └── SettingsWindow.xaml.cs
├── Services/
│   └── GitHubService.cs        # Octokit polling logic
│   └── CredentialService.cs    # Windows Credential Manager read/write
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

### 6.3 Architecture Flow

```
App Start
   │
   ▼
Load PAT from Credential Manager
   │
   ├─ No token found → Open Settings Window
   │
   ▼
Initialize System Tray Icon
   │
   ▼
Start Background Polling Timer (60s interval)
   │
   ▼
[Every 60s] Call GitHub /users/{username}/events
   │
   ▼
Filter: PushEvent | PullRequestEvent
   │
   ▼
Compare against last-seen event ID
   │
   ▼
New events found → Dispatch Toast Notifications
   │
   ▼
Save new last-seen event ID to prefs.json
```

### 6.4 GitHub API Details

- **Endpoint:** `GET https://api.github.com/users/{username}/events`
- **Auth:** Bearer token via Octokit `Credentials` object
- **Response:** Array of event objects, sorted newest-first
- **Rate Limit:** 5,000 requests/hour for authenticated users (polling at 60s = ~1,440 requests/day — well within limits)
- **Pagination:** First page (30 events) is sufficient for a 60s polling window

---

## 7. Data Storage

### Windows Credential Manager Entry
```
Target:  MissedGitHubUpdates/GitHubPAT
User:    github
Secret:  <the PAT>
```

### prefs.json Schema
```json
{
  "lastSeenEventId": "12345678901",
  "githubUsername": "octocat",
  "pollingIntervalSeconds": 60
}
```

---

## 8. UI Specifications

### System Tray Icon
- 16x16 and 32x32 ICO file (GitHub-inspired icon)
- Tooltip on hover: "Missed GitHub Updates — Running"

### Settings Window
```
┌─────────────────────────────────────────┐
│  ⚙  Settings — Missed GitHub Updates   │
├─────────────────────────────────────────┤
│                                         │
│  GitHub Personal Access Token           │
│  ┌───────────────────────────────────┐  │
│  │  ghp_xxxxxxxxxxxxxxxxxxxx         │  │
│  └───────────────────────────────────┘  │
│                                         │
│  GitHub Username                        │
│  ┌───────────────────────────────────┐  │
│  │  octocat                          │  │
│  └───────────────────────────────────┘  │
│                                         │
│  Polling Interval (seconds)             │
│  ┌──────┐                               │
│  │  60  │                               │
│  └──────┘                               │
│                                         │
│         [Test Connection]  [Save]       │
│                                         │
│  ✅ Connected as: octocat               │
└─────────────────────────────────────────┘
```

### Toast Notification — PushEvent
```
┌──────────────────────────────────────┐
│ 🔔 GitHub Push                       │
│ johndoe pushed 3 commits             │
│ → octocat/my-repo (main)             │
└──────────────────────────────────────┘
```

### Toast Notification — PullRequestEvent
```
┌──────────────────────────────────────┐
│ 🔔 GitHub Pull Request               │
│ johndoe opened a PR                  │
│ "Fix login bug" → octocat/my-repo    │
└──────────────────────────────────────┘
```

---

## 9. Build & Release

### Development Prerequisites
- Visual Studio 2022 or Rider
- .NET 8 SDK
- Windows 10/11 machine (WPF is Windows-only)

### NuGet Packages
```xml
<PackageReference Include="Octokit" Version="9.1.0" />
<PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.1.3" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### Build Command
```bash
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained true
```

### Distribution
- Single-file self-contained `.exe` (no .NET runtime install required on target machine)
- Optional: NSIS or WiX installer for proper Windows installation with startup entry

---

## 10. Success Metrics

| Metric | Target |
|---|---|
| Notification delivery delay | < 2 minutes from GitHub event |
| False notifications (duplicate events) | 0 |
| App crash rate | < 1 per week of use |
| Memory usage (idle) | < 50 MB |
| Setup time (first run) | < 2 minutes |

---

## 11. Future Roadmap (v2.0+)

- Support for additional event types (issue comments, reviews, CI/CD status)
- Notification history window (view past 50 events)
- Per-repo filtering (watch only specific repos)
- Multi-account GitHub support
- Auto-start on Windows login via registry entry
- Dark/light mode Settings window

---

*Document Owner: Development Team*  
*Last Updated: July 27, 2026*
