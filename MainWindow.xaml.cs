using System;
using System.Diagnostics;
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
            Log("Dopple_OS initialized. Ready for lobotomy.");
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
            _trayIcon.Text = "DoppleClient — BIO-SECURE ☣";
        }

        private void SetTrayRed()
        {
            _trayIcon.Text = "DoppleClient — ⚠ BREACH DETECTED";
            _trayIcon.ShowBalloonTip(
                5000,
                "☣ DoppleClient",
                "Breach detected! Telemetry re-enabled. Auto-purging...",
                Forms.ToolTipIcon.Warning
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
                CheckSystemStatus();
                Log("=== PURGE COMPLETE. THIS IS YOUR COMPUTER NOW. ===");
                WpfMessageBox.Show("☣ PURGE COMPLETE ☣\n\nMicrosoft has been evicted.\nThis machine belongs to you.");
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
            const string ps = @"
                $services = @(
                    'DiagTrack','dmwappushservice','PcaSvc','SysMain',
                    'WSearch','RetailDemo','MapsBroker','lfsvc',
                    'SharedAccess','TrkWks','WbioSrvc','wisvc'
                )
                foreach ($svc in $services) {
                    try {
                        Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
                        Set-Service -Name $svc -StartupType Disabled -ErrorAction SilentlyContinue
                    } catch {}
                }";
            RunElevatedPowerShell($"-Command \"{ps}\"");
        }

        private void NukeScheduledTasks()
        {
            Log("Nuking scheduled spy tasks...");
            const string ps = @"
                $tasks = @(
                    '\Microsoft\Windows\Customer Experience Improvement Program\Consolidator',
                    '\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask',
                    '\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip',
                    '\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser',
                    '\Microsoft\Windows\Application Experience\ProgramDataUpdater',
                    '\Microsoft\Windows\Application Experience\StartupAppTask',
                    '\Microsoft\Windows\Autochk\Proxy',
                    '\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector',
                    '\Microsoft\Windows\Feedback\Siuf\DmClient',
                    '\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload',
                    '\Microsoft\Windows\Windows Error Reporting\QueueReporting',
                    '\Microsoft\Windows\Device Information\Device',
                    '\Microsoft\Windows\Device Information\Device User'
                )
                foreach ($task in $tasks) {
                    try {
                        Disable-ScheduledTask -TaskName $task -ErrorAction SilentlyContinue
                    } catch {}
                }";
            RunElevatedPowerShell($"-Command \"{ps}\"");
        }

        private void HammerRegistry()
        {
            Log("Hammering telemetry registry keys...");
            const string ps = @"
                Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection' -Name 'AllowTelemetry' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection' -Name 'AllowTelemetry' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo' -Name 'Enabled' -Value 0 -Type DWord -Force
                New-Item -Path 'HKCU:\SOFTWARE\Microsoft\Siuf\Rules' -Force | Out-Null
                Set-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Siuf\Rules' -Name 'NumberOfSIUFInPeriod' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name 'EnableActivityFeed' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name 'PublishUserActivities' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'Start_TrackProgs' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location' -Name 'Value' -Value 'Deny' -Type String -Force
                Set-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy' -Name 'HasAccepted' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager' -Name 'SubscribedContent-338389Enabled' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager' -Name 'SubscribedContent-353694Enabled' -Value 0 -Type DWord -Force
                Set-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager' -Name 'SilentInstalledAppsEnabled' -Value 0 -Type DWord -Force";
            RunElevatedPowerShell($"-Command \"{ps}\"");
        }

        private void BlockMicrosoftFirewall()
        {
            Log("Building Microsoft firewall wall...");
            const string ps = @"
                $ranges = @(
                    '13.64.0.0/11','13.96.0.0/13','20.0.0.0/8',
                    '40.64.0.0/10','52.0.0.0/8','104.40.0.0/13',
                    '157.54.0.0/15','191.232.0.0/13','207.46.0.0/16',
                    '135.233.0.0/16'
                )
                Remove-NetFirewallRule -DisplayName 'DOPPLE - Block Microsoft' -ErrorAction SilentlyContinue
                foreach ($range in $ranges) {
                    New-NetFirewallRule `
                        -DisplayName 'DOPPLE - Block Microsoft' `
                        -Direction Outbound `
                        -Action Block `
                        -RemoteAddress $range `
                        -Profile Any `
                        -ErrorAction SilentlyContinue
                }";
            RunElevatedPowerShell($"-Command \"{ps}\"");
        }

        private void BlockNetworkTracking()
        {
            Log("Updating hosts file sinkhole...");
            const string psCommand = @"
                $path = 'C:\Windows\System32\drivers\etc\hosts'
                $marker = 'DOPPLE SHIELD BLOCKLIST'
                if (-not (Select-String -Path $path -Pattern $marker -Quiet)) {
                    $block = @'
# DOPPLE SHIELD BLOCKLIST
0.0.0.0 telemetry.microsoft.com
0.0.0.0 vortex.data.microsoft.com
0.0.0.0 watson.telemetry.microsoft.com
0.0.0.0 onedscolprdwus18.westus.cloudapp.azure.com
0.0.0.0 onedsblobvmssprdcus03.centralus.cloudapp.azure.com
0.0.0.0 settings-win.data.microsoft.com
0.0.0.0 v10.events.data.microsoft.com
0.0.0.0 v20.events.data.microsoft.com
0.0.0.0 data.microsoft.com
0.0.0.0 msftconnecttest.com
0.0.0.0 activity.windows.com
0.0.0.0 browser.pipe.aria.microsoft.com
0.0.0.0 self.events.data.microsoft.com
'@
                    Add-Content -Path $path -Value $block -Encoding UTF8
                    ipconfig /flushdns
                }";
            RunElevatedPowerShell($"-Command \"{psCommand}\"");
            Log("Network moat established.");
        }

        private void DisableRemoteAccess()
        {
            Log("Sealing remote access vectors...");
            const string ps = @"
                Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server' -Name 'fDenyTSConnections' -Value 1 -Type DWord -Force
                Disable-NetFirewallRule -DisplayGroup 'Remote Desktop' -ErrorAction SilentlyContinue
                Stop-Service -Name 'RemoteRegistry' -Force -ErrorAction SilentlyContinue
                Set-Service -Name 'RemoteRegistry' -StartupType Disabled -ErrorAction SilentlyContinue
                Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Remote Assistance' -Name 'fAllowToGetHelp' -Value 0 -Type DWord -Force
                Stop-Service -Name 'WinRM' -Force -ErrorAction SilentlyContinue
                Set-Service -Name 'WinRM' -StartupType Disabled -ErrorAction SilentlyContinue";
            RunElevatedPowerShell($"-Command \"{ps}\"");
        }

        private void RemoveMicrosoftFirewallBlock()
        {
            Log("Removing Microsoft IP blocks...");
            const string ps = @"
                Remove-NetFirewallRule -DisplayName 'DOPPLE - Block Microsoft' -ErrorAction SilentlyContinue";
            RunElevatedPowerShell($"-Command \"{ps}\"");
        }

        private void ReenableWindowsUpdate()
        {
            Log("Re-enabling Windows Update services...");
            const string ps = @"
                $services = @('wuauserv', 'bits', 'cryptsvc', 'msiserver')
                foreach ($svc in $services) {
                    try {
                        Set-Service -Name $svc -StartupType Manual -ErrorAction SilentlyContinue
                        Start-Service -Name $svc -ErrorAction SilentlyContinue
                    } catch {}
                }";
            RunElevatedPowerShell($"-Command \"{ps}\"");
        }
private void About_Click(object sender, RoutedEventArgs e)
{
    WpfMessageBox.Show(
        "☣ DOPPLE CLIENT v1.0 ☣" +
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
        "\n\n Stay dark. Stay free." +
        "\n\n━━━━━━━━━━━━━━━━━━━━━━━━",
        "About DoppleClient",
        System.Windows.MessageBoxButton.OK
    );
}
        private static void RunElevatedPowerShell(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = arguments,
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start PowerShell process.");

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException(
                    $"PowerShell exited with code {process.ExitCode}.");
        }
    }
}