using System.Windows;

namespace SignalPet;

/// <summary>
/// Boundary for artwork. A later implementation can return a sprite-sheet, GIF,
/// PNG-sequence, or any other WPF visual without changing overlay behavior.
/// </summary>
public interface IPetVisualFactory
{
    IPetVisual Create(double size, DesktopEdge edge);
}

/// <summary>Artwork whose frames can respond to the overlay's animation phase.</summary>
public interface IPetVisual
{
    FrameworkElement Element { get; }

    void Update(PetAnimationPhase phase, TimeSpan phaseElapsed, TimeSpan pauseDuration);
}

public enum PetAnimationPhase
{
    Entering,
    Reacting,
    Leaving
}
