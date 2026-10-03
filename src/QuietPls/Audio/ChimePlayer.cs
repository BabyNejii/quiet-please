using System.IO;
using System.Media;

namespace QuietPls.Audio;

public sealed class ChimePlayer : IAudioNotifier
{
    private readonly MemoryStream _audioStream;
    private readonly SoundPlayer _player;
    private bool _isDisposed;

    public ChimePlayer()
    {
        byte[] wavBytes = GenerateDualToneWav(520, 660, 240);
        _audioStream = new MemoryStream(wavBytes);
        _player = new SoundPlayer(_audioStream);
        _player.Load();
    }

    public void PlayEscalationChime()
    {
        if (_isDisposed)
        {
            return;
        }

        _player.Play();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _player.Dispose();
        _audioStream.Dispose();
    }

    private static byte[] GenerateDualToneWav(double freq1, double freq2, int durationMs)
    {
        const int sampleRate = 44100;
        int sampleCount = (sampleRate * durationMs) / 1000;
        int subChunk2Size = sampleCount * 2;
        int chunkSize = 36 + subChunk2Size;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF Header
        writer.Write("RIFF"u8);
        writer.Write(chunkSize);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16); // Subchunk1Size (16 for PCM)
        writer.Write((short)1); // AudioFormat (1 for PCM)
        writer.Write((short)1); // NumChannels (1 = Mono)
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2); // ByteRate
        writer.Write((short)2); // BlockAlign
        writer.Write((short)16); // BitsPerSample
        writer.Write("data"u8);
        writer.Write(subChunk2Size);

        // Generate envelope and dual tone
        double attackSamples = sampleRate * 0.02; // 20ms attack
        double decaySamples = sampleRate * 0.12;  // 120ms decay

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / sampleRate;
            double envelope = CalculateEnvelope(i, sampleCount, attackSamples, decaySamples);
            double sampleValue = (0.55 * Math.Sin(2 * Math.PI * freq1 * t)) +
                                 (0.45 * Math.Sin(2 * Math.PI * freq2 * t));

            short pcm = (short)Math.Clamp(sampleValue * envelope * 24000.0, short.MinValue, short.MaxValue);
            writer.Write(pcm);
        }

        writer.Flush();
        return ms.ToArray();
    }

    private static double CalculateEnvelope(int index, int total, double attack, double decay)
    {
        if (index < attack)
        {
            return index / attack;
        }

        int remaining = total - index;
        if (remaining < decay)
        {
            return Math.Max(0.0, remaining / decay);
        }

        return 1.0;
    }
}
