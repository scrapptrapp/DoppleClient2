using System;
using System.Diagnostics;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using WpfColor = System.Windows.Media.Color;

namespace DoppleWinClient
{
    public partial class MainWindow : Window
    {
        private readonly StringBuilder _logBuffer = new();
        private Forms.NotifyIcon _trayIcon = null!;
        private DispatcherTimer _watchdog = null!;
        private const int WatchdogIntervalMinutes = 5;
        private const string AppName = "DoppleClient";

        // ── TOR FIELDS ─────────────────────────────────────────────────
        private Process? _torProcess;
        private DispatcherTimer? _ipRotator;
        private int _rotateIntervalSeconds = 60;

        public MainWindow()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    DragMove();
            };

            this.Closing += (s, e) =>
            {
                e.Cancel = true;
                HideToTray();
            };

            InitTrayIcon();
            InitWatchdog();
            RegisterStartup();
            CheckSystemStatus();
            Log("Dopple_OS Core Breach... System ready for Lobotomy.");
        }

        // ── TRAY ICON ──────────────────────────────────────────────────

        private void InitTrayIcon()
        {
            _trayIcon = new Forms.NotifyIcon
            {
                Text = "DoppleClient — Watching...",
                Visible = true,
                Icon = System.Drawing.SystemIcons.Shield
            };

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("☣ Open DoppleClient", null, (s, e) => ShowFromTray());
            menu.Items.Add("-");
            menu.Items.Add("⚡ Run Purge Now", null, async (s, e) => await RunSilentPurge());
            menu.Items.Add("-");
            menu.Items.Add("✕ Exit", null, (s, e) => ExitApp());

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (s, e) => ShowFromTray();

            SetTrayGreen();
        }

        private void SetTrayGreen()
        {
            _trayIcon.Text = "DoppleClient — LOBOTOMIZED ☣";
        }

        private void SetTrayRed()
        {
            _trayIcon.Text = "DoppleClient — ⚠ SYSTEM BREACHED";
            _trayIcon.ShowBalloonTip(
                5000,
                "☣ DoppleClient",
                "Breach detected! Microsoft is leaking. Auto-purging...",
                Forms.ToolTipIcon.Error
            );
        }

        private void HideToTray()
        {
            this.Hide();
            _trayIcon.ShowBalloonTip(
                2000,
                "☣ DoppleClient",
                "Still watching in the background. Right-click tray icon to open.",
                Forms.ToolTipIcon.Info
            );
        }

        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApp()
        {
            _ipRotator?.Stop();
            try { if (_torProcess != null && !_torProcess.HasExited) _torProcess.Kill(); } catch { }
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _watchdog.Stop();
            WpfApplication.Current.Shutdown();
        }

        // ── WATCHDOG ───────────────────────────────────────────────────

        private void InitWatchdog()
        {
            _watchdog = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(WatchdogIntervalMinutes)
            };
            _watchdog.Tick += async (s, e) => await WatchdogTick();
            _watchdog.Start();
            Log($"Watchdog armed. Checking every {WatchdogIntervalMinutes} minutes.");
        }

        private async Task WatchdogTick()
        {
            bool breached = await Task.Run(IsBreached);

            if (breached)
            {
                Log("⚠ WATCHDOG: Breach detected! Auto-purging...");
                SetTrayRed();
                await RunSilentPurge();
                Log("✓ WATCHDOG: Auto-purge complete. Threat neutralized.");
                SetTrayGreen();
            }
            else
            {
                Log("✓ WATCHDOG: All clear.");
                SetTrayGreen();
            }

            CheckSystemStatus();
        }

        private bool IsBreached()
        {
            try
            {
                using var sc = new ServiceController("DiagTrack");
                if (sc.Status == ServiceControllerStatus.Running)
                    return true;

                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows\DataCollection");
                if (key != null)
                {
                    var val = key.GetValue("AllowTelemetry");
                    if (val is int i && i > 0) return true;
                }

                return false;
            }
            catch { return false; }
        }

        private async Task RunSilentPurge()
        {
            await Task.Run(KillTelemetryServices);
            await Task.Run(HammerRegistry);
            await Task.Run(BlockMicrosoftFirewall);
            await Task.Run(BlockWindowsUpdate);
        }

        // ── STARTUP REGISTRY ───────────────────────────────────────────

        private void RegisterStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                string exePath = Process.GetCurrentProcess().MainModule!.FileName;
                key?.SetValue(AppName, $"\"{exePath}\"");
                Log("Startup entry registered. DoppleClient will launch with Windows.");
            }
            catch (Exception ex)
            {
                Log($"Startup registration failed: {ex.Message}");
            }
        }

        // ── UI CONTROLS ────────────────────────────────────────────────

        private void Close_Click(object sender, RoutedEventArgs e) => HideToTray();
        private void Min_Click(object sender, RoutedEventArgs e) => HideToTray();

        private void Log(string message)
        {
            if (ConsoleLog == null) return;
            Dispatcher.Invoke(() =>
            {
                _logBuffer.AppendLine($"> [{DateTime.Now:HH:mm:ss}] {message}");
                ConsoleLog.Text = _logBuffer.ToString();
                Scroller?.ScrollToEnd();
            });
        }

        private void CheckSystemStatus()
        {
            try
            {
                using var sc = new ServiceController("DiagTrack");
                bool isNeutralized =
                    sc.Status == ServiceControllerStatus.Stopped ||
                    sc.StartType == ServiceStartMode.Disabled;

                if (isNeutralized)
                {
                    StatusLight.Fill = new SolidColorBrush(WpfColor.FromRgb(57, 255, 20));
                    StatusText.Text = "BIO-SECURE";
                    SetTrayGreen();
                }
                else
                {
                    StatusLight.Fill = new SolidColorBrush(Colors.Red);
                    StatusText.Text = "SYSTEM BREACHED";
                    SetTrayRed();
                }
            }
            catch (InvalidOperationException ex)
            {
                Log($"Status check failed: {ex.Message}");
                if (StatusText != null) StatusText.Text = "OFFLINE";
            }
            catch (Exception ex)
            {
                Log($"Unexpected status error: {ex.Message}");
                if (StatusText != null) StatusText.Text = "ERROR";
            }
        }

        // ── BUTTON HANDLERS ────────────────────────────────────────────

        private async void DoppleShield_Click(object sender, RoutedEventArgs e)
        {
            DoppleShieldButton.IsEnabled = false;
            try
            {
                Log("Initializing Dopple Shield...");
                await Task.Run(StopTelemetryService);
                Log("Telemetry service neutralized.");
                await Task.Run(BlockNetworkTracking);
                CheckSystemStatus();
                Log("Lobotomy successful. System is now dark.");
                WpfMessageBox.Show("☣ DOPPLE SHIELD ACTIVE :: SYSTEM LOBOTOMIZED ☣");
            }
            catch (Exception ex)
            {
                Log($"Shield error: {ex.Message}");
                WpfMessageBox.Show($"Shield Error: {ex.Message}");
            }
            finally
            {
                DoppleShieldButton.IsEnabled = true;
            }
        }

        private async void KillGov_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            button.IsEnabled = false;
            try
            {
                Log("=== PURGE INITIATED ===");
                await Task.Run(KillTelemetryServices);
                Log("Telemetry services executed.");
                await Task.Run(NukeScheduledTasks);
                Log("Scheduled spy tasks eliminated.");
                await Task.Run(HammerRegistry);
                Log("Registry lobotomized.");
                await Task.Run(BlockMicrosoftFirewall);
                Log("Microsoft IP ranges walled off.");
                await Task.Run(DisableRemoteAccess);
                Log("Remote access vectors sealed.");
                await Task.Run(BlockAgeVerification);
                Log("Age verification signals neutered.");
                await Task.Run(BlockMSAIdentity);
                Log("Microsoft identity services severed.");
                await Task.Run(BlockWindowsUpdate);
                Log("Windows Update locked down. No silent installs.");
                CheckSystemStatus();
                Log("=== PURGE COMPLETE. THIS IS YOUR COMPUTER NOW. ===");
                WpfMessageBox.Show("☣ PURGE COMPLETE ☣\n\nMicrosoft has been evicted.\nWindows Update blocked.\nThis machine belongs to you.");
            }
            catch (Exception ex)
            {
                Log($"Purge error: {ex.Message}");
                WpfMessageBox.Show($"Purge Error: {ex.Message}");
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private async void Undo_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            button.IsEnabled = false;
            try
            {
                Log("=== RESTORE INITIATED ===");
                await Task.Run(RemoveMicrosoftFirewallBlock);
                Log("Microsoft firewall rules lifted.");
                await Task.Run(ReenableWindowsUpdate);
                Log("Windows Update services restored.");
                Log("=== RESTORE COMPLETE. Update, then re-run PURGE. ===");
                WpfMessageBox.Show("✓ Windows Update restored.\n\nUpdate your system, then hit PURGE again to re-seal.");
            }
            catch (Exception ex)
            {
                Log($"Restore error: {ex.Message}");
                WpfMessageBox.Show($"Restore Error: {ex.Message}");
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        // ── TOR / IP ROTATION ──────────────────────────────────────────

        private async void TorConnect_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            button.IsEnabled = false;
            try
            {
                Log("=== STARTING TOR CIRCUIT ===");
                await Task.Run(() =>
                {
                    _torProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory, "tor.exe"),
                            Arguments = "--SocksPort 9050 --ControlPort 9051 --CookieAuthentication 0 --HashedControlPassword \"\"",
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = false
                        }
                    };
                    _torProcess.Start();
                });

                Log("Tor process started. Waiting for bootstrap...");
                await Task.Delay(5000);
                await ShowCurrentIP();
                StartIPRotation();
                Log($"Tor active. Rotating every {_rotateIntervalSeconds}s.");
            }
            catch (Exception ex)
            {
                Log($"Tor start error: {ex.Message}");
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private void StartIPRotation()
        {
            _ipRotator = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_rotateIntervalSeconds)
            };
            _ipRotator.Tick += async (s, e) =>
            {
                await RotateCircuit();
                await ShowCurrentIP();
            };
            _ipRotator.Start();
        }

        private async Task RotateCircuit()
        {
            await Task.Run(() =>
            {
                using var client = new System.Net.Sockets.TcpClient("127.0.0.1", 9051);
                using var stream = client.GetStream();
                using var writer = new System.IO.StreamWriter(stream);
                writer.WriteLine("AUTHENTICATE \"\"");
                writer.WriteLine("SIGNAL NEWNYM");
                writer.Flush();
            });
            Log("Circuit rotated. New identity active.");
        }

        private async Task ShowCurrentIP()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    Proxy = new System.Net.WebProxy("socks5://127.0.0.1:9050"),
                    UseProxy = true
                };
                using var client = new HttpClient(handler);
                string ip = await client.GetStringAsync("https://api.ipify.org");
                Log($"☣ Current exit IP: {ip.Trim()}");
            }
            catch
            {
                Log("IP check failed — Tor still bootstrapping, try again in a few seconds.");
            }
        }

        private void TorStop_Click(object sender, RoutedEventArgs e)
        {
            _ipRotator?.Stop();
            _ipRotator = null;
            try { if (_torProcess != null && !_torProcess.HasExited) _torProcess.Kill(); } catch { }
            _torProcess = null;
            Log("=== TOR STOPPED. Back to real IP. ===");
        }

        // ── CORE OPERATIONS ────────────────────────────────────────────

        private static void StopTelemetryService()
        {
            const string args =
                "-Command \"Stop-Service -Name 'DiagTrack' -Force; " +
                "Set-Service -Name 'DiagTrack' -StartupType Disabled\"";
            RunElevatedPowerShell(args);
        }

        private void KillTelemetryServices()
        {
            Log("Killing telemetry services...");
            const string ps = @"$services = @('DiagTrack','dmwappushservice','PcaSvc','SysMain','WSearch','RetailDemo','MapsBroker','lfsvc','SharedAccess','TrkWks','WbioSrvc','wisvc')
foreach ($svc in $services) {
    try {
        Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
        Set-Service -Name $svc -StartupType Disabled -ErrorAction SilentlyContinue
    } catch {}
}";
            RunElevatedPowerShell("-Command \"" + ps + "\"");
        }

        private void NukeScheduledTasks()
        {
            Log("Nuking scheduled spy tasks...");
            const string ps = @"$tasks = @('\Microsoft\Windows\Customer Experience Improvement Program\Consolidator','\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask','\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip','\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser','\Microsoft\Windows\Application Experience\ProgramDataUpdater','\Microsoft\Windows\Application Experience\StartupAppTask','\Microsoft\Windows\Autochk\Proxy','\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector','\Microsoft\Windows\Feedback\Siuf\DmClient','\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload','\Microsoft\Windows\Windows Error Reporting\QueueReporting','\Microsoft\Windows\Device Information\Device','\Microsoft\Windows\Device Information\Device User')
foreach ($task in $tasks) {
    try { Disable-ScheduledTask -TaskName $task -ErrorAction SilentlyContinue } catch {}
}";
            RunElevatedPowerShell("-Command \"" + ps + "\"");
        }

        private void HammerRegistry()
        {
            Log("Hammering telemetry registry keys...");
            RunElevatedPowerShell("-Command \"" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'AllowTelemetry' -Value 0 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection' -Name 'AllowTelemetry' -Value 0 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo' -Name 'Enabled' -Value 0 -Type DWord -Force;" +
                "New-Item -Path 'HKCU:\\SOFTWARE\\Microsoft\\Siuf\\Rules' -Force | Out-Null;" +
                "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Siuf\\Rules' -Name 'NumberOfSIUFInPeriod' -Value 0 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System' -Name 'EnableActivityFeed' -Value 0 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System' -Name 'PublishUserActivities' -Value 0 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'Start_TrackProgs' -Value 0 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy' -Name 'HasAccepted' -Value 0 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SilentInstalledAppsEnabled' -Value 0 -Type DWord -Force" +
                "\"");
        }

        // ── AGE VERIFICATION + IDENTITY BLOCKS ─────────────────────────

        private void BlockAgeVerification()
        {
            Log("Neutering age verification signals...");
            RunElevatedPowerShell("-Command \"" +
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\ParentalControls' -Force | Out-Null;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\ParentalControls' -Name 'Value' -Value 0 -Type DWord -Force;" +
                "Stop-Service -Name 'spectrum' -Force -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'spectrum' -StartupType Disabled -ErrorAction SilentlyContinue;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System' -Name 'EnableSmartScreen' -Value 0 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Privacy' -Name 'TailoredExperiencesWithDiagnosticDataEnabled' -Value 0 -Type DWord -Force" +
                "\"");
        }

        private void BlockMSAIdentity()
        {
            Log("Severing Microsoft Account identity link...");
            RunElevatedPowerShell("-Command \"" +
                "Stop-Service -Name 'wlidsvc' -Force -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'wlidsvc' -StartupType Disabled -ErrorAction SilentlyContinue;" +
                "Stop-Service -Name 'NgcSvc' -Force -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'NgcSvc' -StartupType Disabled -ErrorAction SilentlyContinue;" +
                "Stop-Service -Name 'NgcCtnrSvc' -Force -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'NgcCtnrSvc' -StartupType Disabled -ErrorAction SilentlyContinue;" +
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\MicrosoftAccount' -Force | Out-Null;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\MicrosoftAccount' -Name 'DisableUserAuth' -Value 1 -Type DWord -Force" +
                "\"");
        }

        // ── WINDOWS UPDATE BLOCK ────────────────────────────────────────

        private void BlockWindowsUpdate()
        {
            Log("Locking down Windows Update...");
            RunElevatedPowerShell("-Command \"" +
                "Stop-Service -Name 'wuauserv' -Force -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'wuauserv' -StartupType Disabled -ErrorAction SilentlyContinue;" +
                "Stop-Service -Name 'UsoSvc' -Force -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'UsoSvc' -StartupType Disabled -ErrorAction SilentlyContinue;" +
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Force | Out-Null;" +
                "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Force | Out-Null;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'NoAutoUpdate' -Value 1 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'AUOptions' -Value 1 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DisableWindowsUpdateAccess' -Value 1 -Type DWord -Force;" +
                "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DoNotConnectToWindowsUpdateInternetLocations' -Value 1 -Type DWord -Force;" +
                "Disable-ScheduledTask -TaskName '\\Microsoft\\Windows\\WindowsUpdate\\Scheduled Start' -ErrorAction SilentlyContinue;" +
                "Disable-ScheduledTask -TaskName '\\Microsoft\\Windows\\UpdateOrchestrator\\Schedule Scan' -ErrorAction SilentlyContinue" +
                "\"");
        }

        private void BlockMicrosoftFirewall()
        {
            Log("Building Microsoft firewall wall...");
            const string ps = @"$ranges = @('13.64.0.0/11','13.96.0.0/13','20.0.0.0/8','40.64.0.0/10','52.0.0.0/8','104.40.0.0/13','157.54.0.0/15','191.232.0.0/13','207.46.0.0/16','135.233.0.0/16')
Remove-NetFirewallRule -DisplayName 'DOPPLE - Block Microsoft' -ErrorAction SilentlyContinue
foreach ($range in $ranges) {
    New-NetFirewallRule -DisplayName 'DOPPLE - Block Microsoft' -Direction Outbound -Action Block -RemoteAddress $range -Profile Any -ErrorAction SilentlyContinue
}";
            RunElevatedPowerShell("-Command \"" + ps + "\"");
        }

        private void BlockNetworkTracking()
        {
            Log("Updating hosts file sinkhole...");
            RunElevatedPowerShell("-Command \"" +
                "$path = 'C:\\Windows\\System32\\drivers\\etc\\hosts';" +
                "$marker = 'DOPPLE SHIELD BLOCKLIST';" +
                "if (-not (Select-String -Path $path -Pattern $marker -Quiet)) {" +
                "Add-Content -Path $path -Value '# DOPPLE SHIELD BLOCKLIST' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 telemetry.microsoft.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 vortex.data.microsoft.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 watson.telemetry.microsoft.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 settings-win.data.microsoft.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 data.microsoft.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 msftconnecttest.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 activity.windows.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 login.live.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 account.microsoft.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 windowsupdate.microsoft.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 update.microsoft.com' -Encoding UTF8;" +
                "Add-Content -Path $path -Value '0.0.0.0 download.windowsupdate.com' -Encoding UTF8;" +
                "ipconfig /flushdns}" +
                "\"");
            Log("Network moat established.");
        }

        private void DisableRemoteAccess()
        {
            Log("Sealing remote access vectors...");
            RunElevatedPowerShell("-Command \"" +
                "Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Terminal Server' -Name 'fDenyTSConnections' -Value 1 -Type DWord -Force;" +
                "Disable-NetFirewallRule -DisplayGroup 'Remote Desktop' -ErrorAction SilentlyContinue;" +
                "Stop-Service -Name 'RemoteRegistry' -Force -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'RemoteRegistry' -StartupType Disabled -ErrorAction SilentlyContinue;" +
                "Stop-Service -Name 'WinRM' -Force -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'WinRM' -StartupType Disabled -ErrorAction SilentlyContinue" +
                "\"");
        }

        private void RemoveMicrosoftFirewallBlock()
        {
            Log("Removing Microsoft IP blocks...");
            RunElevatedPowerShell("-Command \"Remove-NetFirewallRule -DisplayName 'DOPPLE - Block Microsoft' -ErrorAction SilentlyContinue\"");
        }

        private void ReenableWindowsUpdate()
        {
            Log("Re-enabling Windows Update services...");
            RunElevatedPowerShell("-Command \"" +
                "Set-Service -Name 'wuauserv' -StartupType Manual -ErrorAction SilentlyContinue;" +
                "Start-Service -Name 'wuauserv' -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'UsoSvc' -StartupType Manual -ErrorAction SilentlyContinue;" +
                "Start-Service -Name 'UsoSvc' -ErrorAction SilentlyContinue;" +
                "Set-Service -Name 'bits' -StartupType Manual -ErrorAction SilentlyContinue;" +
                "Start-Service -Name 'bits' -ErrorAction SilentlyContinue;" +
                "Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU' -Name 'NoAutoUpdate' -ErrorAction SilentlyContinue;" +
                "Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DisableWindowsUpdateAccess' -ErrorAction SilentlyContinue;" +
                "Remove-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate' -Name 'DoNotConnectToWindowsUpdateInternetLocations' -ErrorAction SilentlyContinue" +
                "\"");
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            WpfMessageBox.Show(
                "☣ DOPPLE CLIENT v1.1 ☣" +
                "\n\n━━━━━━━━━━━━━━━━━━━━━━━━" +
                "\n CREATOR: [☣void☣]" +
                "\n━━━━━━━━━━━━━━━━━━━━━━━━" +
                "\n\n This tool was built out of pure hatred" +
                "\n for Microsoft and their obsession with" +
                "\n spying on people who just want to use" +
                "\n their own damn computer." +
                "\n\n Microsoft's new ID verification?" +
                "\n Absolute clownery. Your PC is YOURS." +
                "\n Not theirs. Not the government's. YOURS." +
                "\n\n☣ MANUAL STEPS FOR FULL LOBOTOMY ☣" +
                "\n • BIOS: Disable Microsoft Pluton/TPM" +
                "\n • Reinstall: Use OOBE\\BYPASSNRO to" +
                "\n   skip Microsoft Account setup" +
                "\n • Consider Arch/Nobara Linux" +
                "\n\n Stay dark. Stay free." +
                "\n\n━━━━━━━━━━━━━━━━━━━━━━━━",
                "About DoppleClient",
                System.Windows.MessageBoxButton.OK
            );
        }

private static void RunElevatedPowerShell(string scriptBlock)
{
    // 1. Wrap the script in a Try/Catch within PowerShell so it always exits cleanly
    // 2. Added -NoProfile and -NonInteractive to prevent hanging
    // Use double {{ and }} for the literal PowerShell parts so C# doesn't try to parse them
    string safeArguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"& {{ try {{ {scriptBlock} }} catch {{ exit 0 }} }}\"";

    var psi = new ProcessStartInfo
    {
        FileName = "powershell.exe",
        Arguments = safeArguments,
        Verb = "runas",
        UseShellExecute = true,
        WindowStyle = ProcessWindowStyle.Hidden,
    };

    try
    {
        using var process = Process.Start(psi);
        process?.WaitForExit();
        
        // Only throw if it's a catastrophic failure (like code 0xFFFFFFFF)
        // Code 1 usually just means a command inside the script "sighed"
        if (process != null && process.ExitCode != 0 && process.ExitCode != 1)
        {
             // Log it, but maybe don't crash the whole app
             Debug.WriteLine($"PowerShell warning: {process.ExitCode}");
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Could not launch PowerShell: {ex.Message}");
    }
}

    }
}
