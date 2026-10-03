using System.Windows;
using QuietPls.Audio;
using QuietPls.Core;
using QuietPls.Overlay;
using QuietPls.Services;
using QuietPls.Storage;
using QuietPls.UI;

namespace QuietPls;

public partial class App : System.Windows.Application
{
    private QuietPlsController? _controller;
    private TrayManager? _trayManager;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var sensor = new WasapiAudioSensor();
        var detector = new ShoutDetector();
        var visual = new VisualOverlayNotifier();
        var audio = new ChimePlayer();
        var store = new JsonSettingsStore();

        _controller = new QuietPlsController(sensor, detector, visual, audio, store);
        _trayManager = new TrayManager();

        _mainWindow = new MainWindow(_controller, sensor, _trayManager);

        try
        {
            _controller.Start();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not start audio monitoring: {ex.Message}\nPlease ensure a microphone is connected.",
                "Quiet, Pls",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        _trayManager?.Dispose();
        base.OnExit(e);
    }
}
