using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace SignalPet;

public partial class PetOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExNoActivate = 0x08000000L;
    private const long WsExToolWindow = 0x00000080L;
    private readonly PetAnimationOptions _options;
    private readonly FrameworkElement _pet;
    private readonly Stopwatch _clock = new();
    private readonly DispatcherTimer _timer;
    private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Rect _workArea;

    public PetOverlayWindow(PetAnimationOptions options, IPetVisualFactory visualFactory)
    {
        _options = options;
        _pet = visualFactory.Create(options.PetSize);
        _workArea = SystemParameters.WorkArea;
        InitializeComponent();

        Left = _workArea.Left;
        Top = _workArea.Top;
        Width = _workArea.Width;
        Height = _workArea.Height;
        Stage.Children.Add(_pet);

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnFrame;
        SourceInitialized += (_, _) => MakeInputTransparent();
    }

    protected override bool ShowWithoutActivation => true;

    public Task PlayAsync()
    {
        _clock.Start();
        _timer.Start();
        return _completed.Task;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        var elapsed = _clock.Elapsed;
        var total = _options.WalkInDuration + _options.PauseDuration + _options.WalkOutDuration;
        if (elapsed >= total)
        {
            _timer.Stop();
            _clock.Stop();
            _completed.TrySetResult();
            return;
        }

        var x = CalculateX(elapsed);
        Canvas.SetLeft(_pet, x);
        Canvas.SetTop(_pet, Height - _options.PetSize - 24);
        AnimateWalkBounce(elapsed);
    }

    private double CalculateX(TimeSpan elapsed)
    {
        var outside = _options.PetSize + 8;
        var restingX = _options.Edge == DesktopEdge.Right
            ? Width - _options.PetSize - 48
            : 48;

        if (elapsed < _options.WalkInDuration)
        {
            var progress = elapsed.TotalMilliseconds / _options.WalkInDuration.TotalMilliseconds;
            return _options.Edge == DesktopEdge.Right
                ? Lerp(Width + 8, restingX, progress)
                : Lerp(-outside, restingX, progress);
        }

        if (elapsed < _options.WalkInDuration + _options.PauseDuration)
        {
            return restingX;
        }

        var outElapsed = elapsed - _options.WalkInDuration - _options.PauseDuration;
        var outProgress = outElapsed.TotalMilliseconds / _options.WalkOutDuration.TotalMilliseconds;
        return _options.Edge == DesktopEdge.Right
            ? Lerp(restingX, Width + 8, outProgress)
            : Lerp(restingX, -outside, outProgress);
    }

    private void AnimateWalkBounce(TimeSpan elapsed)
    {
        var isWalking = elapsed < _options.WalkInDuration || elapsed > _options.WalkInDuration + _options.PauseDuration;
        _pet.RenderTransform = new TranslateTransform(0, isWalking ? Math.Sin(elapsed.TotalMilliseconds / 75) * 4 : 0);
    }

    private void MakeInputTransparent()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExTransparent | WsExNoActivate | WsExToolWindow));
    }

    private static double Lerp(double from, double to, double progress) => from + ((to - from) * Math.Clamp(progress, 0, 1));

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}
