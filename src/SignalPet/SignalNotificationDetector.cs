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
            var access = await _listener.RequestAccessAsync();
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

        var notification = sender.GetNotification(args.UserNotificationId);
        if (notification is not null && IsFromSignal(notification))
        {
            SignalToastReceived?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool IsFromSignal(UserNotification notification)
    {
        // Signal's classic desktop install does not expose a stable public package
        // identity. Match only the app display name; do not inspect toast content.
        var displayName = notification.AppInfo.DisplayInfo.DisplayName;
        return string.Equals(displayName, "Signal", StringComparison.OrdinalIgnoreCase);
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
