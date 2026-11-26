using CursorMate.Core;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CursorMate.Companion;

// Small per-monitor surfaces avoid virtual-desktop DPI scaling and large transparent bitmaps.
public sealed class OverlayWindow
{
    private readonly DispatcherTimer _timer;
    private readonly Surface _cursor = new();
    private readonly Surface _marker = new();
    private readonly System.Windows.Shapes.Path _pointer;
    private readonly Rectangle _highlight;
    private Func<Rect?>? _targetBounds;
    private PixelPoint? _current;
    private IntPtr _lastMonitor;
    private const double IconDip = 18;
    public bool IsVisible { get; private set; }

    public OverlayWindow()
    {
        _pointer = new System.Windows.Shapes.Path {
            Data = Geometry.Parse("F0 M3.10042 6.29005C2.40168 4.30935 4.30916 2.40186 6.28987 3.1006L18.8719 7.54103C20.9168 8.263 21.1141 11.0361 19.2801 12.0693L19.0965 12.1641L14.535 14.293C14.4552 14.3302 14.3867 14.3878 14.3367 14.459L14.2928 14.5352L12.1639 19.0967C11.2172 21.1253 8.2861 20.9829 7.54084 18.8721L3.10042 6.29005ZM5.62483 4.98732C5.22864 4.84749 4.8473 5.22882 4.98713 5.62501L9.42756 18.2061C9.57668 18.6281 10.162 18.6566 10.3514 18.251L12.4803 13.6895L12.5819 13.4942C12.8374 13.0525 13.2237 12.6977 13.6893 12.4805L18.2508 10.3516C18.6564 10.1622 18.6279 9.57686 18.2059 9.42775L5.62483 4.98732Z"),
            Fill = new SolidColorBrush(Color.FromRgb(117,235,201)),
            Width = IconDip, Height = IconDip, Stretch = Stretch.Uniform,
            RenderTransformOrigin = new Point(.5,.5)
        };
        _cursor.Content = new Grid { Children = { _pointer } };
        _highlight = new Rectangle {
            Stroke = new SolidColorBrush(Color.FromRgb(117,235,201)),
            StrokeThickness = 1.5, RadiusX = 8, RadiusY = 8,
            Fill = new SolidColorBrush(Color.FromArgb(18,117,235,201)),
            Margin = new Thickness(2)
        };
        _marker.Content = _highlight;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_,_) => DrawFrame();
    }
    public void Show() { IsVisible = true; _current = null; _timer.Start(); DrawFrame(); }
    public void Hide() { IsVisible = false; _timer.Stop(); _cursor.Hide(); _marker.Hide(); }
    public void Close() { Hide(); _cursor.Close(); _marker.Close(); }
    public void PointAt(Func<Rect?> targetBounds) => _targetBounds = targetBounds;
    public void Park() { _targetBounds = null; _marker.Hide(); }

    private void DrawFrame()
    {
        if (!IsVisible || !GetCursorPos(out var mouse)) return;
        Rect? target = _targetBounds?.Invoke();
        bool hasTarget = target is { IsEmpty:false } t && t.Width > 0 && t.Height > 0;
        var anchor = hasTarget ? new NativePoint((int)(target!.Value.Left+target.Value.Width/2),
            (int)(target.Value.Top+target.Value.Height/2)) : mouse;
        var monitor = MonitorFromPoint(anchor,2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor,ref info)) return;
        var b = new PixelRect(info.Monitor.Left,info.Monitor.Top,
            info.Monitor.Right-info.Monitor.Left,info.Monitor.Bottom-info.Monitor.Top);
        double scale = 1;
        if (GetDpiForMonitor(monitor,0,out uint dpi,out _) == 0 && dpi > 0) scale = dpi/96.0;
        // Reserve space for a rotated icon: 26 DIP surface, centered 18 DIP glyph.
        int size = (int)Math.Ceiling(26*scale);
        PixelPoint desired;
        double rotation = 0;
        if (hasTarget)
        {
            var r = target!.Value;
            var clipped = new PixelRect(r.Left,r.Top,r.Width,r.Height).Intersect(b);
            if (clipped.IsValid)
            {
                var placement = CursorMath.PlaceMarker(clipped,b,size,8*scale);
                desired = placement.Origin; rotation = placement.Rotation;
                int pad = (int)Math.Ceiling(4*scale);
                int left = (int)Math.Max(b.Left,Math.Floor(clipped.Left)-pad);
                int top = (int)Math.Max(b.Top,Math.Floor(clipped.Top)-pad);
                int right = (int)Math.Min(b.Right,Math.Ceiling(clipped.Right)+pad);
                int bottom = (int)Math.Min(b.Bottom,Math.Ceiling(clipped.Bottom)+pad);
                _marker.Place(left,top,right-left,bottom-top);
            }
            else { _marker.Hide(); desired = CursorMath.ParkNear(new(anchor.X,anchor.Y),b,24*scale,size); }
        }
        else
        {
            _marker.Hide();
            desired = CursorMath.ParkNear(new(mouse.X,mouse.Y),b,24*scale,size);
        }
        // Do not interpolate across monitor gaps or incompatible DPI coordinate systems.
        if (_lastMonitor != monitor || !SystemParameters.ClientAreaAnimation) _current = null;
        _lastMonitor = monitor;
        var next = _current is { } p ? new PixelPoint(p.X+(desired.X-p.X)*.38,p.Y+(desired.Y-p.Y)*.38) : desired;
        _current = next;
        _pointer.RenderTransform = new RotateTransform(rotation);
        _cursor.Place((int)Math.Round(next.X),(int)Math.Round(next.Y),size,size);
    }

    private sealed class Surface : Window
    {
        private IntPtr _handle;
        private (int,int,int,int)? _last;
        public Surface()
        {
            WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize;
            AllowsTransparency=true; Background=Brushes.Transparent;
            Topmost=true; ShowActivated=false; ShowInTaskbar=false;
            Focusable=false; IsHitTestVisible=false;
            Width=26; Height=26;
            SourceInitialized += (_,_) =>
            {
                _handle=new WindowInteropHelper(this).Handle;
                var style=GetWindowLongPtr(_handle,-20).ToInt64();
                SetWindowLongPtr(_handle,-20,new IntPtr(style|0x20|0x80|0x08000000));
            };
        }
        public void Place(int x,int y,int w,int h)
        {
            if(w<=0||h<=0) { Hide(); return; }
            bool wasVisible=IsVisible;
            if(!wasVisible) { Show(); _last=null; }
            if(_last==(x,y,w,h)) return;
            if(SetWindowPos(_handle,new IntPtr(-1),x,y,w,h,0x0010|0x0200))
                _last=(x,y,w,h);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private record struct NativePoint(int X,int Y);
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor,Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint p);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(NativePoint p,uint flags);
    [DllImport("user32.dll",EntryPoint="GetMonitorInfoW")] private static extern bool GetMonitorInfo(IntPtr h,ref MonitorInfo info);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr h,int type,out uint x,out uint y);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr h,int n);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr h,int n,IntPtr value);
}
