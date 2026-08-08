using System.Windows;

namespace SignalPet;

/// <summary>
/// Boundary for artwork. A later implementation can return a sprite-sheet, GIF,
/// PNG-sequence, or any other WPF visual without changing overlay behavior.
/// </summary>
public interface IPetVisualFactory
{
    FrameworkElement Create(double size);
}
