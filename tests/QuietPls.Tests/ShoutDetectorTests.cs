using QuietPls.Core;
using Xunit;

namespace QuietPls.Tests;

public class ShoutDetectorTests
{
    private readonly ThresholdConfig _testConfig = new()
    {
        LoudThreshold = 0.50f,
        WarningDuration = TimeSpan.FromMilliseconds(150),
        EscalationDuration = TimeSpan.FromMilliseconds(1000),
        EscalationRepeatInterval = TimeSpan.FromMilliseconds(1500),
        Cooldown = TimeSpan.FromMilliseconds(800),
        HoldTime = TimeSpan.FromMilliseconds(100)
    };

    [Fact]
    public void QuietAudio_DoesNotTriggerAlerts()
    {
        var detector = new ShoutDetector(_testConfig);
        var baseTime = DateTimeOffset.UtcNow;

        for (int i = 0; i < 20; i++)
        {
            var res = detector.ProcessSample(0.2f, baseTime.AddMilliseconds(i * 25));
            Assert.Equal(AlertTier.None, res.Tier);
            Assert.False(res.VisualTriggered);
            Assert.False(res.AudioTriggered);
        }
    }

    [Fact]
    public void ShortSpike_UnderWarningDuration_DoesNotTriggerAlerts()
    {
        var detector = new ShoutDetector(_testConfig);
        var baseTime = DateTimeOffset.UtcNow;

        // 75ms loud burst (below 150ms warning threshold)
        detector.ProcessSample(0.8f, baseTime);
        detector.ProcessSample(0.8f, baseTime.AddMilliseconds(25));
        var res = detector.ProcessSample(0.8f, baseTime.AddMilliseconds(50));

        Assert.Equal(AlertTier.None, res.Tier);
        Assert.False(res.VisualTriggered);
        Assert.False(res.AudioTriggered);

        // Sound drops below threshold, hold time passes
        var quietRes = detector.ProcessSample(0.1f, baseTime.AddMilliseconds(200));
        Assert.Equal(AlertTier.None, quietRes.Tier);
        Assert.False(quietRes.VisualTriggered);
        Assert.False(quietRes.InCooldown);
    }

    [Fact]
    public void SustainedShout_TriggersVisualAlertOnce()
    {
        var detector = new ShoutDetector(_testConfig);
        var baseTime = DateTimeOffset.UtcNow;

        // Feed samples up to 150ms
        detector.ProcessSample(0.8f, baseTime);
        detector.ProcessSample(0.8f, baseTime.AddMilliseconds(50));
        detector.ProcessSample(0.8f, baseTime.AddMilliseconds(100));

        var triggerRes = detector.ProcessSample(0.8f, baseTime.AddMilliseconds(150));
        Assert.Equal(AlertTier.Visual, triggerRes.Tier);
        Assert.True(triggerRes.VisualTriggered, "Visual alert should trigger when hitting 150ms");
        Assert.False(triggerRes.AudioTriggered);

        // Subsequent sample while still shouting should NOT re-trigger VisualTriggered flag
        var subsequentRes = detector.ProcessSample(0.8f, baseTime.AddMilliseconds(200));
        Assert.Equal(AlertTier.Visual, subsequentRes.Tier);
        Assert.False(subsequentRes.VisualTriggered, "Visual should only trigger on the transition");
    }

    [Fact]
    public void ContinuedShouting_EscalatesToAudioChime()
    {
        var detector = new ShoutDetector(_testConfig);
        var baseTime = DateTimeOffset.UtcNow;

        // Yelling for 1000ms
        for (int ms = 0; ms < 950; ms += 50)
        {
            detector.ProcessSample(0.85f, baseTime.AddMilliseconds(ms));
        }

        var escalateRes = detector.ProcessSample(0.85f, baseTime.AddMilliseconds(1000));
        Assert.Equal(AlertTier.Escalated, escalateRes.Tier);
        Assert.True(escalateRes.AudioTriggered, "Audio chime should trigger on escalation duration");
    }

    [Fact]
    public void SyllableDip_WithinHoldTime_PreservesBurstDuration()
    {
        var detector = new ShoutDetector(_testConfig);
        var baseTime = DateTimeOffset.UtcNow;

        // Shouting starts
        detector.ProcessSample(0.8f, baseTime);
        detector.ProcessSample(0.8f, baseTime.AddMilliseconds(100));

        // 50ms brief pause between words (within 100ms HoldTime)
        var dipRes = detector.ProcessSample(0.1f, baseTime.AddMilliseconds(150));
        Assert.Equal(AlertTier.Visual, dipRes.Tier);
        Assert.False(dipRes.InCooldown);

        // Shouting resumes
        var resumedRes = detector.ProcessSample(0.8f, baseTime.AddMilliseconds(200));
        Assert.Equal(AlertTier.Visual, resumedRes.Tier);
        Assert.True(resumedRes.SustainedDuration >= TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void Cooldown_SuppressesImmediateRetrigger()
    {
        var detector = new ShoutDetector(_testConfig);
        var baseTime = DateTimeOffset.UtcNow;

        // Trigger visual
        detector.ProcessSample(0.8f, baseTime);
        detector.ProcessSample(0.8f, baseTime.AddMilliseconds(150));

        // Stop shouting; wait longer than HoldTime (100ms)
        detector.ProcessSample(0.1f, baseTime.AddMilliseconds(300));

        // Attempt shouting again at +500ms (within 800ms cooldown)
        var cooldownRes = detector.ProcessSample(0.9f, baseTime.AddMilliseconds(500));
        Assert.True(cooldownRes.InCooldown);
        Assert.False(cooldownRes.VisualTriggered);

        // After cooldown expires (+300ms + 800ms = 1100ms)
        var postCooldownRes = detector.ProcessSample(0.9f, baseTime.AddMilliseconds(1200));
        Assert.False(postCooldownRes.InCooldown);
    }
}
