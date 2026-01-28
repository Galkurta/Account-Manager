# Roblox Account Manager (RAM)

A comprehensive, open-source tool for managing multiple Roblox accounts, auto-rejoining games, browsing servers, and handling multi-instance launching with ease. Built with modern WPF and Fluent Design.

![License](https://img.shields.io/badge/license-MIT-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## ✨ Features

- **Multi-Account Manager**: Securely store and switch between unlimited Roblox accounts. Cookies are encrypted locally.
- **Auto Rejoiner**: Automatically detects disconnects and rejoins your last game.
- **Server Browser**: Browse public servers for any game, view player counts, fps, and ping.
- **Multi-Instance**: Launch multiple accounts into the same or different games simultaneously.
- **Integrated Browser**: Isolated WebView2 browser for each account to safely browse Roblox without cookie conflicts.
- **Friend Manager**: Mass add friends or manage requests across accounts.
- **Exploit Status**: Check the status of popular executors (updated dynamically).
- **Version Manager**: Manage Roblox versions and Bloxstrap configurations.
- **Modern UI**: Sleek, responsive Fluent Design interface using `Wpf.Ui`.

## 🚀 Getting Started

### Prerequisites

- Windows 10/11
- [.NET 8.0 Runtime (Desktop)](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (Usually pre-installed on Windows 11)

### Installation

1. Download the latest release from the [Releases](https://github.com/Galkurta/Account-Manager/releases) page.
2. Run the installer (`RobloxAccountManager_Setup.exe`) or extract the portable zip.
3. Launch `RobloxAccountManager.exe`.

## 🛠️ Building from Source

Requirements:

- Visual Studio 2022
- .NET 8.0 SDK

1. Clone the repository:
   ```bash
   git clone https://github.com/Galkurta/Account-Manager.git
   ```
2. Open `RobloxAccountManager.sln` in Visual Studio.
3. Restore NuGet packages.
4. Build the solution (Release configuration recommended).

## 📖 Usage

### Adding Accounts

1. Go to the **Accounts** tab.
2. Click **Add Account**.
3. You can either log in via the embedded browser or parse a `.ROBLOSECURITY` cookie directly.

### Auto Rejoin

1. Navigate to **Auto Join**.
2. Select the accounts you want to monitor.
3. Click **Start Monitoring**.
4. The tool will check presence status and relaunch if a disconnect is detected.

### Server Browser

1. Go to **Browser** or **Explore**.
2. Enter a Place ID.
3. Select a server to join.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⚠️ Disclaimer

This software is not affiliated with, maintained, authorized, endorsed, or sponsored by Roblox Corporation. Use at your own risk.
