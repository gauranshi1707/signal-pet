# Stage 1 — Windows notification detection findings

## Conclusion

The supported mechanism is `Windows.UI.Notifications.Management.UserNotificationListener` in a **packaged** Windows desktop application. It receives lifecycle events for toast notifications and, after explicit user consent, can retrieve a notification's app metadata. Signal Pet uses only the originating app's display name to decide whether the event came from Signal. It deliberately never reads the notification's visual payload, bindings, or text elements.

Signal Desktop is installed on this development computer as the classic Win32 application at `%LOCALAPPDATA%\Programs\signal-desktop\Signal.exe`, rather than as an AppX/MSIX package. It still emits Windows toast notifications when its desktop notification preference and Windows notifications are enabled. The listener sees Windows toast records, not Signal messages, and has no access to Signal's database, IPC, or network traffic.

## What Windows exposes

`UserNotificationListener` exposes:

- a consent-gated list of current app toasts;
- an in-memory `NotificationChanged` event while Signal Pet is running;
- a background-task trigger for added/dismissed toast changes in a packaged app; and
- app identity metadata plus the notification visual payload.

The POC observes only `Id`, change kind, and `AppInfo.DisplayInfo.DisplayName`. It does not call `UserNotification.Notification`, `Visual`, `GetBinding`, or `GetTextElements`.

## Suppression limitation

Windows has no third-party API that atomically intercepts another application's toast *before* Windows shows its banner and replaces it with a custom UI. A listener can be notified only after the notification platform has accepted the toast. Although `RemoveNotification(id)` exists, it is post-delivery, races the banner, and would remove the notification from Notification Center. Signal Pet will not use it.

The recommended supported configuration is:

1. Keep Signal's desktop notifications enabled so it emits a Windows toast.
2. In Windows' per-app notification settings for Signal, disable **Show notification banners** while leaving notifications enabled/history available.
3. Grant Signal Pet notification-listener access in Windows when prompted.

This produces the closest reliable behavior: Windows records the Signal notification for the listener, Windows does not show the banner, and Signal Pet reacts with the pet. If a particular Windows build or Signal release stops publishing a toast when banners are disabled, Signal Pet cannot reliably detect it by supported means. The app must report that limitation rather than scrape Signal UI, watch Signal data, or use hooks to defeat Windows notifications.

## POC behavior

`SignalNotificationDetector` seeds currently existing notification IDs and then reacts only to a future `Added` event from an application whose display name is exactly `Signal`. Its window reports a count, but never a sender, message, title, preview, or toast text.

The project must be MSIX-packaged before running: the package manifest declares the `userNotificationListener` capability and `runFullTrust`. The first launch must request user consent from the UI thread. Permission can later be revoked; Windows can return an empty notification set instead of throwing.

## Validation status

- Windows 10 build 26200 and a classic Signal Desktop installation were confirmed on the development machine.
- The development machine has the .NET 8 runtime but **no .NET SDK or MSIX build tooling**, so this POC cannot be compiled/installed locally yet.
- A live Signal message was not generated during Stage 1. After installing the SDK and package tooling, validation is: install the signed development MSIX, grant access, send a test message to Signal Desktop, and verify that the POC count increments without any notification text appearing in its UI or logs.

## Stage 2 implementation note

Stage 2 adds an independent WPF overlay and a manual test button. The overlay is borderless, transparent, topmost, excluded from the taskbar, configured not to activate, and marked with `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`, and `WS_EX_TOOLWINDOW` so normal pointer input passes through it. It currently uses the primary monitor work area; monitor selection becomes a persisted option in Stage 4.

## Sources

- [Microsoft: Notification listener](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/notification-listener)
- [Microsoft: Notifications and Do Not Disturb in Windows](https://support.microsoft.com/en-us/windows/experience/notifications-and-do-not-disturb-in-windows)
- [Signal: Troubleshooting notifications](https://support.signal.org/hc/en-us/articles/360007318711-Troubleshooting-Notifications)
