using System.Windows;

namespace SignalPet;

public partial class MainWindow : Window
{
    private readonly SignalNotificationDetector _detector = new();
    private readonly PetAnimationController _petAnimation = new();
    private readonly SettingsService _settingsService = new();
    private readonly StartupRegistrationService _startupService = new();
    private PetSettings _settings = new();
    private int _detectedCount;

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        WalkText.Text = _settings.WalkDurationMilliseconds.ToString();
        PauseText.Text = _settings.PauseDurationMilliseconds.ToString();
        SizeText.Text = _settings.PetSize.ToString();
        EdgeBox.ItemsSource = Enum.GetValues<DesktopEdge>();
        EdgeBox.SelectedItem = _settings.Edge;
        StartupBox.IsChecked = _startupService.IsEnabled();
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

    private async void OnSignalToastReceived(object? sender, EventArgs e)
    {
        var animation = await Dispatcher.InvokeAsync(() =>
        {
            _detectedCount++;
            StatusText.Text = $"Detection is active. Signal toast detected: {_detectedCount}. No notification text was accessed.";
            return _petAnimation.PlayAsync(_settings.ToAnimationOptions());
        });

        await animation;
    }

    private async void OnTestPetAnimation(object sender, RoutedEventArgs e)
    {
        await _petAnimation.PlayAsync(_settings.ToAnimationOptions());
    }

    private void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WalkText.Text, out var walk) || walk is < 100 or > 10000 ||
            !int.TryParse(PauseText.Text, out var pause) || pause is < 0 or > 30000 ||
            !int.TryParse(SizeText.Text, out var size) || size is < 32 or > 512 ||
            EdgeBox.SelectedItem is not DesktopEdge edge)
        {
            StatusText.Text = "Settings must use: walk 100–10000 ms, pause 0–30000 ms, size 32–512.";
            return;
        }
        _settings = new PetSettings { WalkDurationMilliseconds = walk, PauseDurationMilliseconds = pause, PetSize = size, Edge = edge };
        _settingsService.Save(_settings);
        StatusText.Text = "Settings saved.";
    }

    private void OnStartupChanged(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            _startupService.SetEnabled(StartupBox.IsChecked == true);
    }
}
