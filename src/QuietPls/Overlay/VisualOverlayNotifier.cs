using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using QuietPls.Interop;

namespace QuietPls.Overlay;

public sealed class VisualOverlayNotifier : IVisualNotifier
{
    private readonly List<GhostOverlayWindow> _windows = [];
    private bool _enabled = true;
    private bool _isDisposed;

    public VisualOverlayNotifier()
    {
        InitializeOverlayWindows();
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    public void PulseWarning()
    {
        if (!_enabled || _isDisposed)
        {
            return;
        }

        foreach (var window in _windows)
        {
            window.Pulse(peakOpacity: 0.75, totalDurationMs: 350);
        }
    }

    public void PulseEscalation()
    {
        if (!_enabled || _isDisposed)
        {
            return;
        }

        foreach (var window in _windows)
        {
            window.Pulse(peakOpacity: 1.0, totalDurationMs: 600);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (System.Windows.Application.Current != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            System.Windows.Application.Current.Dispatcher.Invoke(CloseAllWindows);
        }
        else
        {
            CloseAllWindows();
        }
    }

    private void InitializeOverlayWindows()
    {
        var monitors = EnumerateMonitors();

        if (monitors.Count == 0)
        {
            // Fallback to virtual screen
            CreateFallbackWindow();
            return;
        }

        foreach (var rect in monitors)
        {
            CreateMonitorWindow(rect);
        }
    }

    private void CreateMonitorWindow(NativeMethods.RECT rc)
    {
        var window = new GhostOverlayWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual
        };

        window.Show();

        // Convert physical pixels to WPF DIPs for the target monitor
        var source = PresentationSource.FromVisual(window);
        if (source?.CompositionTarget != null)
        {
            var transform = source.CompositionTarget.TransformFromDevice;
            var topLeft = transform.Transform(new System.Windows.Point(rc.Left, rc.Top));
            var bottomRight = transform.Transform(new System.Windows.Point(rc.Right, rc.Bottom));

            window.Left = topLeft.X;
            window.Top = topLeft.Y;
            window.Width = Math.Max(100, bottomRight.X - topLeft.X);
            window.Height = Math.Max(100, bottomRight.Y - topLeft.Y);
        }
        else
        {
            window.Left = rc.Left;
            window.Top = rc.Top;
            window.Width = rc.Right - rc.Left;
            window.Height = rc.Bottom - rc.Top;
        }

        _windows.Add(window);
    }

    private void CreateFallbackWindow()
    {
        var window = new GhostOverlayWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = SystemParameters.VirtualScreenLeft,
            Top = SystemParameters.VirtualScreenTop,
            Width = SystemParameters.VirtualScreenWidth,
            Height = SystemParameters.VirtualScreenHeight
        };
        window.Show();
        _windows.Add(window);
    }

    private static List<NativeMethods.RECT> EnumerateMonitors()
    {
        var list = new List<NativeMethods.RECT>();

        NativeMethods.EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (IntPtr hMonitor, IntPtr _, ref NativeMethods.RECT _, IntPtr _) =>
            {
                var mi = new NativeMethods.MONITORINFOEX
                {
                    cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
                };

                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                {
                    list.Add(mi.rcMonitor);
                }

                return true;
            },
            IntPtr.Zero);

        return list;
    }

    private void CloseAllWindows()
    {
        foreach (var window in _windows)
        {
            window.Close();
        }
        _windows.Clear();
    }
}
