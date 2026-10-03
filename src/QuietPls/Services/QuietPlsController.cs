using QuietPls.Audio;
using QuietPls.Core;
using QuietPls.Overlay;
using QuietPls.Storage;

namespace QuietPls.Services;

public sealed class QuietPlsController : IDisposable
{
    private readonly IAudioSensor _sensor;
    private readonly IShoutDetector _detector;
    private readonly IVisualNotifier _visual;
    private readonly IAudioNotifier _audio;
    private readonly ISettingsStore _store;

    private UserSettings _settings;
    private bool _isMuted;
    private bool _isDisposed;

    public event Action<float, ShoutDetectionResult>? SampleProcessed;

    public QuietPlsController(
        IAudioSensor sensor,
        IShoutDetector detector,
        IVisualNotifier visual,
        IAudioNotifier audio,
        ISettingsStore store)
    {
        _sensor = sensor;
        _detector = detector;
        _visual = visual;
        _audio = audio;
        _store = store;

        _settings = _store.Load();
        _detector.UpdateConfig(_settings.ToThresholdConfig());
        _visual.SetEnabled(_settings.VisualEnabled);

        _sensor.LevelSampled += HandleLevelSampled;
    }

    public UserSettings Settings => _settings;

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            if (_isMuted)
            {
                _detector.Reset();
            }
        }
    }

    public void Start()
    {
        _sensor.Start(_settings.SelectedDeviceId);
    }

    public void Stop()
    {
        _sensor.Stop();
        _detector.Reset();
    }

    public void UpdateSettings(UserSettings newSettings)
    {
        ArgumentNullException.ThrowIfNull(newSettings);

        _settings = newSettings;
        _store.Save(newSettings);
        _detector.UpdateConfig(newSettings.ToThresholdConfig());
        _visual.SetEnabled(newSettings.VisualEnabled);

        if (_sensor.IsRunning)
        {
            _sensor.Start(newSettings.SelectedDeviceId);
        }
    }

    public void TestVisual()
    {
        _visual.PulseWarning();
    }

    public void TestAudio()
    {
        _audio.PlayEscalationChime();
    }

    private void HandleLevelSampled(float peak)
    {
        if (_isMuted || _isDisposed)
        {
            return;
        }

        var result = _detector.ProcessSample(peak, DateTimeOffset.UtcNow);

        if (result.VisualTriggered && _settings.VisualEnabled)
        {
            _visual.PulseWarning();
        }

        if (result.AudioTriggered)
        {
            if (_settings.VisualEnabled)
            {
                _visual.PulseEscalation();
            }

            if (_settings.AudioEnabled)
            {
                _audio.PlayEscalationChime();
            }
        }

        SampleProcessed?.Invoke(peak, result);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _sensor.LevelSampled -= HandleLevelSampled;
        _sensor.Dispose();
        _visual.Dispose();
        _audio.Dispose();
    }
}
