using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CursorMate.Core;

namespace CursorMate.Companion;

internal sealed class CursorOverlay : IDisposable
{
    private const double HighlightPaddingDip = 4;

    private readonly HwndSource _panel;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private MonitorOverlay[] _overlays = [];
    private MonitorGeometry[] _monitors = [];
    private PixelPoint? _position;
    private int _previousMonitor = -1;
    private TimeSpan _lastFrame;
    private TimeSpan _nextDisplayCheck;
    private bool _refreshDisplays = true;
    private Func<PixelRect?>? _readTarget;

    public CursorOverlay(HwndSource panel)
    {
        _panel = panel;
        _panel.AddHook(PanelMessage);
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60)
        };
        _timer.Tick += (_, _) => DrawFrame();
    }

    public event Action? TargetLost;
    public bool ReduceMotion { get; set; }
    public bool IsVisible => _timer.IsEnabled;

    public void PointAt(Func<PixelRect?> readTarget) => _readTarget = readTarget;

    public void Park() => _readTarget = null;

    public void Show()
    {
        _lastFrame = _clock.Elapsed;
        _refreshDisplays = true;
        _timer.Start();
    }

    public void Hide()
    {
        _timer.Stop();
        Park();
        ReleaseOverlays();
    }

    public void Dispose()
    {
        Hide();
        if (!_panel.IsDisposed) _panel.RemoveHook(PanelMessage);
    }

    private void DrawFrame()
    {
        var now = _clock.Elapsed;
        var elapsed = now - _lastFrame;
        _lastFrame = now;
        if (_refreshDisplays || now >= _nextDisplayCheck)
        {
            // Poll as a fallback for topology changes that don't send a message
            // to the panel, such as some driver or remote desktop changes.
            _nextDisplayCheck = now + TimeSpan.FromMilliseconds(500);
            RefreshDisplays();
        }

        if (NativeDesktop.ReadPointer() is not { } mouse)
        {
            Render(null, null);
            ResetMotion();
            return;
        }

        var target = _readTarget?.Invoke();
        CursorPlacement? destination = target is { } bounds ? CursorMath.PointAt(bounds, _monitors) : null;
        if (destination is null && _readTarget is not null)
        {
            Park();
            target = null;
            TargetLost?.Invoke();
        }

        destination ??= CursorMath.ParkNear(mouse, _monitors);
        if (destination is not { } anchor)
        {
            Render(null, null);
            ResetMotion();
            return;
        }

        var position = _position is { } previous && !ReduceMotion && SystemParameters.ClientAreaAnimation
            ? CursorMath.StepToward(previous, anchor.Origin, elapsed)
            : anchor.Origin;
        _position = position;
        var frame = CursorMath.PlaceAnimationFrame(position, _previousMonitor, _monitors);
        if (frame is { } placement && !CursorMath.CanDrawPointer(placement, _monitors, mouse, target))
        {
            frame = CursorMath.CanDrawPointer(anchor, _monitors, mouse, target) ? anchor : null;
            if (frame.HasValue) _position = anchor.Origin;
        }

        if (frame is { } visible) _previousMonitor = visible.MonitorIndex;
        Render(frame, target, _monitors[anchor.MonitorIndex].DpiScale);
    }

    private void Render(CursorPlacement? frame, PixelRect? target, double highlightScale = 1)
    {
        var outline = target?.Inflate(HighlightPaddingDip * highlightScale);
        for (int i = 0; i < _overlays.Length; i++)
        {
            var origin = frame is { } placement && placement.MonitorIndex == i ? placement.Origin : (PixelPoint?)null;
            _overlays[i].Draw(origin, outline, highlightScale);
        }
    }

    private void RefreshDisplays()
    {
        var bounds = NativeDesktop.GetMonitorBounds();
        if (!_refreshDisplays && bounds.SequenceEqual(_monitors.Select(monitor => monitor.Bounds))) return;

        ReleaseOverlays();
        _refreshDisplays = false;
        var overlays = new List<MonitorOverlay>(bounds.Length);
        try
        {
            foreach (var rect in bounds)
            {
                overlays.Add(new MonitorOverlay(rect, () => _refreshDisplays = true));
            }
        }
        catch (Win32Exception)
        {
            // A display can disappear between enumeration and HWND creation.
            // Release any partial set and retry on the next topology check.
            foreach (var overlay in overlays) overlay.Dispose();
            return;
        }

        _overlays = overlays.ToArray();
        _monitors = _overlays.Select(overlay => overlay.Geometry).ToArray();
    }

    private void ReleaseOverlays()
    {
        foreach (var overlay in _overlays) overlay.Dispose();
        _overlays = [];
        _monitors = [];
        ResetMotion();
    }

    private void ResetMotion()
    {
        _position = null;
        _previousMonitor = -1;
    }

    private IntPtr PanelMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeDesktop.DisplayChangedMessage) _refreshDisplays = true;
        return IntPtr.Zero;
    }
}
