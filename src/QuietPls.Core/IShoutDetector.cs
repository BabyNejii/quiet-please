namespace QuietPls.Core;

public interface IShoutDetector
{
    ThresholdConfig Config { get; }
    void UpdateConfig(ThresholdConfig config);
    ShoutDetectionResult ProcessSample(float peak, DateTimeOffset now);
    void Reset();
}
