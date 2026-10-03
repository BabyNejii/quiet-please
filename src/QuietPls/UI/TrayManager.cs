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
        Icon? baseIcon = null;
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/app.ico");
            var streamInfo = System.Windows.Application.GetResourceStream(uri);
            if (streamInfo != null)
            {
                using var stream = streamInfo.Stream;
                baseIcon = new Icon(stream, new System.Drawing.Size(32, 32));
            }
        }
        catch
        {
            // Fallback handled below
        }

        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            if (baseIcon != null)
            {
                g.DrawIcon(baseIcon, new Rectangle(0, 0, 32, 32));
                baseIcon.Dispose();
            }
            else
            {
                using var bgBrush = new SolidBrush(Color.FromArgb(24, 24, 28));
                g.FillEllipse(bgBrush, 2, 2, 28, 28);
            }

            // Draw status pip in bottom-right corner
            using var pipBrush = new SolidBrush(indicatorColor);
            using var pipBorder = new Pen(Color.FromArgb(20, 20, 24), 2f);
            g.FillEllipse(pipBrush, 20, 20, 10, 10);
            g.DrawEllipse(pipBorder, 20, 20, 10, 10);
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
