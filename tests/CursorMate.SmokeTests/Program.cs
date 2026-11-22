using CursorMate.Core;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}

static bool Near(double first, double second) => Math.Abs(first - second) < 0.000001;

var primary = new MonitorGeometry(new PixelRect(0, 0, 3440, 1440), 1);
var portrait = new MonitorGeometry(new PixelRect(-1200, -320, 1200, 1920), 1.5);
var upper = new MonitorGeometry(new PixelRect(700, -1080, 1920, 1080), 2);
MonitorGeometry[] monitors = [primary, portrait, upper];

Check(CursorMath.ParkNear(new PixelPoint(250, 250), [primary]) ==
    new CursorPlacement(0, new PixelPoint(268, 268)), "single-monitor idle offset");
Check(CursorMath.ParkNear(new PixelPoint(-200, 420), [primary, portrait]) ==
    new CursorPlacement(1, new PixelPoint(-173, 447)), "negative-origin portrait at 150% scale");
Check(CursorMath.ParkNear(new PixelPoint(800, -500), monitors) ==
    new CursorPlacement(2, new PixelPoint(836, -464)), "third monitor above primary at 200% scale");

foreach (var monitor in monitors)
{
    var bounds = monitor.Bounds;
    PixelPoint[] corners =
    [
        new(bounds.Left, bounds.Top), new(bounds.Right - 1, bounds.Top),
        new(bounds.Left, bounds.Bottom - 1), new(bounds.Right - 1, bounds.Bottom - 1)
    ];
    foreach (var corner in corners)
    {
        var parked = CursorMath.ParkNear(corner, [monitor]);
        var artwork = CursorMath.PointerBounds(parked.GetValueOrDefault().Origin, monitor);
        double dx = Math.Max(0, Math.Max(artwork.Left - corner.X, corner.X - artwork.Right));
        double dy = Math.Max(0, Math.Max(artwork.Top - corner.Y, corner.Y - artwork.Bottom));
        double gapDip = Math.Sqrt(dx * dx + dy * dy) / monitor.DpiScale;
        Check(parked.HasValue && artwork.Intersect(bounds) == artwork &&
            !artwork.Contains(corner) && gapDip >= 18 && gapDip <= 28,
            $"whole avatar parks clear of hotspot at {corner} with scale {monitor.DpiScale}");
    }
}

Check(CursorMath.ParkNear(new PixelPoint(-1, 300), monitors)?.MonitorIndex == 1 &&
    CursorMath.ParkNear(new PixelPoint(0, 300), monitors)?.MonitorIndex == 0,
    "shared boundary belongs to exactly one monitor");
Check(CursorMath.ParkNear(new PixelPoint(-100, -500), monitors) is null,
    "idle coordinate in a virtual desktop gap is rejected");
Check(CursorMath.ParkNear(new PixelPoint(250, 250), []) is null, "disconnected displays have no anchor");
Check(CursorMath.ParkNear(new PixelPoint(15, 15), [new MonitorGeometry(new PixelRect(0, 0, 30, 30), 1)])
    is null, "no parking space suppresses the avatar");

var target = new PixelRect(400, 350, 120, 40);
Check(CursorMath.PointAt(target, monitors) == new CursorPlacement(0, new PixelPoint(532, 361)),
    "target anchor preserves the teaching gap");
Check(CursorMath.PointAt(new PixelRect(2500, -200, 119, 100), monitors) ==
    new CursorPlacement(2, new PixelPoint(2448, -168)), "target at the right edge flips left at 200%");
Check(CursorMath.PointAt(new PixelRect(0, 400, 3440, 50), [primary])?.Origin.Y == 462,
    "wide target falls back to a vertical anchor");
Check(CursorMath.PointAt(new PixelRect(-80, 100, 100, 40), monitors) ==
    new CursorPlacement(1, new PixelPoint(-119, 106.5)), "straddling target uses its largest visible monitor");
Check(CursorMath.PointAt(new PixelRect(-200, -500, 100, 40), monitors) is null,
    "target outside real displays is rejected");
Check(CursorMath.PointAt(new PixelRect(0, 0, 0, 0), monitors) is null &&
    CursorMath.PointAt(new PixelRect(double.NaN, 0, 20, 20), monitors) is null &&
    CursorMath.PointAt(new PixelRect(0, 0, double.PositiveInfinity, 20), monitors) is null,
    "empty and non-finite targets are rejected");
Check(CursorMath.PointAt(primary.Bounds, [primary]) is null, "full-screen target has no safe adjacent anchor");
Check(CursorMath.PointAt(target, [portrait, upper]) is null,
    "target on a removed display no longer resolves");

var crossing = CursorMath.PlaceAnimationFrame(new PixelPoint(-5, 300), 1, monitors);
Check(crossing == new CursorPlacement(1, new PixelPoint(-21, 300)) &&
    CursorMath.PlaceAnimationFrame(new PixelPoint(0, 300), 1, monitors) ==
        new CursorPlacement(0, new PixelPoint(0, 300)), "whole avatar hands off at a mixed-DPI seam");
Check(CursorMath.PlaceAnimationFrame(new PixelPoint(350, -600), 1, monitors) ==
    new CursorPlacement(1, new PixelPoint(-21, -320)) &&
    CursorMath.PlaceAnimationFrame(new PixelPoint(700, -600), 1, monitors)?.MonitorIndex == 2,
    "animation stays on the last display across a gap and resumes on entry");
Check(CursorMath.PlaceAnimationFrame(new PixelPoint(350, -600), -1, monitors) is null,
    "animation starting in a gap has no visible destination");

Check(!CursorMath.CanDrawPointer(new CursorPlacement(0, new PixelPoint(250, 250)), monitors,
    new PixelPoint(253, 253), null), "animated frame cannot cover the real pointer hotspot");
Check(!CursorMath.CanDrawPointer(new CursorPlacement(0, new PixelPoint(390, 350)), monitors,
    new PixelPoint(300, 300), target), "animated frame cannot cover the target");

var start = new PixelPoint(-1000, 0);
var end = new PixelPoint(1400, 600);
var oneFrame = CursorMath.StepToward(start, end, TimeSpan.FromMilliseconds(300));
var manyFrames = start;
for (int frame = 0; frame < 20; frame++)
{
    manyFrames = CursorMath.StepToward(manyFrames, end, TimeSpan.FromMilliseconds(15));
}
Check(Near(oneFrame.X, manyFrames.X) && Near(oneFrame.Y, manyFrames.Y),
    "easing depends on elapsed time across frame rates");
Check(CursorMath.StepToward(start, end, TimeSpan.FromSeconds(1)) == end,
    "easing settles exactly after a long frame");

Console.WriteLine("All core smoke tests passed.");
