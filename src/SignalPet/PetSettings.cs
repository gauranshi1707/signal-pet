namespace SignalPet;

public sealed class PetSettings
{
    public int WalkDurationMilliseconds { get; set; } = 900;
    public int PauseDurationMilliseconds { get; set; } = 1200;
    public int PetSize { get; set; } = 128;
    public DesktopEdge Edge { get; set; } = DesktopEdge.Right;

    public PetAnimationOptions ToAnimationOptions() => new(
        TimeSpan.FromMilliseconds(WalkDurationMilliseconds),
        TimeSpan.FromMilliseconds(PauseDurationMilliseconds),
        TimeSpan.FromMilliseconds(WalkDurationMilliseconds),
        PetSize,
        Edge);
}
