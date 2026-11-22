using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CursorMate.Core;

namespace CursorMate.Companion;

public partial class MainWindow : Window
{
    private CursorOverlay? _overlay;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) => _overlay?.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_overlay is not null || PresentationSource.FromVisual(this) is not HwndSource source) return;
        _overlay = new CursorOverlay(source);
        _overlay.TargetLost += () => Status.Text = "Demo target unavailable. AI cursor parked.";
        _overlay.Show();
        Status.Text = "AI cursor parked beside your mouse.";
    }

    private void DemoTarget_Click(object sender, RoutedEventArgs e)
    {
        if (_overlay is not { IsVisible: true }) return;
        _overlay.PointAt(ReadDemoTarget);
        Status.Text = "Pointing at the demo button.";
    }

    private void Park_Click(object sender, RoutedEventArgs e)
    {
        _overlay?.Park();
        Status.Text = _overlay is { IsVisible: true }
            ? "AI cursor parked beside your mouse."
            : "AI cursor hidden.";
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (_overlay is null) return;
        if (_overlay.IsVisible)
        {
            _overlay.Hide();
            Status.Text = "AI cursor hidden.";
            ToggleCursor.Content = "Show AI cursor";
            DemoTarget.IsEnabled = false;
        }
        else
        {
            _overlay.Show();
            Status.Text = "AI cursor parked beside your mouse.";
            ToggleCursor.Content = "Hide AI cursor";
            DemoTarget.IsEnabled = true;
        }
    }

    private void ReduceMotion_Changed(object sender, RoutedEventArgs e)
    {
        if (_overlay is not null) _overlay.ReduceMotion = ReduceMotion.IsChecked == true;
    }

    private PixelRect? ReadDemoTarget()
    {
        if (!IsActive || WindowState == WindowState.Minimized ||
            !DemoTarget.IsLoaded || !DemoTarget.IsVisible || !DemoTarget.IsEnabled ||
            PresentationSource.FromVisual(DemoTarget)?.RootVisual is not UIElement root)
            return null;

        var visible = DemoTarget.TransformToAncestor(root).TransformBounds(new Rect(DemoTarget.RenderSize));
        visible.Intersect(new Rect(root.RenderSize));

        // Ancestor clips include WPF's scrolled viewport and layout clipping.
        for (Visual? visual = DemoTarget; visual is not null && visual != root;
             visual = VisualTreeHelper.GetParent(visual) as Visual)
        {
            if (VisualTreeHelper.GetClip(visual) is { } clip)
            {
                visible.Intersect(visual.TransformToAncestor(root).TransformBounds(clip.Bounds));
            }
            if (visible.IsEmpty || visible.Width <= 0 || visible.Height <= 0) return null;
        }

        Point topLeft = root.PointToScreen(visible.TopLeft);
        Point bottomRight = root.PointToScreen(visible.BottomRight);
        return new PixelRect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
    }
}
