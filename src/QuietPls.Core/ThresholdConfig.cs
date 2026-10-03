namespace QuietPls.Core;

public sealed record ThresholdConfig
{
    public float LoudThreshold { get; init; } = 0.60f;
    public TimeSpan WarningDuration { get; init; } = TimeSpan.FromMilliseconds(150);
    public TimeSpan EscalationDuration { get; init; } = TimeSpan.FromMilliseconds(1200);
    public TimeSpan EscalationRepeatInterval { get; init; } = TimeSpan.FromMilliseconds(2000);
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromMilliseconds(1000);
    public TimeSpan HoldTime { get; init; } = TimeSpan.FromMilliseconds(150);

    public static ThresholdConfig Default => new();

    public void Validate()
    {
        if (LoudThreshold is <= 0.01f or > 1.0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(LoudThreshold),
                LoudThreshold,
                "LoudThreshold must be between 0.01 and 1.0.");
        }

        if (WarningDuration < TimeSpan.FromMilliseconds(20))
        {
            throw new ArgumentOutOfRangeException(
                nameof(WarningDuration),
                WarningDuration,
                "WarningDuration must be at least 20ms.");
        }

        if (EscalationDuration <= WarningDuration)
        {
            throw new ArgumentException(
                "EscalationDuration must be greater than WarningDuration.",
                nameof(EscalationDuration));
        }

        if (EscalationRepeatInterval < TimeSpan.FromMilliseconds(500))
        {
            throw new ArgumentOutOfRangeException(
                nameof(EscalationRepeatInterval),
                EscalationRepeatInterval,
                "EscalationRepeatInterval must be at least 500ms.");
        }

        if (Cooldown < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Cooldown),
                Cooldown,
                "Cooldown must be non-negative.");
        }

        if (HoldTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(HoldTime),
                HoldTime,
                "HoldTime must be non-negative.");
        }
    }
}
