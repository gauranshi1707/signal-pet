namespace SignalPet;

/// <summary>
/// Serializes pet appearances so notification bursts do not create overlapping overlays.
/// </summary>
public sealed class PetAnimationController
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task PlayAsync(PetAnimationOptions options)
    {
        await _gate.WaitAsync();
        try
        {
            var overlay = new PetOverlayWindow(options, new PlaceholderPetVisualFactory());
            overlay.Show();
            await overlay.PlayAsync();
            overlay.Close();
        }
        finally
        {
            _gate.Release();
        }
    }
}
