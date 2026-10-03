using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuietPls.UI;

public sealed class TrayManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _muteMenuItem;
    private bool _isDisposed;

    public event Action? OpenRequested;
    public event Action<bool>? MuteToggled;
    public event Action? TestRequested;
    public event Action? ExitRequested;

    public TrayManager()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Quiet, Pls - Voice Volume Monitor",
            Visible = true,
            Icon = CreateTrayIcon(Color.LimeGreen)
        };

        var menu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem("Open Dashboard", null, (_, _) => OpenRequested?.Invoke());
        var baseFont = openItem.Font ?? SystemFonts.DefaultFont;
        openItem.Font = new Font(baseFont, FontStyle.Bold);
        menu.Items.Add(openItem);

        _muteMenuItem = new ToolStripMenuItem("Mute Alerts", null, (sender, _) =>
        {
            if (sender is ToolStripMenuItem item)
            {
                item.Checked = !item.Checked;
                UpdateIconState(item.Checked);
                MuteToggled?.Invoke(item.Checked);
            }
        });
        menu.Items.Add(_muteMenuItem);

        menu.Items.Add(new ToolStripMenuItem("Test Alert", null, (_, _) => TestRequested?.Invoke()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitRequested?.Invoke()));

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke();
    }

    public void SetMuted(bool isMuted)
    {
        _muteMenuItem.Checked = isMuted;
        UpdateIconState(isMuted);
    }

    private void UpdateIconState(bool isMuted)
    {
        var oldIcon = _notifyIcon.Icon;
        _notifyIcon.Icon = CreateTrayIcon(isMuted ? Color.OrangeRed : Color.LimeGreen);
        oldIcon?.Dispose();
    }

    private static Icon CreateTrayIcon(Color indicatorColor)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Outer dark badge
            using var badgeBrush = new SolidBrush(Color.FromArgb(220, 24, 24, 28));
            g.FillEllipse(badgeBrush, 2, 2, 28, 28);

            // Status ring
            using var ringPen = new Pen(indicatorColor, 3f);
            g.DrawEllipse(ringPen, 4, 4, 24, 24);

            // Center mic capsule
            using var micBrush = new SolidBrush(Color.White);
            g.FillRoundedRectangle(micBrush, 13, 8, 6, 11, 3);

            // Mic stand
            using var standPen = new Pen(Color.White, 2f);
            g.DrawArc(standPen, 10, 11, 12, 10, 0, 180);
            g.DrawLine(standPen, 16, 21, 16, 24);
        }

        IntPtr hIcon = bitmap.GetHicon();
        return (Icon)Icon.FromHandle(hIcon).Clone();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
    }
}

file static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
    {
        using var path = new GraphicsPath();
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        path.AddArc(x + width - (radius * 2), y, radius * 2, radius * 2, 270, 90);
        path.AddArc(x + width - (radius * 2), y + height - (radius * 2), radius * 2, radius * 2, 0, 90);
        path.AddArc(x, y + height - (radius * 2), radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
