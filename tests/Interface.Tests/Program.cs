using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BlueberryAirPlay4K;

internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        // Own test-output directory only. Never start a receiver or change registry/config.
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "display-control.version"), "1");
        var app = new App(); app.InitializeComponent();
        var window = new MainWindow();
        try
        {
            var stack = (StackPanel)window.FindName("ContentStack");
            if (stack.Children[0] != window.FindName("QualityCard")) throw new Exception("Quality must come first");
            ((RadioButton)window.FindName("DisplayAspect")).IsChecked = true;
            var display = new DisplaySettings(AppContext.BaseDirectory);
            if (display.Read().Mode != PictureMode.AspectWindow) throw new Exception("Mode not applied immediately");
            ((RadioButton)window.FindName("DisplayFullscreen")).IsChecked = true;
            if (display.Read().State != PictureWindowState.Fullscreen) throw new Exception("State not applied immediately");
            var content = (FrameworkElement)window.Content;
            content.Measure(new Size(940,760)); content.Arrange(new Rect(0,0,940,760)); content.UpdateLayout();
            if (args.Length > 0)
            {
                var bitmap = new RenderTargetBitmap(940,760,96,96,System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(content); var encoder = new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream = File.Create(args[0]);encoder.Save(stream);
            }
            Console.WriteLine("PASS: quality first; immediate mode/state selection; WPF layout render");
        }
        finally { window.Close(); app.Shutdown(); }
    }
}
