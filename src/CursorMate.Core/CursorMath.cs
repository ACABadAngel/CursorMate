namespace CursorMate.Core;

/// <summary>Physical-pixel geometry used independently of the WPF visual layer.</summary>
public readonly record struct PixelPoint(double X, double Y);

public readonly record struct PixelRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
    public bool IsValid => Width > 0 && Height > 0;
}

/// <summary>Pure, side-effect-free helper. It never sends OS pointer input.</summary>
public static class CursorMath
{
    private const double CursorSize = 18;

    public static PixelPoint ParkNear(PixelPoint mouse, PixelRect visibleBounds, double offset = 26)
    {
        if (!visibleBounds.IsValid) return mouse;

        double x = mouse.X + offset;
        double y = mouse.Y + offset;
        if (x + CursorSize > visibleBounds.Right) x = mouse.X - offset - CursorSize;
        if (y + CursorSize > visibleBounds.Bottom) y = mouse.Y - offset - CursorSize;
        return Clamp(new PixelPoint(x, y), visibleBounds);
    }

    public static PixelPoint PointAt(PixelRect target, PixelRect visibleBounds)
    {
        if (!target.IsValid || !visibleBounds.IsValid)
            throw new ArgumentException("Target and visible bounds must have positive dimensions.");

        double x = target.Right + 12;
        if (x + CursorSize > visibleBounds.Right) x = target.Left - CursorSize - 12;

        return Clamp(new PixelPoint(x, target.Top + target.Height / 2 - CursorSize / 2), visibleBounds);
    }

    private static PixelPoint Clamp(PixelPoint point, PixelRect bounds) =>
        new(
            Math.Clamp(point.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - CursorSize)),
            Math.Clamp(point.Y, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - CursorSize))
        );
}
