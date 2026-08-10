using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace SignalPet;

public partial class PetOverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WmNCHitTest = 0x0084;
    private const int HtClient = 1;
    private const int HtTransparent = -1;
    private const long WsExNoActivate = 0x08000000L;
    private const long WsExToolWindow = 0x00000080L;
    private readonly PetAnimationOptions _options;
    private readonly IPetVisual _pet;
    private readonly Stopwatch _clock = new();
    private readonly DispatcherTimer _timer;
    private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Rect _workArea;
    private readonly SignalDesktopActivator _signalDesktopActivator = new();
    private HwndSource? _windowSource;

    public PetOverlayWindow(PetAnimationOptions options, IPetVisualFactory visualFactory)
    {
        _options = options;
        _pet = visualFactory.Create(options.PetSize, options.Edge);
        _workArea = SystemParameters.WorkArea;
        InitializeComponent();

        Left = _workArea.Left;
        Top = _workArea.Top;
        Width = _workArea.Width;
        Height = _workArea.Height;
        Stage.IsHitTestVisible = true;
        _pet.Element.IsHitTestVisible = true;
        _pet.Element.MouseLeftButtonUp += (_, _) => _signalDesktopActivator.ActivateOrLaunch();
        Stage.Children.Add(_pet.Element);

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnFrame;
        SourceInitialized += (_, _) => ConfigureInputRouting();
    }

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
        Canvas.SetLeft(_pet.Element, x);
        Canvas.SetTop(_pet.Element, Height - _options.PetSize - 24);
        AnimatePet(elapsed);
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

    private void AnimatePet(TimeSpan elapsed)
    {
        var pauseStart = _options.WalkInDuration;
        var pauseEnd = pauseStart + _options.PauseDuration;
        var isEntering = elapsed < pauseStart;
        var isLeaving = elapsed >= pauseEnd;
        var phase = isEntering
            ? PetAnimationPhase.Entering
            : isLeaving ? PetAnimationPhase.Leaving : PetAnimationPhase.Reacting;
        var phaseElapsed = isEntering
            ? elapsed
            : isLeaving ? elapsed - pauseEnd : elapsed - pauseStart;
        var isWalking = isEntering || isLeaving;

        _pet.Update(phase, phaseElapsed, _options.PauseDuration);
        var verticalOffset = isWalking
            ? Math.Sin(phaseElapsed.TotalMilliseconds / 75) * 4
            : Math.Sin(phaseElapsed.TotalMilliseconds / 330) * 1.25;
        _pet.Element.RenderTransform = new TranslateTransform(0, verticalOffset);
    }

    private void ConfigureInputRouting()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExNoActivate | WsExToolWindow));
        _windowSource = HwndSource.FromHwnd(handle);
        _windowSource?.AddHook(WindowProcedure);
    }

    private IntPtr WindowProcedure(IntPtr handle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmNCHitTest)
        {
            return IntPtr.Zero;
        }

        var screenPoint = new Point(
            (short)(lParam.ToInt64() & 0xffff),
            (short)((lParam.ToInt64() >> 16) & 0xffff));
        var windowPoint = PointFromScreen(screenPoint);
        var petOrigin = _pet.Element.TranslatePoint(new Point(), this);
        var petBounds = new Rect(petOrigin, _pet.Element.RenderSize);

        handled = true;
        return petBounds.Contains(windowPoint) ? new IntPtr(HtClient) : new IntPtr(HtTransparent);
    }

    private static double Lerp(double from, double to, double progress) => from + ((to - from) * Math.Clamp(progress, 0, 1));

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}
