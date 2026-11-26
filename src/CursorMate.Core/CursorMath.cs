namespace CursorMate.Core;

public readonly record struct PixelPoint(double X, double Y);
public readonly record struct PixelRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
    public bool IsValid => double.IsFinite(Left) && double.IsFinite(Top) &&
        double.IsFinite(Right) && double.IsFinite(Bottom) && Width > 0 && Height > 0;
    public bool Contains(PixelPoint p) => p.X >= Left && p.X < Right && p.Y >= Top && p.Y < Bottom;
    public PixelRect Intersect(PixelRect b) => new(Math.Max(Left,b.Left), Math.Max(Top,b.Top),
        Math.Max(0,Math.Min(Right,b.Right)-Math.Max(Left,b.Left)),
        Math.Max(0,Math.Min(Bottom,b.Bottom)-Math.Max(Top,b.Top)));
}
public readonly record struct MarkerPlacement(PixelPoint Origin, double Rotation);

public static class CursorMath
{
    public static PixelPoint ParkNear(PixelPoint mouse, PixelRect visibleBounds, double offset = 24, double size = 18)
    {
        if (!visibleBounds.IsValid) return mouse;
        double x = mouse.X + offset, y = mouse.Y + offset;
        if (x + size > visibleBounds.Right) x = mouse.X - offset - size;
        if (y + size > visibleBounds.Bottom) y = mouse.Y - offset - size;
        return Clamp(new(x,y), visibleBounds,size);
    }

    public static PixelPoint PointAt(PixelRect target, PixelRect visibleBounds) =>
        PlaceMarker(target, visibleBounds,18,12).Origin;

    // The icon's natural direction is northwest (-135 degrees).
    // The cursor square sits outside the highlighted control whenever space permits.
    public static MarkerPlacement PlaceMarker(PixelRect target, PixelRect bounds, double size, double gap)
    {
        if (!target.IsValid || !bounds.IsValid || !double.IsFinite(size) || size <= 0 ||
            !double.IsFinite(gap) || gap < 0) throw new ArgumentException("Invalid marker geometry.");
        var clipped = target.Intersect(bounds);
        if (!clipped.IsValid) throw new ArgumentException("Target outside monitor.");
        double cx = clipped.Left + clipped.Width/2, cy = clipped.Top + clipped.Height/2;
        PixelPoint[] candidates = [
            new(clipped.Right+gap,cy-size/2),
            new(clipped.Left-gap-size,cy-size/2),
            new(cx-size/2,clipped.Bottom+gap),
            new(cx-size/2,clipped.Top-gap-size)];
        PixelPoint chosen = Clamp(candidates[0],bounds,size);
        double best = double.PositiveInfinity;
        foreach (var p in candidates)
        {
            var c = Clamp(p,bounds,size);
            var overlap = new PixelRect(c.X,c.Y,size,size).Intersect(clipped);
            double cost = overlap.Width*overlap.Height*1000 + Math.Abs(c.X-p.X)+Math.Abs(c.Y-p.Y);
            if (cost < best) { best=cost; chosen=c; }
        }
        double angle = Math.Atan2(cy-chosen.Y-size/2,cx-chosen.X-size/2)*180/Math.PI + 135;
        return new(chosen,angle);
    }
    private static PixelPoint Clamp(PixelPoint p, PixelRect b, double size) =>
        new(Math.Clamp(p.X,b.Left,Math.Max(b.Left,b.Right-size)),
            Math.Clamp(p.Y,b.Top,Math.Max(b.Top,b.Bottom-size)));
}
