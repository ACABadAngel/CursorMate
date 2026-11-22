using System.Windows;

namespace CursorMate.Companion;

public partial class MainWindow : Window
{
    private OverlayWindow? _overlay;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) => _overlay?.Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _overlay = new OverlayWindow();
        _overlay.Show();
        Status.Text = "Idle — AI cursor rests next to your mouse";
        Activate();
    }

    private void DemoTarget_Click(object sender, RoutedEventArgs e)
    {
        _overlay?.PointAt(() =>
        {
            if (!DemoTarget.IsVisible) return null;
            Point topLeft = DemoTarget.PointToScreen(new Point(0, 0));
            Point bottomRight = DemoTarget.PointToScreen(
                new Point(DemoTarget.ActualWidth, DemoTarget.ActualHeight));
            return new Rect(topLeft, bottomRight);
        });
        Status.Text = "Pointing — your real mouse remains untouched";
    }

    private void Park_Click(object sender, RoutedEventArgs e)
    {
        _overlay?.Park();
        Status.Text = "Idle — tracking the real cursor locally";
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (_overlay is null) return;
        if (_overlay.IsVisible)
        {
            _overlay.Hide();
            Status.Text = "Hidden — no overlay displayed";
        }
        else
        {
            _overlay.Show();
            _overlay.Park();
            Status.Text = "Idle — AI cursor is visible";
        }
    }
}
