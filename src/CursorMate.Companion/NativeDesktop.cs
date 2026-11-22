using System.Runtime.InteropServices;
using CursorMate.Core;

namespace CursorMate.Companion;

internal static class NativeDesktop
{
    internal const int DisplayChangedMessage = 0x007E;
    private const uint MonitorDefaultToNull = 0;

    internal static PixelRect[] GetMonitorBounds()
    {
        var monitors = new List<PixelRect>();
        bool success = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (IntPtr monitor, IntPtr dc, ref NativeRect bounds, IntPtr data) =>
            {
                var rect = new PixelRect(bounds.Left, bounds.Top,
                    bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
                // Enumeration can also report invisible mirroring-driver surfaces.
                var center = new NativePoint
                {
                    X = bounds.Left + (bounds.Right - bounds.Left) / 2,
                    Y = bounds.Top + (bounds.Bottom - bounds.Top) / 2
                };
                if (rect.IsValid && MonitorFromPoint(center, MonitorDefaultToNull) == monitor)
                {
                    monitors.Add(rect);
                }
                return true;
            }, IntPtr.Zero);

        return success
            ? monitors.Distinct().OrderBy(bounds => bounds.Left).ThenBy(bounds => bounds.Top)
                .ThenBy(bounds => bounds.Width).ThenBy(bounds => bounds.Height).ToArray()
            : [];
    }

    internal static PixelPoint? ReadPointer() =>
        GetPhysicalCursorPos(out var point) ? new PixelPoint(point.X, point.Y) : null;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X, Y;
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool MonitorCallback(IntPtr monitor, IntPtr dc, ref NativeRect bounds, IntPtr data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorCallback callback, IntPtr data);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicalCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(IntPtr window, int command);
}
