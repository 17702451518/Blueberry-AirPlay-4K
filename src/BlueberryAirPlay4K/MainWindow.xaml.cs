using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlueberryAirPlay4K;

public partial class MainWindow : Window
{
    private sealed record ReceiverProfile(string Id, string Title, string ReceiverName, string Arguments, string Summary);

    private static readonly IReadOnlyDictionary<string, ReceiverProfile> Profiles =
        new Dictionary<string, ReceiverProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["4k60-sync"] = new(
                "4k60-sync",
                "4K60 标准同步",
                "蓝莓投屏-4K60",
                "-n 蓝莓投屏-4K60 -nh -h265 -s 3840x2160@60 -fps 60 -vsync -nofreeze -FPSdata",
                "HEVC 4K60请求 · D3D11 · 时间戳音画同步"),
            ["4k60-low"] = new(
                "4k60-low",
                "4K60 低延迟互动",
                "蓝莓投屏-4K60-低延迟",
                "-n 蓝莓投屏-4K60-低延迟 -nh -h265 -s 3840x2160@60 -fps 60 -vsync no -nofreeze -FPSdata",
                "HEVC 4K60请求 · D3D11 · 到帧尽快显示"),
            ["4k30"] = new(
                "4k30",
                "4K30 稳定兼容",
                "蓝莓投屏-4K30",
                "-n 蓝莓投屏-4K30 -nh -h265 -s 3840x2160@60 -fps 30 -vsync -nofreeze -FPSdata",
                "HEVC 4K30请求 · D3D11 · 稳定优先"),
            ["2k60"] = new(
                "2k60",
                "2K60 中间档",
                "蓝莓投屏-2K60",
                "-n 蓝莓投屏-2K60 -nh -h265 -s 2560x1440@60 -fps 60 -vsync -nofreeze -FPSdata",
                "HEVC 2K60请求 · D3D11 · 中等网络负载"),
            ["1080p60"] = new(
                "1080p60",
                "1080P60 H.264回退",
                "蓝莓投屏-1080P60",
                "-n 蓝莓投屏-1080P60 -nh -s 1920x1080@60 -fps 60 -vsync -nofreeze -FPSdata",
                "H.264 1080P60请求 · D3D11 · 兼容排查"),
        };

    private readonly DispatcherTimer _statusTimer;
    private bool _loading = true;
    private bool _busy;
    private string _selectedProfileId = "4k60-sync";

    private string PackageRoot => AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    private string EnginePath => Path.Combine(PackageRoot, "uxplay-windows.exe");
    private static string UserConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "leapbtw", "uxplay-windows");
    private static string UserConfigPath => Path.Combine(UserConfigDirectory, "arguments.txt");

    public MainWindow()
    {
        InitializeComponent();
        SelectProfileFromExistingConfig();
        LoadAutostartState();
        _loading = false;
        UpdateSelectedProfileText();
        UpdateStatus();

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _statusTimer.Tick += (_, _) => UpdateStatus();
        _statusTimer.Start();
    }

    private ReceiverProfile SelectedProfile => Profiles[_selectedProfileId];

    private void Mode_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio && radio.Tag is string id && Profiles.ContainsKey(id))
        {
            _selectedProfileId = id;
            if (!_loading)
            {
                UpdateSelectedProfileText();
            }
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _busy = true;
        StartButton.IsEnabled = false;
        FooterText.Text = "正在应用配置并启动……";

        try
        {
            ValidatePackage();
            var foreign = FindForeignEngineProcesses();
            if (foreign.Count > 0)
            {
                MessageBox.Show(
                    "检测到另一个目录中的 uxplay-windows 正在运行。请先退出旧测试版，避免端口和Bonjour名称冲突。\n\n" +
                    string.Join("\n", foreign),
                    "需要先关闭旧接收器",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            await StopOurProcessesAsync();
            WriteSelectedConfiguration();
            ConfigureRenderer();

            Process.Start(new ProcessStartInfo
            {
                FileName = EnginePath,
                WorkingDirectory = PackageRoot,
                UseShellExecute = true,
            });

            await Task.Delay(1200);
            if (!IsOurEngineRunning())
            {
                throw new InvalidOperationException("接收核心启动后立即退出。请检查Windows安全提示或Bonjour服务状态。");
            }

            FooterText.Text = $"已启动：{SelectedProfile.ReceiverName}";
            UpdateStatus();
        }
        catch (Exception ex)
        {
            FooterText.Text = "启动失败";
            MessageBox.Show(ex.Message, "蓝莓 AirPlay 4K", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _busy = false;
            StartButton.IsEnabled = true;
        }
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            await StopOurProcessesAsync();
            FooterText.Text = "接收器已停止";
            UpdateStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "停止失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{PackageRoot}\"",
            UseShellExecute = true,
        });
    }

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        try
        {
            using var runKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (AutoStartCheckBox.IsChecked == true)
            {
                ValidatePackage();
                WriteSelectedConfiguration();
                ConfigureRenderer();
                runKey?.SetValue("BlueberryAirPlay4K", $"\"{EnginePath}\"", RegistryValueKind.String);
                FooterText.Text = "已启用登录后预启动当前模式";
            }
            else
            {
                runKey?.DeleteValue("BlueberryAirPlay4K", false);
                FooterText.Text = "已关闭登录后预启动";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "自动启动设置失败", MessageBoxButton.OK, MessageBoxImage.Error);
            _loading = true;
            LoadAutostartState();
            _loading = false;
        }
    }

    private void SelectProfileFromExistingConfig()
    {
        try
        {
            if (!File.Exists(UserConfigPath)) return;
            string content = File.ReadAllText(UserConfigPath).Trim();
            ReceiverProfile? match = Profiles.Values.FirstOrDefault(profile =>
                string.Equals(profile.Arguments, content, StringComparison.Ordinal));
            if (match is null) return;

            _selectedProfileId = match.Id;
            switch (match.Id)
            {
                case "4k60-sync": Mode4K60Sync.IsChecked = true; break;
                case "4k60-low": Mode4K60Low.IsChecked = true; break;
                case "4k30": Mode4K30.IsChecked = true; break;
                case "2k60": Mode2K60.IsChecked = true; break;
                case "1080p60": Mode1080P60.IsChecked = true; break;
            }
        }
        catch
        {
            _selectedProfileId = "4k60-sync";
        }
    }

    private void LoadAutostartState()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        string? value = runKey?.GetValue("BlueberryAirPlay4K") as string;
        AutoStartCheckBox.IsChecked = value?.Contains(EnginePath, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void ValidatePackage()
    {
        string[] required =
        {
            EnginePath,
            Path.Combine(PackageRoot, "lib", "gstreamer-1.0", "libgstlibav.dll"),
            Path.Combine(PackageRoot, "lib", "gstreamer-1.0", "libgstvideoparsersbad.dll"),
            Path.Combine(PackageRoot, "lib", "gstreamer-1.0", "libgstd3d11.dll"),
        };

        string? missing = required.FirstOrDefault(path => !File.Exists(path));
        if (missing is not null)
        {
            throw new FileNotFoundException("程序包不完整，缺少必要文件：", missing);
        }
    }

    private void WriteSelectedConfiguration()
    {
        Directory.CreateDirectory(UserConfigDirectory);
        string tempPath = UserConfigPath + ".tmp";
        File.WriteAllText(tempPath, SelectedProfile.Arguments + Environment.NewLine, new UTF8Encoding(false));
        File.Move(tempPath, UserConfigPath, true);
    }

    private static void ConfigureRenderer()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\leapbtw\uxplay-windows", true);
        key?.SetValue("renderer_mode", "d3d11", RegistryValueKind.String);
        key?.SetValue("ble_enabled", true, RegistryValueKind.DWord);
    }

    private async Task StopOurProcessesAsync()
    {
        foreach (Process process in FindOurProcesses())
        {
            try
            {
                process.Kill(true);
            }
            catch
            {
                // The process may have already exited between enumeration and termination.
            }
        }

        for (int i = 0; i < 20 && FindOurProcesses().Count > 0; i++)
        {
            await Task.Delay(100);
        }

        if (FindOurProcesses().Count > 0)
        {
            throw new InvalidOperationException("接收器没有在预期时间内退出，请从系统托盘退出后重试。");
        }
    }

    private List<Process> FindOurProcesses()
    {
        var result = new List<Process>();
        foreach (string name in new[] { "uxplay-windows", "uxplay-bluetooth-beacon" })
        {
            foreach (Process process in Process.GetProcessesByName(name))
            {
                try
                {
                    string? path = process.MainModule?.FileName;
                    if (path is not null && path.StartsWith(PackageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(process);
                    }
                }
                catch
                {
                    process.Dispose();
                }
            }
        }
        return result;
    }

    private List<string> FindForeignEngineProcesses()
    {
        var paths = new List<string>();
        foreach (Process process in Process.GetProcessesByName("uxplay-windows"))
        {
            try
            {
                string? path = process.MainModule?.FileName;
                if (path is not null && !path.StartsWith(PackageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(path);
                }
            }
            catch
            {
                paths.Add($"进程 {process.Id}（路径不可读取）");
            }
            finally
            {
                process.Dispose();
            }
        }
        return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private bool IsOurEngineRunning()
    {
        List<Process> processes = FindOurProcesses();
        bool running = processes.Any(process => process.ProcessName.Equals("uxplay-windows", StringComparison.OrdinalIgnoreCase));
        foreach (Process process in processes) process.Dispose();
        return running;
    }

    private void UpdateSelectedProfileText()
    {
        SelectedArgsText.Text = $"将公布：{SelectedProfile.ReceiverName}\n参数：{SelectedProfile.Arguments}";
    }

    private void UpdateStatus()
    {
        bool running = IsOurEngineRunning();
        if (running)
        {
            HeaderStatusDot.Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            RunningAccent.Background = new SolidColorBrush(Color.FromRgb(8, 145, 178));
            HeaderStatusText.Text = "接收器运行中";
            RunningModeText.Text = $"正在等待连接：{ReadConfiguredReceiverName()}";
            RunningDetailText.Text = ReadConfiguredSummary();
            StartButton.Content = "应用并重启";
        }
        else
        {
            HeaderStatusDot.Fill = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            RunningAccent.Background = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            HeaderStatusText.Text = "接收器已停止";
            RunningModeText.Text = "接收器未运行";
            RunningDetailText.Text = "选择模式后点击“应用并启动”";
            StartButton.Content = "应用并启动";
        }
    }

    private string ReadConfiguredReceiverName()
    {
        ReceiverProfile? profile = ReadConfiguredProfile();
        return profile?.ReceiverName ?? "自定义或旧配置";
    }

    private string ReadConfiguredSummary()
    {
        ReceiverProfile? profile = ReadConfiguredProfile();
        return profile?.Summary ?? "当前使用自定义参数或其他版本配置";
    }

    private static ReceiverProfile? ReadConfiguredProfile()
    {
        try
        {
            if (!File.Exists(UserConfigPath)) return null;
            string content = File.ReadAllText(UserConfigPath).Trim();
            return Profiles.Values.FirstOrDefault(profile =>
                string.Equals(profile.Arguments, content, StringComparison.Ordinal));
        }
        catch
        {
            return null;
        }
    }
}
