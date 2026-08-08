using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SignalPet;

/// <summary>A deliberately text-free vector pet used until final artwork is supplied.</summary>
public sealed class PlaceholderPetVisualFactory : IPetVisualFactory
{
    public FrameworkElement Create(double size)
    {
        var canvas = new Canvas { Width = size, Height = size, IsHitTestVisible = false };
        var ink = Color.FromRgb(43, 55, 76);
        var belly = Color.FromRgb(123, 183, 255);

        var body = new Ellipse
        {
            Width = size * 0.62,
            Height = size * 0.52,
            Fill = new SolidColorBrush(belly),
            Stroke = new SolidColorBrush(ink),
            StrokeThickness = size * 0.045
        };
        canvas.Children.Add(body);
        Canvas.SetLeft(body, size * 0.19);
        Canvas.SetTop(body, size * 0.30);

        var head = new Ellipse
        {
            Width = size * 0.48,
            Height = size * 0.44,
            Fill = new SolidColorBrush(Color.FromRgb(151, 201, 255)),
            Stroke = new SolidColorBrush(ink),
            StrokeThickness = size * 0.045
        };
        canvas.Children.Add(head);
        Canvas.SetLeft(head, size * 0.26);
        Canvas.SetTop(head, size * 0.08);

        foreach (var x in new[] { 0.29, 0.57 })
        {
            var eye = new Ellipse { Width = size * 0.07, Height = size * 0.09, Fill = new SolidColorBrush(ink) };
            canvas.Children.Add(eye);
            Canvas.SetLeft(eye, size * x);
            Canvas.SetTop(eye, size * 0.23);
        }

        foreach (var x in new[] { 0.23, 0.57 })
        {
            var foot = new Ellipse { Width = size * 0.20, Height = size * 0.10, Fill = new SolidColorBrush(ink) };
            canvas.Children.Add(foot);
            Canvas.SetLeft(foot, size * x);
            Canvas.SetTop(foot, size * 0.76);
        }

        return canvas;
    }
}
