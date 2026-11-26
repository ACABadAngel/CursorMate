using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Automation;

namespace CursorMate.Companion;

public partial class MainWindow : Window
{
    private OverlayWindow? _overlay;
    private CancellationTokenSource? _inspection;
    private bool _expanded;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) => { _inspection?.Cancel(); _overlay?.Close(); };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Top = SystemParameters.WorkArea.Top + 10;
        CenterDock();
        _overlay = new OverlayWindow();
        _overlay.Show();
    }

    private void CenterDock() => Left = SystemParameters.WorkArea.Left +
        (SystemParameters.WorkArea.Width - Width) / 2;

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        _expanded = !_expanded;
        Details.Visibility = _expanded ? Visibility.Visible : Visibility.Collapsed;
        if (!_expanded) _overlay?.Park();
        ExpandButton.Content = _expanded ? "⌃" : "⌄";
        double targetWidth = _expanded ? Math.Min(440, SystemParameters.WorkArea.Width) : 236;
        double targetHeight = _expanded ? Math.Min(550, SystemParameters.WorkArea.Height - 20) : 48;
        var duration = TimeSpan.FromMilliseconds(SystemParameters.ClientAreaAnimation ? 220 : 1);
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(WidthProperty, new DoubleAnimation(targetWidth, duration) { EasingFunction = easing });
        BeginAnimation(HeightProperty, new DoubleAnimation(targetHeight, duration) { EasingFunction = easing });
        BeginAnimation(LeftProperty, new DoubleAnimation(SystemParameters.WorkArea.Left +
            (SystemParameters.WorkArea.Width - targetWidth) / 2, duration) { EasingFunction = easing });
    }

    private async void Inspect_Click(object sender, RoutedEventArgs e)
    {
        _inspection?.Cancel();
        var source = new CancellationTokenSource();
        _inspection = source;
        InspectButton.IsEnabled = false;
        ContextText.Text = "Switch to your chosen window now. Reading local control types in 3 seconds…";
        try
        {
            await Task.Delay(3000, source.Token);
            var handle = GetForegroundWindow();
            if (handle == IntPtr.Zero) { ContextText.Text = "No foreground window."; return; }
            GetWindowThreadProcessId(handle, out uint pid);
            if (pid == (uint)Environment.ProcessId)
            { ContextText.Text = "Select another application and try again."; return; }

            // Only control types, never text, values, screenshots, or clipboard contents.
            var result = await Task.Run(() =>
            {
                source.Token.ThrowIfCancellationRequested();
                using var process = Process.GetProcessById((int)pid);
                var root = AutomationElement.FromHandle(handle);
                var walker = TreeWalker.ControlViewWalker;
                var counts = new SortedDictionary<string, int>();
                var node = walker.GetFirstChild(root);
                for (int i = 0; node != null && i < 40; i++)
                {
                    source.Token.ThrowIfCancellationRequested();
                    var type = node.Current.ControlType.ProgrammaticName;
                    counts[type] = counts.GetValueOrDefault(type) + 1;
                    node = walker.GetNextSibling(node);
                }
                return process.ProcessName + " — " + (counts.Count == 0 ? "No exposed child controls." :
                    string.Join(", ", counts.Select(kv => kv.Key.Replace("ControlType.", "") + ": " + kv.Value)));
            }, source.Token).WaitAsync(TimeSpan.FromSeconds(5), source.Token);
            if (!source.IsCancellationRequested && GetForegroundWindow() == handle)
                ContextText.Text = result;
            else if (!source.IsCancellationRequested)
                ContextText.Text = "Window changed; context discarded.";
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!source.IsCancellationRequested) ContextText.Text = "Window unavailable or inspection timed out."; }
        finally
        {
            if (_inspection == source) { _inspection = null; InspectButton.IsEnabled = true; }
            // A timed-out UIA provider may still return; never publish its result.
        }
    }

    private void DemoTarget_Click(object sender, RoutedEventArgs e)
    {
        _overlay?.PointAt(() =>
        {
            if (!DemoTarget.IsVisible || !_expanded) return null;
            return new Rect(DemoTarget.PointToScreen(new Point(0, 0)),
                DemoTarget.PointToScreen(new Point(DemoTarget.ActualWidth, DemoTarget.ActualHeight)));
        });
        Status.Text = "Pointing — real mouse untouched";
    }

    private void Park_Click(object sender, RoutedEventArgs e)
    {
        _overlay?.Park();
        Status.Text = "Idle — tracking locally";
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (_overlay is null) return;
        if (_overlay.IsVisible) { _overlay.Hide(); Status.Text = "Cursor hidden"; }
        else { _overlay.Show(); _overlay.Park(); Status.Text = "Cursor visible"; }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _inspection?.Cancel();
        _overlay?.Park();
        _overlay?.Hide();
        ContextText.Text = "Context cleared.";
        Status.Text = "Stopped";
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
}
