namespace CursorMate.Core;

public readonly record struct PixelPoint(double X, double Y);

public readonly record struct PixelRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
    public bool IsValid => Width > 0 && Height > 0 &&
        double.IsFinite(Left) && double.IsFinite(Top) &&
        double.IsFinite(Right) && double.IsFinite(Bottom);

    public bool Contains(PixelPoint point) =>
        point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    public PixelRect Intersect(PixelRect other)
    {
        double left = Math.Max(Left, other.Left);
        double top = Math.Max(Top, other.Top);
        return new PixelRect(left, top, Math.Min(Right, other.Right) - left,
            Math.Min(Bottom, other.Bottom) - top);
    }

    public PixelRect Inflate(double pixels) =>
        new(Left - pixels, Top - pixels, Width + 2 * pixels, Height + 2 * pixels);
}

public readonly record struct MonitorGeometry(PixelRect Bounds, double DpiScale);

public readonly record struct CursorPlacement(int MonitorIndex, PixelPoint Origin);

/// <summary>Resolves anchors and animation in physical desktop pixels.</summary>
public static class CursorMath
{
    public const double PointerWidthDip = 14;
    public const double PointerHeightDip = 18;
    private const double ParkOffsetDip = 18;
    private const double TargetGapDip = 12;
    private const double TimeConstantSeconds = 0.075;

    public static CursorPlacement? ParkNear(PixelPoint mouse, IReadOnlyList<MonitorGeometry> monitors)
    {
        int index = FindMonitor(mouse, monitors);
        if (index < 0) return null;

        var monitor = monitors[index];
        var bounds = monitor.Bounds;
        var artwork = PointerBounds(mouse, monitor);
        if (artwork.Width > bounds.Width || artwork.Height > bounds.Height) return null;

        double gap = ParkOffsetDip * monitor.DpiScale;
        double right = mouse.X + gap;
        double left = mouse.X - gap - artwork.Width;
        double below = mouse.Y + gap;
        double above = mouse.Y - gap - artwork.Height;
        double x = right + artwork.Width <= bounds.Right ? right : left;
        double y = below + artwork.Height <= bounds.Bottom ? below : above;
        var origin = FitPointer(new PixelPoint(x, y), monitor);

        // On a small display, clamping may erase one of the offsets. Keep the
        // other offset intact, or suppress the avatar if neither side fits.
        bool hasClearance = (x >= bounds.Left && x + artwork.Width <= bounds.Right) ||
            (y >= bounds.Top && y + artwork.Height <= bounds.Bottom);
        return hasClearance ? new CursorPlacement(index, origin) : null;
    }

    public static CursorPlacement? PointAt(PixelRect target, IReadOnlyList<MonitorGeometry> monitors)
    {
        if (!target.IsValid) return null;

        CursorPlacement? best = null;
        double bestArea = 0;
        for (int i = 0; i < monitors.Count; i++)
        {
            var visible = target.Intersect(monitors[i].Bounds);
            if (!visible.IsValid) continue;

            double area = visible.Width * visible.Height;
            if (area <= bestArea) continue;

            if (BesideTarget(target, visible, monitors[i]) is { } origin)
            {
                best = new CursorPlacement(i, origin);
                bestArea = area;
            }
        }

        return best;
    }

    public static CursorPlacement? PlaceAnimationFrame(PixelPoint position, int previousMonitor,
        IReadOnlyList<MonitorGeometry> monitors)
    {
        int index = FindMonitor(position, monitors);
        // While the path crosses a gap, hold at the edge of the last monitor.
        if (index < 0) index = previousMonitor;
        if (index < 0 || index >= monitors.Count) return null;

        var monitor = monitors[index];
        var artwork = PointerBounds(position, monitor);
        if (artwork.Width > monitor.Bounds.Width || artwork.Height > monitor.Bounds.Height) return null;
        return new CursorPlacement(index, FitPointer(position, monitor));
    }

    public static PixelRect PointerBounds(PixelPoint origin, MonitorGeometry monitor) =>
        new(origin.X, origin.Y, PointerWidthDip * monitor.DpiScale, PointerHeightDip * monitor.DpiScale);

    public static bool CanDrawPointer(CursorPlacement placement, IReadOnlyList<MonitorGeometry> monitors,
        PixelPoint? realPointer, PixelRect? target)
    {
        var artwork = PointerBounds(placement.Origin, monitors[placement.MonitorIndex]);
        return (realPointer is not { } mouse || !artwork.Contains(mouse)) &&
            (target is not { } bounds || !artwork.Intersect(bounds).IsValid);
    }

    public static PixelPoint StepToward(PixelPoint current, PixelPoint destination, TimeSpan elapsed)
    {
        double remaining = Math.Exp(-Math.Max(0, elapsed.TotalSeconds) / TimeConstantSeconds);
        double dx = (current.X - destination.X) * remaining;
        double dy = (current.Y - destination.Y) * remaining;
        return dx * dx + dy * dy <= 1
            ? destination
            : new PixelPoint(destination.X + dx, destination.Y + dy);
    }

    private static int FindMonitor(PixelPoint point, IReadOnlyList<MonitorGeometry> monitors)
    {
        for (int i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].Bounds.Contains(point)) return i;
        }

        return -1;
    }

    private static PixelPoint? BesideTarget(PixelRect target, PixelRect visible, MonitorGeometry monitor)
    {
        var bounds = monitor.Bounds;
        var artwork = PointerBounds(default, monitor);
        if (artwork.Width > bounds.Width || artwork.Height > bounds.Height) return null;

        double gap = TargetGapDip * monitor.DpiScale;
        double centerX = visible.Left + (visible.Width - artwork.Width) / 2;
        double centerY = visible.Top + (visible.Height - artwork.Height) / 2;
        double right = target.Right + gap;
        double left = target.Left - gap - artwork.Width;
        double below = target.Bottom + gap;
        double above = target.Top - gap - artwork.Height;

        if (right + artwork.Width <= bounds.Right) return FitPointer(new PixelPoint(right, centerY), monitor);
        if (left >= bounds.Left) return FitPointer(new PixelPoint(left, centerY), monitor);
        if (below + artwork.Height <= bounds.Bottom) return FitPointer(new PixelPoint(centerX, below), monitor);
        if (above >= bounds.Top) return FitPointer(new PixelPoint(centerX, above), monitor);
        return null;
    }

    private static PixelPoint FitPointer(PixelPoint origin, MonitorGeometry monitor)
    {
        var artwork = PointerBounds(origin, monitor);
        var bounds = monitor.Bounds;
        return new PixelPoint(
            Math.Clamp(origin.X, bounds.Left, bounds.Right - artwork.Width),
            Math.Clamp(origin.Y, bounds.Top, bounds.Bottom - artwork.Height));
    }
}
