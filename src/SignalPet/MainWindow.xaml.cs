using System.Windows;

namespace SignalPet;

public partial class MainWindow : Window
{
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
        StatusText.Text = "Use Test pet animation to preview the text-free pet overlay.";
    }

    private async void OnTestPetAnimation(object sender, RoutedEventArgs e)
    {
        var testOptions = _settings.ToAnimationOptions() with
        {
            PauseDuration = TimeSpan.FromSeconds(2),
            Edge = DesktopEdge.Right
        };
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
