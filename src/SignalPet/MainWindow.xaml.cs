using System.Windows;

namespace SignalPet;

public partial class MainWindow : Window
{
    private readonly SignalNotificationDetector _detector = new();
    private int _detectedCount;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) => _detector.Dispose();
        _detector.SignalToastReceived += OnSignalToastReceived;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _detector.StartAsync();
            StatusText.Text = result switch
            {
                DetectorStartResult.Active => "Detection is active. New Signal toasts will be counted without reading their contents.",
                DetectorStartResult.PermissionDenied => "Windows notification access was denied. Enable Signal Pet under Settings > Privacy & security > Notifications.",
                DetectorStartResult.Unpackaged => "This proof of concept must be installed as an MSIX package before Windows will grant notification-listener access.",
                _ => "Detection is unavailable on this Windows installation. See docs/STAGE-1-RESEARCH.md."
            };
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Detector could not start: {exception.GetType().Name}. See docs/STAGE-1-RESEARCH.md.";
        }
    }

    private void OnSignalToastReceived(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _detectedCount++;
            StatusText.Text = $"Detection is active. Signal toast detected: {_detectedCount}. No notification text was accessed.";
        });
    }
}
