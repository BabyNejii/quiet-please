using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using QuietPls.Interop;

namespace QuietPls.Overlay;

public partial class GhostOverlayWindow : Window
{
    private readonly DoubleAnimation _pulseAnimation;

    public GhostOverlayWindow()
    {
        InitializeComponent();

        _pulseAnimation = new DoubleAnimation
        {
            From = 0.0,
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.ApplyGhostWindowStyles(hwnd);
    }

    public void Pulse(double peakOpacity, int totalDurationMs)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => Pulse(peakOpacity, totalDurationMs));
            return;
        }

        _pulseAnimation.To = peakOpacity;
        _pulseAnimation.Duration = TimeSpan.FromMilliseconds(totalDurationMs / 2.0);

        VignetteRoot.BeginAnimation(OpacityProperty, _pulseAnimation);
    }
}
