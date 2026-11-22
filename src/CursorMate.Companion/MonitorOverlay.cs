using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using CursorMate.Core;

namespace CursorMate.Companion;

internal sealed class MonitorOverlay : IDisposable
{
    private const int WsPopup = unchecked((int)0x80000000);
    private const int WsExTopmost = 0x00000008;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int SwHide = 0;
    private const int SwShowNoActivate = 8;

    private readonly HwndSource _source;
    private readonly Polygon _pointer;
    private readonly Rectangle _highlight;
    private bool _visible;

    public MonitorOverlay(PixelRect bounds, Action refreshDisplays)
    {
        // HwndSource accepts physical pixels here. Starting on the owning monitor
        // gives WPF the correct DPI for the surface from its first layout pass.
        var parameters = new HwndSourceParameters("CursorMate overlay", (int)bounds.Width, (int)bounds.Height)
        {
            PositionX = (int)bounds.Left,
            PositionY = (int)bounds.Top,
            WindowStyle = WsPopup,
            ExtendedWindowStyle = WsExTopmost | WsExTransparent | WsExToolWindow | WsExNoActivate,
            UsesPerPixelOpacity = true,
            AcquireHwndFocusInMenuMode = false
        };

        _source = new HwndSource(parameters);
        Geometry = new MonitorGeometry(bounds, _source.CompositionTarget!.TransformToDevice.M11);
        _source.DpiChanged += (_, _) => refreshDisplays();

        _highlight = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromRgb(112, 220, 204)),
            Visibility = Visibility.Collapsed
        };
        _pointer = new Polygon
        {
            Points = new PointCollection
            {
                new(1.5, 1.5), new(1.5, 13.5), new(4.5, 10.5), new(7.5, 16.5),
                new(10, 15.5), new(7, 9), new(12.5, 9)
            },
            Fill = new SolidColorBrush(Color.FromRgb(111, 235, 216)),
            Stroke = new SolidColorBrush(Color.FromRgb(22, 61, 59)),
            StrokeThickness = 1.25,
            StrokeLineJoin = PenLineJoin.Round,
            Visibility = Visibility.Collapsed
        };
        _source.RootVisual = new Canvas
        {
            ClipToBounds = true,
            IsHitTestVisible = false,
            Children = { _highlight, _pointer }
        };
    }

    public MonitorGeometry Geometry { get; }

    public void Draw(PixelPoint? pointer, PixelRect? outline, double highlightScale)
    {
        double scale = Geometry.DpiScale;
        var monitor = Geometry.Bounds;
        bool highlightVisible = false;
        if (outline is { } bounds)
        {
            // Draw the original outline and let the canvas clip it at the seam,
            // so a target straddling two monitors doesn't acquire a false edge.
            highlightVisible = bounds.Intersect(monitor).IsValid;
            if (highlightVisible)
            {
                Place(_highlight, new PixelPoint(bounds.Left, bounds.Top));
                _highlight.Width = bounds.Width / scale;
                _highlight.Height = bounds.Height / scale;
                _highlight.StrokeThickness = 2 * highlightScale / scale;
                _highlight.RadiusX = _highlight.RadiusY = 6 * highlightScale / scale;
            }
        }

        if (pointer is { } origin) Place(_pointer, origin);
        _highlight.Visibility = highlightVisible ? Visibility.Visible : Visibility.Collapsed;
        _pointer.Visibility = pointer.HasValue ? Visibility.Visible : Visibility.Collapsed;

        bool visible = highlightVisible || pointer.HasValue;
        if (visible == _visible) return;
        NativeDesktop.ShowWindow(_source.Handle, visible ? SwShowNoActivate : SwHide);
        _visible = visible;
    }

    public void Dispose() => _source.Dispose();

    private void Place(UIElement element, PixelPoint origin)
    {
        Canvas.SetLeft(element, (origin.X - Geometry.Bounds.Left) / Geometry.DpiScale);
        Canvas.SetTop(element, (origin.Y - Geometry.Bounds.Top) / Geometry.DpiScale);
    }
}
