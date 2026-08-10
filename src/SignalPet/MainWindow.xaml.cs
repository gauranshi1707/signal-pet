using System.Windows;

namespace SignalPet;

public partial class MainWindow : Window
{
    private readonly SignalNotificationDetector _detector = new();
    private readonly PetAnimationController _petAnimation = new();
    private readonly SettingsService _settingsService = new();
    private readonly StartupRegistrationService _startupService = new();
    private PetSettings _settings = new();

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
        StatusText.Text = "Checking notification-listener access…";
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
                DetectorStartResult.Active => "Notification-listener access is active. Use Test pet animation to preview the pet.",
                DetectorStartResult.PermissionDenied => "Windows notification-listener access was denied. Enable it in Windows Settings, then restart Signal Pet.",
                DetectorStartResult.Unpackaged => "Notification-listener access requires the installed MSIX package.",
                _ => "Windows notification-listener access is unavailable."
            };
        }
        catch (Exception exception)
        {
            StatusText.Text = exception is System.Runtime.InteropServices.COMException comException
                ? $"Notification-listener startup failed: COMException 0x{comException.HResult:X8}."
                : $"Notification-listener startup failed: {exception.GetType().Name}.";
        }
    }

    private async void OnSignalToastReceived(object? sender, EventArgs e)
    {
        await _petAnimation.TryPlayAsync(_settings.ToAnimationOptions() with { Edge = DesktopEdge.Right });
    }

    private async void OnTestPetAnimation(object sender, RoutedEventArgs e)
    {
        var testOptions = _settings.ToAnimationOptions() with { PauseDuration = TimeSpan.FromSeconds(2) };
        await _petAnimation.PlayAsync(testOptions);
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
