using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

// Small .NET Framework launcher. It does not change user settings or start the receiver.
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app");
        string controller = Path.Combine(directory, "蓝莓AirPlay4K控制台.exe");
        try
        {
            if (!File.Exists(controller)) throw new FileNotFoundException("请完整解压程序包，不能只移动启动文件。", controller);
            Process.Start(new ProcessStartInfo(controller) { WorkingDirectory = directory, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\n\n运行界面需要 .NET 8 Desktop Runtime x64。", "蓝莓投屏启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
