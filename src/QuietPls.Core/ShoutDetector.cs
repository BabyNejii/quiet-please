namespace QuietPls.Core;

public sealed class ShoutDetector : IShoutDetector
{
    private readonly object _syncLock = new();
    private ThresholdConfig _config;

    private DateTimeOffset? _burstStartTime;
    private DateTimeOffset? _lastLoudTime;
    private DateTimeOffset? _cooldownUntil;
    private DateTimeOffset? _lastAudioTriggerTime;
    private bool _visualFiredInBurst;

    public ShoutDetector(ThresholdConfig? config = null)
    {
        var cfg = config ?? ThresholdConfig.Default;
        cfg.Validate();
        _config = cfg;
    }

    public ThresholdConfig Config
    {
        get
        {
            lock (_syncLock)
            {
                return _config;
            }
        }
    }

    public void UpdateConfig(ThresholdConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Validate();

        lock (_syncLock)
        {
            _config = config;
        }
    }

    public void Reset()
    {
        lock (_syncLock)
        {
            _burstStartTime = null;
            _lastLoudTime = null;
            _cooldownUntil = null;
            _lastAudioTriggerTime = null;
            _visualFiredInBurst = false;
        }
    }

    public ShoutDetectionResult ProcessSample(float peak, DateTimeOffset now)
    {
        lock (_syncLock)
        {
            if (IsInCooldown(now))
            {
                return new ShoutDetectionResult(AlertTier.None, false, false, TimeSpan.Zero, peak, InCooldown: true);
            }

            bool isLoud = peak >= _config.LoudThreshold;

            if (isLoud)
            {
                return HandleLoudSample(peak, now);
            }

            return HandleQuietSample(peak, now);
        }
    }

    private bool IsInCooldown(DateTimeOffset now)
    {
        if (_cooldownUntil is null)
        {
            return false;
        }

        if (now < _cooldownUntil.Value)
        {
            return true;
        }

        _cooldownUntil = null;
        return false;
    }

    private ShoutDetectionResult HandleLoudSample(float peak, DateTimeOffset now)
    {
        _lastLoudTime = now;
        _burstStartTime ??= now;

        var sustained = now - _burstStartTime.Value;
        bool triggerVisual = false;
        bool triggerAudio = false;
        var tier = AlertTier.None;

        if (sustained >= _config.WarningDuration)
        {
            tier = AlertTier.Visual;
            if (!_visualFiredInBurst)
            {
                triggerVisual = true;
                _visualFiredInBurst = true;
            }
        }

        if (sustained >= _config.EscalationDuration)
        {
            tier = AlertTier.Escalated;
            if (ShouldTriggerAudio(now))
            {
                triggerAudio = true;
                _lastAudioTriggerTime = now;
            }
        }

        return new ShoutDetectionResult(tier, triggerVisual, triggerAudio, sustained, peak, InCooldown: false);
    }

    private bool ShouldTriggerAudio(DateTimeOffset now)
    {
        if (_lastAudioTriggerTime is null)
        {
            return true;
        }

        return (now - _lastAudioTriggerTime.Value) >= _config.EscalationRepeatInterval;
    }

    private ShoutDetectionResult HandleQuietSample(float peak, DateTimeOffset now)
    {
        if (_burstStartTime is null)
        {
            return new ShoutDetectionResult(AlertTier.None, false, false, TimeSpan.Zero, peak, InCooldown: false);
        }

        bool withinGracePeriod = _lastLoudTime is not null && (now - _lastLoudTime.Value) <= _config.HoldTime;
        if (withinGracePeriod)
        {
            var sustained = now - _burstStartTime.Value;
            var currentTier = sustained >= _config.EscalationDuration
                ? AlertTier.Escalated
                : (sustained >= _config.WarningDuration ? AlertTier.Visual : AlertTier.None);

            return new ShoutDetectionResult(currentTier, false, false, sustained, peak, InCooldown: false);
        }

        EndBurst(now);
        return new ShoutDetectionResult(AlertTier.None, false, false, TimeSpan.Zero, peak, InCooldown: _cooldownUntil is not null);
    }

    private void EndBurst(DateTimeOffset now)
    {
        if (_visualFiredInBurst && _config.Cooldown > TimeSpan.Zero)
        {
            _cooldownUntil = now + _config.Cooldown;
        }

        _burstStartTime = null;
        _lastLoudTime = null;
        _lastAudioTriggerTime = null;
        _visualFiredInBurst = false;
    }
}
