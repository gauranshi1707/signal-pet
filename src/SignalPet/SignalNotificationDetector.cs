using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace SignalPet;

/// <summary>
/// Observes Windows toast metadata only. This class deliberately never accesses
/// UserNotification.Notification, Visual, bindings, or text elements.
/// </summary>
public sealed class SignalNotificationDetector : IDisposable
{
    private readonly HashSet<uint> _knownNotificationIds = [];
    private readonly UserNotificationListener _listener = UserNotificationListener.Current;
    private bool _started;

    /// <summary>Raised when Windows adds a toast whose owning app is Signal.</summary>
    public event EventHandler? SignalToastReceived;

    public async Task<DetectorStartResult> StartAsync()
    {
        if (_started)
        {
            return DetectorStartResult.Active;
        }

        try
        {
            var access = _listener.GetAccessStatus();
            if (access != UserNotificationListenerAccessStatus.Allowed)
            {
                access = await _listener.RequestAccessAsync();
            }
            if (access != UserNotificationListenerAccessStatus.Allowed)
            {
                return access == UserNotificationListenerAccessStatus.Denied
                    ? DetectorStartResult.PermissionDenied
                    : DetectorStartResult.Unavailable;
            }

            // Seed existing IDs so the POC reacts only to future notifications.
            var existing = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            foreach (var notification in existing)
            {
                _knownNotificationIds.Add(notification.Id);
            }

            _listener.NotificationChanged += OnNotificationChanged;
            _started = true;
            return DetectorStartResult.Active;
        }
        catch (UnauthorizedAccessException)
        {
            // Expected when the executable has no MSIX identity/capability.
            return DetectorStartResult.Unpackaged;
        }
    }

    private void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added || !_knownNotificationIds.Add(args.UserNotificationId))
        {
            return;
        }

        try
        {
            var notification = sender.GetNotification(args.UserNotificationId);
            if (notification is null)
            {
                return;
            }

            // This is the only toast metadata deliberately read by Signal Pet.
            var appUserModelId = notification.AppInfo.AppUserModelId;

            if (IsFromSignal(appUserModelId))
            {
                _listener.RemoveNotification(notification.Id);
                SignalToastReceived?.Invoke(this, EventArgs.Empty);
            }
        }
        catch
        {
            // A toast can disappear between the event and metadata lookup.
            // There is intentionally no logging of notification data.
        }
    }

    private static bool IsFromSignal(string? appUserModelId)
    {
        // Examine only the originating application's AUMID, never its toast payload.
        return string.Equals(appUserModelId, "org.whispersystems.signal-desktop", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_started)
        {
            _listener.NotificationChanged -= OnNotificationChanged;
            _started = false;
        }
    }
}

public enum DetectorStartResult
{
    Active,
    PermissionDenied,
    Unpackaged,
    Unavailable
}
