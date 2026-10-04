using System.Threading;
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
    private static Mutex? _instanceMutex;
    private static EventWaitHandle? _showSignal;

    private QuietPlsController? _controller;
    private TrayManager? _trayManager;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(true, "QuietPls_SingleInstance_App_Mutex_78a1", out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running; signal it to open dashboard and exit
            try
            {
                using var signal = EventWaitHandle.OpenExisting("QuietPls_Show_Event_Signal_78a1");
                signal.Set();
            }
            catch
            {
                // Fallback handled
            }

            Environment.Exit(0);
            return;
        }

        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "QuietPls_Show_Event_Signal_78a1");
        var listenerThread = new Thread(() =>
        {
            try
            {
                while (_showSignal.WaitOne())
                {
                    _mainWindow?.ShowAndActivate();
                }
            }
            catch (ObjectDisposedException)
            {
                // App exiting
            }
        })
        {
            IsBackground = true
        };
        listenerThread.Start();

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

        bool forceShow = e.Args.Any(a => a.Equals("--show", StringComparison.OrdinalIgnoreCase));
        bool forceMinimized = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) || a.Equals("-minimized", StringComparison.OrdinalIgnoreCase));
        bool shouldMinimize = forceMinimized || (_controller.Settings.StartMinimized && !forceShow);

        if (!shouldMinimize)
        {
            _mainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _showSignal?.Dispose();
        _instanceMutex?.Dispose();
        _controller?.Dispose();
        _trayManager?.Dispose();
        base.OnExit(e);
    }
}
