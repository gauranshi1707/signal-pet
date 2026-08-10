using System.Windows;

namespace SignalPet;

/// <summary>
/// Serializes pet appearances so notification bursts do not create overlapping overlays.
/// </summary>
public sealed class PetAnimationController
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task PlayAsync(PetAnimationOptions options)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        await RunAnimationAsync(options).ConfigureAwait(false);
    }

    /// <summary>Starts an animation only when no pet is currently visible.</summary>
    public async Task<bool> TryPlayAsync(PetAnimationOptions options)
    {
        if (!await _gate.WaitAsync(0).ConfigureAwait(false))
        {
            return false;
        }

        await RunAnimationAsync(options).ConfigureAwait(false);
        return true;
    }

    private async Task RunAnimationAsync(PetAnimationOptions options)
    {
        try
        {
            PetOverlayWindow? overlay = null;
            Task playback = Task.CompletedTask;

            // The gate may resume on a worker thread after a burst of events.
            // Keep every WPF operation on the application's UI dispatcher.
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                overlay = new PetOverlayWindow(options, new PlaceholderPetVisualFactory());
                overlay.Show();
                playback = overlay.PlayAsync();
            });

            await playback.ConfigureAwait(false);
            await Application.Current.Dispatcher.InvokeAsync(() => overlay?.Close());
        }
        finally
        {
            _gate.Release();
        }
    }
}
