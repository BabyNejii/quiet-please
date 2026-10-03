using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using QuietPls.Audio;
using QuietPls.Core;
using QuietPls.Services;
using QuietPls.Storage;
using QuietPls.UI;

namespace QuietPls;

public partial class MainWindow : Window
{
    private readonly QuietPlsController _controller;
    private readonly IAudioSensor _sensor;
    private readonly TrayManager _trayManager;
    private bool _isExplicitExit;
    private bool _isInitializing = true;

    private readonly System.Windows.Media.Brush _uiGreen = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 230, 118));
    private readonly System.Windows.Media.Brush _uiYellow = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 214, 0));
    private readonly System.Windows.Media.Brush _uiRed = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 23, 68));
    private readonly System.Windows.Media.Brush _uiMuted = new SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 142, 160));

    public MainWindow(QuietPlsController controller, IAudioSensor sensor, TrayManager trayManager)
    {
        _controller = controller;
        _sensor = sensor;
        _trayManager = trayManager;

        InitializeComponent();
        LoadInitialSettings();
        SubscribeEvents();

        _isInitializing = false;
    }

    private void LoadInitialSettings()
    {
        var settings = _controller.Settings;
        ThresholdSlider.Value = Math.Round(settings.LoudThreshold * 100);
        ThresholdValueLabel.Text = $"{(int)ThresholdSlider.Value}%";

        WarningDelaySlider.Value = settings.WarningDurationMs;
        WarningDelayLabel.Text = $"{settings.WarningDurationMs} ms";

        EscalationDelaySlider.Value = settings.EscalationDurationMs;
        EscalationDelayLabel.Text = $"{settings.EscalationDurationMs} ms";

        VisualToggle.IsChecked = settings.VisualEnabled;
        AudioToggle.IsChecked = settings.AudioEnabled;

        PopulateDevices(settings.SelectedDeviceId);
    }

    private void SubscribeEvents()
    {
        _controller.SampleProcessed += HandleSampleProcessed;

        _trayManager.OpenRequested += ShowAndActivate;
        _trayManager.MuteToggled += OnTrayMuteToggled;
        _trayManager.TestRequested += OnTrayTestRequested;
        _trayManager.ExitRequested += ExitApplication;
    }

    private void PopulateDevices(string? selectedId)
    {
        DeviceComboBox.Items.Clear();
        var devices = _sensor.GetCaptureDevices();

        int selectedIndex = 0;
        for (int i = 0; i < devices.Count; i++)
        {
            var dev = devices[i];
            var label = dev.IsDefault ? $"{dev.Name} (Default)" : dev.Name;
            DeviceComboBox.Items.Add(new ComboBoxItemTag(dev.Id, label));

            if (!string.IsNullOrEmpty(selectedId) && dev.Id == selectedId)
            {
                selectedIndex = i;
            }
            else if (string.IsNullOrEmpty(selectedId) && dev.IsDefault)
            {
                selectedIndex = i;
            }
        }

        if (DeviceComboBox.Items.Count > 0)
        {
            DeviceComboBox.SelectedIndex = selectedIndex;
        }
    }

    private void HandleSampleProcessed(float peak, ShoutDetectionResult result)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => UpdateMeterUi(peak, result));
            return;
        }

        UpdateMeterUi(peak, result);
    }

    private void UpdateMeterUi(float peak, ShoutDetectionResult result)
    {
        int percent = Math.Clamp((int)(peak * 100), 0, 100);
        VolumeProgressBar.Value = percent;
        VolumePercentText.Text = $"{percent}%";

        if (result.InCooldown)
        {
            StatusBadge.Text = "Cooling Down";
            StatusBadge.Foreground = _uiYellow;
            VolumeProgressBar.Foreground = _uiYellow;
        }
        else if (result.Tier == AlertTier.Escalated)
        {
            StatusBadge.Text = "LOUD! (Escalated)";
            StatusBadge.Foreground = _uiRed;
            VolumeProgressBar.Foreground = _uiRed;
        }
        else if (result.Tier == AlertTier.Visual)
        {
            StatusBadge.Text = "LOUD! (Warning)";
            StatusBadge.Foreground = _uiYellow;
            VolumeProgressBar.Foreground = _uiYellow;
        }
        else
        {
            StatusBadge.Text = peak >= (_controller.Settings.LoudThreshold) ? "Threshold Met" : "Normal";
            StatusBadge.Foreground = _uiGreen;
            VolumeProgressBar.Foreground = _uiGreen;
        }
    }

    private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ThresholdValueLabel == null) return;
        int val = (int)e.NewValue;
        ThresholdValueLabel.Text = $"{val}%";
        SaveCurrentSettings();
    }

    private void WarningDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WarningDelayLabel == null) return;
        int val = (int)e.NewValue;
        WarningDelayLabel.Text = $"{val} ms";
        SaveCurrentSettings();
    }

    private void EscalationDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EscalationDelayLabel == null) return;
        int val = (int)e.NewValue;
        EscalationDelayLabel.Text = $"{val} ms";
        SaveCurrentSettings();
    }

    private void VisualToggle_Changed(object sender, RoutedEventArgs e) => SaveCurrentSettings();
    private void AudioToggle_Changed(object sender, RoutedEventArgs e) => SaveCurrentSettings();

    private void DeviceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_isInitializing)
        {
            SaveCurrentSettings();
        }
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        PopulateDevices(_controller.Settings.SelectedDeviceId);
    }

    private void SaveCurrentSettings()
    {
        if (_isInitializing) return;

        string? deviceId = null;
        if (DeviceComboBox.SelectedItem is ComboBoxItemTag itemTag)
        {
            deviceId = itemTag.Id;
        }

        var newSettings = new UserSettings
        {
            LoudThreshold = (float)(ThresholdSlider.Value / 100.0),
            WarningDurationMs = (int)WarningDelaySlider.Value,
            EscalationDurationMs = (int)EscalationDelaySlider.Value,
            VisualEnabled = VisualToggle.IsChecked == true,
            AudioEnabled = AudioToggle.IsChecked == true,
            SelectedDeviceId = deviceId
        };

        _controller.UpdateSettings(newSettings);
    }

    private void MuteToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        ToggleMuteState(!_controller.IsMuted);
    }

    private void OnTrayMuteToggled(bool isMuted)
    {
        Dispatcher.InvokeAsync(() => ToggleMuteState(isMuted));
    }

    private void ToggleMuteState(bool isMuted)
    {
        _controller.IsMuted = isMuted;
        _trayManager.SetMuted(isMuted);

        if (isMuted)
        {
            MuteStatusDot.Fill = _uiRed;
            MuteStatusText.Text = "Muted";
            StatusBadge.Text = "Alerts Paused";
            StatusBadge.Foreground = _uiMuted;
        }
        else
        {
            MuteStatusDot.Fill = _uiGreen;
            MuteStatusText.Text = "Active";
            StatusBadge.Text = "Normal";
            StatusBadge.Foreground = _uiGreen;
        }
    }

    private void TestVisual_Click(object sender, RoutedEventArgs e) => _controller.TestVisual();
    private void TestAudio_Click(object sender, RoutedEventArgs e) => _controller.TestAudio();

    private void OnTrayTestRequested()
    {
        Dispatcher.InvokeAsync(() =>
        {
            _controller.TestVisual();
            _controller.TestAudio();
        });
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e) => Hide();

    private void ShowAndActivate()
    {
        Dispatcher.InvokeAsync(() =>
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
        });
    }

    private void ExitApplication()
    {
        Dispatcher.InvokeAsync(() =>
        {
            _isExplicitExit = true;
            Close();
            System.Windows.Application.Current.Shutdown();
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }
}

file sealed record ComboBoxItemTag(string Id, string DisplayName)
{
    public override string ToString() => DisplayName;
}