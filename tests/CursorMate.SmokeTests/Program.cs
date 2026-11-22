using CursorMate.Core;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}

var screen = new PixelRect(0, 0, 1920, 1080);
var parked = CursorMath.ParkNear(new PixelPoint(250, 250), screen);
Check(parked.X > 250 && parked.Y > 250, "idle cursor parks beside real pointer");
var edge = CursorMath.ParkNear(new PixelPoint(1915, 1075), screen);
Check(edge.X < 1915 && edge.Y < 1075, "cursor flips near screen edges");
var target = CursorMath.PointAt(new PixelRect(400, 350, 120, 40), screen);
Check(target.X > 520 && target.Y >= 350, "points beside the target");
var negative = new PixelRect(-1440, -100, 1440, 900);
var negPoint = CursorMath.ParkNear(new PixelPoint(-1390, 20), negative);
Check(negPoint.X >= negative.Left && negPoint.X <= negative.Right,
    "negative-origin display coordinates");
var rejected = false;
try { CursorMath.PointAt(new PixelRect(0, 0, 0, 0), screen); }
catch (ArgumentException) { rejected = true; }
Check(rejected, "invalid target rejected");
Console.WriteLine("All core smoke tests passed.");
