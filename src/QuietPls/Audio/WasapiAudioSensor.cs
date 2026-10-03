using NAudio.CoreAudioApi;
using NAudio.Wave;

#pragma warning disable CS0618

namespace QuietPls.Audio;

public sealed class WasapiAudioSensor : IAudioSensor
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly object _lock = new();
    private WasapiCapture? _capture;
    private bool _isDisposed;

    public event Action<float>? LevelSampled;

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _capture != null && _capture.CaptureState == CaptureState.Capturing;
            }
        }
    }

    public IReadOnlyList<AudioDeviceInfo> GetCaptureDevices()
    {
        var result = new List<AudioDeviceInfo>();
        string? defaultId = null;

        try
        {
            using var defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            defaultId = defaultDevice?.ID;
        }
        catch
        {
            // Handled when no audio capture hardware is present
        }

        var endpoints = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        foreach (var endpoint in endpoints)
        {
            bool isDefault = defaultId != null && string.Equals(endpoint.ID, defaultId, StringComparison.OrdinalIgnoreCase);
            result.Add(new AudioDeviceInfo(endpoint.ID, endpoint.FriendlyName, isDefault));
        }

        return result;
    }

    public void Start(string? deviceId = null)
    {
        lock (_lock)
        {
            Stop();

            MMDevice? device = null;
            if (!string.IsNullOrEmpty(deviceId))
            {
                try
                {
                    device = _enumerator.GetDevice(deviceId);
                }
                catch
                {
                    // Fall back to default if configured ID is missing/disconnected
                    device = null;
                }
            }

            device ??= _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            if (device == null)
            {
                throw new InvalidOperationException("No audio capture device found.");
            }

            _capture = new WasapiCapture(device);
            _capture.DataAvailable += HandleDataAvailable;
            _capture.RecordingStopped += HandleRecordingStopped;
            _capture.StartRecording();
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (_capture != null)
            {
                _capture.DataAvailable -= HandleDataAvailable;
                _capture.RecordingStopped -= HandleRecordingStopped;
                try
                {
                    _capture.StopRecording();
                }
                catch
                {
                    // Device may already be disconnected
                }
                finally
                {
                    _capture.Dispose();
                    _capture = null;
                }
            }
        }
    }

    private void HandleDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0 || _capture == null)
        {
            return;
        }

        float peak = CalculatePeak(e.Buffer, e.BytesRecorded, _capture.WaveFormat);
        LevelSampled?.Invoke(peak);
    }

    private static float CalculatePeak(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        float max = 0f;

        if (format.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            int floatCount = bytesRecorded / 4;
            for (int i = 0; i < floatCount; i++)
            {
                float sample = BitConverter.ToSingle(buffer, i * 4);
                float abs = Math.Abs(sample);
                if (abs > max)
                {
                    max = abs;
                }
            }
        }
        else if (format.BitsPerSample == 16)
        {
            int shortCount = bytesRecorded / 2;
            for (int i = 0; i < shortCount; i++)
            {
                short sample = BitConverter.ToInt16(buffer, i * 2);
                float abs = Math.Abs(sample) / 32768f;
                if (abs > max)
                {
                    max = abs;
                }
            }
        }

        return Math.Clamp(max, 0f, 1f);
    }

    private void HandleRecordingStopped(object? sender, StoppedEventArgs e)
    {
        lock (_lock)
        {
            if (_capture != null)
            {
                _capture.Dispose();
                _capture = null;
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Stop();
        _enumerator.Dispose();
    }
}
