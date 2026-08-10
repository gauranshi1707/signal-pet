using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SignalPet;

/// <summary>
/// Creates the local, text-free tuxedo-cat animation from cropped PNG frames.
/// The source artwork stays independent from the overlay and can be replaced by
/// changing only this factory and the files in Assets/Cat.
/// </summary>
public sealed class TuxedoCatVisualFactory : IPetVisualFactory
{
    public IPetVisual Create(double size, DesktopEdge edge) => new TuxedoCatVisual(size, edge);
}

public sealed class TuxedoCatVisual : IPetVisual
{
    private const double WalkFrameMilliseconds = 115;
    private const double ReactionMilliseconds = 650;
    private readonly Image _image;
    private readonly Grid _container;
    private readonly DesktopEdge _edge;
    private readonly BitmapSource[] _walkFrames;
    private readonly BitmapSource _idleFrame;
    private readonly BitmapSource _reactionFrame;
    private readonly ScaleTransform _facingTransform = new(1, 1);
    private ImageSource? _currentFrame;

    public TuxedoCatVisual(double size, DesktopEdge edge)
    {
        _edge = edge;
        _walkFrames = [
            LoadFrame("walk-01.png"),
            LoadFrame("walk-02.png"),
            LoadFrame("walk-03.png"),
            LoadFrame("walk-04.png")
        ];
        _idleFrame = LoadFrame("idle.png");
        _reactionFrame = LoadFrame("reaction.png");

        _image = new Image
        {
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true,
            IsHitTestVisible = true,
            RenderTransformOrigin = new Point(0.5, 1),
            RenderTransform = _facingTransform
        };

        _container = new Grid
        {
            Width = size,
            Height = size,
            IsHitTestVisible = false
        };
        _container.Children.Add(_image);
        Element = _container;
        SetFrame(_idleFrame);
    }

    public FrameworkElement Element { get; }

    public void Update(PetAnimationPhase phase, TimeSpan phaseElapsed, TimeSpan pauseDuration)
    {
        switch (phase)
        {
            case PetAnimationPhase.Entering:
                _facingTransform.ScaleX = _edge == DesktopEdge.Right ? -1 : 1;
                SetFrame(_walkFrames[(int)(phaseElapsed.TotalMilliseconds / WalkFrameMilliseconds) % _walkFrames.Length]);
                break;

            case PetAnimationPhase.Leaving:
                _facingTransform.ScaleX = _edge == DesktopEdge.Right ? 1 : -1;
                SetFrame(_walkFrames[(int)(phaseElapsed.TotalMilliseconds / WalkFrameMilliseconds) % _walkFrames.Length]);
                break;

            default:
                _facingTransform.ScaleX = 1;
                SetFrame(phaseElapsed.TotalMilliseconds < Math.Min(ReactionMilliseconds, pauseDuration.TotalMilliseconds)
                    ? _reactionFrame
                    : _idleFrame);
                break;
        }
    }

    private void SetFrame(ImageSource frame)
    {
        if (!ReferenceEquals(_currentFrame, frame))
        {
            _image.Source = frame;
            _currentFrame = frame;
        }
    }

    private static BitmapSource LoadFrame(string fileName)
    {
        var frame = new BitmapImage();
        frame.BeginInit();
        frame.UriSource = new Uri($"pack://application:,,,/Assets/Cat/{fileName}", UriKind.Absolute);
        frame.CacheOption = BitmapCacheOption.OnLoad;
        frame.EndInit();
        frame.Freeze();
        return frame;
    }
}
