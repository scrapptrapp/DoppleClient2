# ☣ DoppleClient v1.7

> **Your computer. Not Microsoft's. Not anyone else's. YOURS.**

![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Version](https://img.shields.io/badge/version-1.0-red)

---

## What is DoppleClient?

DoppleClient is a free, open source Windows privacy tool built out of pure frustration with Microsoft's relentless data collection, telemetry, and their push toward mandatory ID verification just to use an operating system you already paid for.

This is your computer. DoppleClient makes sure it stays that way.

---

## Features

- **☣ Dopple Shield** — Kills the DiagTrack telemetry service and sinkholes known Microsoft tracking domains in your hosts file
- **☣ Purge System Telemetry** — Full system lobotomy:
  - Kills and permanently disables 12 telemetry services
  - Nukes 13 Microsoft spy scheduled tasks
  - Hammers telemetry registry keys to 0
  - Blocks Microsoft IP ranges via Windows Firewall
  - Disables Remote Desktop, Remote Registry, Remote Assistance, and WinRM
- **✓ Restore Windows Update** — Temporarily lifts the firewall block so you can update, then re-run Purge to re-seal
- **☣ Background Watchdog** — Runs silently in your system tray, checks every 5 minutes, and auto-purges if Microsoft tries to re-enable anything
- **Launches with Windows** — Registers itself at startup so you're always protected

---

## Why I Built This

Microsoft has been treating Windows users like products for years — collecting data in the background, pushing ads into the Start menu, and now requiring ID verification just to use features of an OS people already own.

I built DoppleClient because I wanted my laptop to be MY laptop. Not a Microsoft data collection terminal. Not a government-accessible machine. Mine.

This tool is free. It will always be free. Use it, share it, do whatever you want with it.

---

## How to Use

### Requirements
- Windows 10 or Windows 11
- **Must be run as Administrator** (right-click → Run as administrator)

### Steps
1. Download `DoppleClient.exe` from the [Releases](../../releases) page
2. Right-click it → **Run as administrator**
3. Hit **[ INITIALIZE DOPPLE SHIELD ]** first
4. Then hit **[ PURGE SYSTEM TELEMETRY ]** for the full lockdown
5. DoppleClient will minimize to your system tray and watch in the background

### To update Windows
1. Click **[ RESTORE WINDOWS UPDATE ]**
2. Run Windows Update manually
3. Hit **[ PURGE SYSTEM TELEMETRY ]** again to re-seal

---

## ⚠️ Antivirus Warning

Windows Defender and other antivirus software **will likely flag this as a false positive.**

This happens because DoppleClient:
- Requests administrator elevation
- Modifies Windows Firewall rules
- Edits registry keys
- Stops and disables system services

**This is expected behavior for a privacy tool.** The full source code is available right here on GitHub so you can read every single line before running it. Nothing is hidden.

To run it anyway:
- Windows Defender → Allow on device
- Or add an exclusion for the file

---

## What Gets Blocked / Disabled

### Services Disabled
| Service | Purpose |
|---|---|
| DiagTrack | Connected User Experiences and Telemetry |
| dmwappushservice | WAP Push Message Routing |
| PcaSvc | Program Compatibility Assistant |
| SysMain | Superfetch / usage data |
| WSearch | Windows Search (sends query data) |
| RetailDemo | Retail demo telemetry |
| MapsBroker | Downloaded Maps Manager |
| lfsvc | Geolocation Service |
| TrkWks | Distributed Link Tracking Client |
| WbioSrvc | Windows Biometric Service |
| wisvc | Windows Insider Service |

### Scheduled Tasks Nuked
- Customer Experience Improvement Program (all)
- Application Experience tasks
- Disk Diagnostic data collector
- Feedback / SIUF tasks
- Windows Error Reporting
- Device Information collectors

### Firewall IP Ranges Blocked
Microsoft ASN 8075 ranges including `20.0.0.0/8`, `52.0.0.0/8`, and more.

### Registry Keys Hammered
- Telemetry level → 0
- Advertising ID → disabled
- Activity history → disabled
- Location tracking → denied
- Cloud speech recognition → disabled
- Windows tips and suggestions → disabled
- App launch tracking → disabled

---

## Building from Source

```bash
git clone https://github.com/scrapptrapp/DoppleClient2.git
cd DoppleClient2
dotnet build
```

To publish a single self-contained exe:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## License

MIT — free forever. Do whatever you want with it.

---

> *Stay dark. Stay free. ☣*
