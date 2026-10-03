namespace QuietPls.Audio;

public sealed record AudioDeviceInfo(string Id, string Name, bool IsDefault);

public interface IAudioSensor : IDisposable
{
    event Action<float>? LevelSampled;
    IReadOnlyList<AudioDeviceInfo> GetCaptureDevices();
    void Start(string? deviceId = null);
    void Stop();
    bool IsRunning { get; }
}
