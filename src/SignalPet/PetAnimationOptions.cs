namespace SignalPet;

/// <summary>
/// Animation settings. Stage 4 will persist and expose these to the user.
/// </summary>
public sealed record PetAnimationOptions(
    TimeSpan WalkInDuration,
    TimeSpan PauseDuration,
    TimeSpan WalkOutDuration,
    double PetSize,
    DesktopEdge Edge)
{
    public static PetAnimationOptions Default { get; } = new(
        WalkInDuration: TimeSpan.FromMilliseconds(900),
        PauseDuration: TimeSpan.FromSeconds(2),
        WalkOutDuration: TimeSpan.FromMilliseconds(900),
        PetSize: 128,
        Edge: DesktopEdge.Right);
}

public enum DesktopEdge
{
    Left,
    Right
}
