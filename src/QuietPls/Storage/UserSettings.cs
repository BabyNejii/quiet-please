using QuietPls.Core;

namespace QuietPls.Storage;

public sealed record UserSettings
{
    public float LoudThreshold { get; init; } = 0.55f;
    public int WarningDurationMs { get; init; } = 150;
    public int EscalationDurationMs { get; init; } = 1200;
    public bool VisualEnabled { get; init; } = true;
    public bool AudioEnabled { get; init; } = true;
    public string? SelectedDeviceId { get; init; }

    public ThresholdConfig ToThresholdConfig()
    {
        return new ThresholdConfig
        {
            LoudThreshold = Math.Clamp(LoudThreshold, 0.05f, 1.0f),
            WarningDuration = TimeSpan.FromMilliseconds(Math.Max(50, WarningDurationMs)),
            EscalationDuration = TimeSpan.FromMilliseconds(Math.Max(WarningDurationMs + 100, EscalationDurationMs))
        };
    }
}
