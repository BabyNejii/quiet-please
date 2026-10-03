namespace QuietPls.Core;

public readonly record struct ShoutDetectionResult(
    AlertTier Tier,
    bool VisualTriggered,
    bool AudioTriggered,
    TimeSpan SustainedDuration,
    float PeakLevel,
    bool InCooldown
);
