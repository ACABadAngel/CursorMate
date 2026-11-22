using CursorMate.Core;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CursorMate.Companion;

/// <summary>
/// Decorative click-through companion cursor. It NEVER injects OS mouse input.
/// Current prototype uses one virtual-screen WPF window; per-monitor DPI is future work.
/// </summary>
public sealed class OverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;

    private readonly Polygon _pointer;
    private readonly Rectangle _highlight;
    private readonly DispatcherTimer _timer;
    private Func<Rect?>? _targetBounds;
    private Point? _currentLocal;

    public OverlayWindow()
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowActivated = false;
        ShowInTaskbar = false;
        Focusable = false;
        IsHitTestVisible = false;

        var surface = new Canvas { Background = Brushes.Transparent, IsHitTestVisible = false };
        _highlight = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromRgb(112, 220, 204)),
            StrokeThickness = 2,
            RadiusX = 6,
            RadiusY = 6,
            Fill = Brushes.Transparent,
            Visibility = Visibility.Collapsed
        };
        _pointer = new Polygon
        {
            Points = new PointCollection
            {
                new(0, 0), new(0, 21), new(5, 16),
                new(10, 27), new(14, 25), new(9, 14), new(17, 14)
            },
            Fill = new SolidColorBrush(Color.FromRgb(111, 235, 216)),
            Stroke = new SolidColorBrush(Color.FromRgb(22, 61, 59)),
            StrokeThickness = 1.5,
            IsHitTestVisible = false
        };
        surface.Children.Add(_highlight);
        surface.Children.Add(_pointer);
        Content = surface;

        SourceInitialized += (_, _) =>
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            long current = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
            SetWindowLongPtr(handle, GwlExStyle,
                new IntPtr(current | WsExTransparent | WsExToolWindow | WsExNoActivate));
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) => DrawFrame();
        Loaded += (_, _) => _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    public void PointAt(Func<Rect?> targetBounds) => _targetBounds = targetBounds;

    public void Park()
    {
        _targetBounds = null;
        _highlight.Visibility = Visibility.Collapsed;
    }

    private void DrawFrame()
    {
        if (!IsVisible || ActualWidth <= 0 || ActualHeight <= 0) return;
        if (!GetCursorPos(out var realPointer)) return;

        Point deviceOrigin = PointToScreen(new Point(0, 0));
        Point deviceEnd = PointToScreen(new Point(ActualWidth, ActualHeight));
        var desktop = new PixelRect(deviceOrigin.X, deviceOrigin.Y,
            deviceEnd.X - deviceOrigin.X, deviceEnd.Y - deviceOrigin.Y);
        if (!desktop.IsValid) return;

        PixelPoint desired;
        Rect? target = _targetBounds?.Invoke();
        if (target is { IsEmpty: false } bounds && bounds.Width > 0 && bounds.Height > 0)
        {
            desired = CursorMath.PointAt(
                new PixelRect(bounds.Left, bounds.Top, bounds.Width, bounds.Height), desktop);
            Point tl = PointFromScreen(bounds.TopLeft);
            Point br = PointFromScreen(bounds.BottomRight);
            Canvas.SetLeft(_highlight, tl.X - 4);
            Canvas.SetTop(_highlight, tl.Y - 4);
            _highlight.Width = Math.Max(1, br.X - tl.X + 8);
            _highlight.Height = Math.Max(1, br.Y - tl.Y + 8);
            _highlight.Visibility = Visibility.Visible;
        }
        else
        {
            desired = CursorMath.ParkNear(new PixelPoint(realPointer.X, realPointer.Y), desktop);
            _highlight.Visibility = Visibility.Collapsed;
        }

        Point targetLocal = PointFromScreen(new Point(desired.X, desired.Y));
        Point next = _currentLocal is { } current
            ? new Point(current.X + (targetLocal.X - current.X) * 0.28,
                        current.Y + (targetLocal.Y - current.Y) * 0.28)
            : targetLocal;
        _currentLocal = next;
        Canvas.SetLeft(_pointer, next.X);
        Canvas.SetTop(_pointer, next.Y);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint lpPoint);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}
