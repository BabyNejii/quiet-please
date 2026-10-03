namespace QuietPls.Overlay;

public interface IVisualNotifier : IDisposable
{
    void PulseWarning();
    void PulseEscalation();
    void SetEnabled(bool enabled);
}
