# 🐈 Signal Pet

> **A quiet, privacy-first desktop companion for Signal.**

Signal Pet is a Windows desktop application that turns an incoming **Signal Desktop notification** into a small animated tuxedo cat that walks across your screen.

Instead of interrupting you with a visible message preview, Signal Pet is designed around a much simpler interaction:

```text
        Someone messages you on Signal
                    │
                    ▼
          Signal Desktop notification
                    │
                    ▼
             Signal Pet detects it
                    │
                    ▼
              🐈 Cat appears
                    │
                    ▼
          Click the cat if you want
                    │
                    ▼
           Signal Desktop opens
```

The application deliberately does **not** need to know who messaged you or what they said.

**No message content. No sender names. No sound. Just a cat.**

---

## ✨ Overview

Signal Pet was built around a simple question:

> **What if a notification could tell you that something happened without showing you what happened?**

Modern desktop notifications often expose information directly on the screen — message previews, sender names, conversation titles, and other contextual information — even when the user may not want that information displayed publicly.

Signal Pet takes a different approach.

It uses Windows' notification-listener infrastructure to detect that a notification belonging to **Signal Desktop** has arrived. Once the notification is identified, the notification object is not passed through the rest of the application.

The animation system receives only the fact that a matching Signal notification occurred.

That creates a deliberate privacy boundary:

```text
┌──────────────────────────────────────┐
│        Windows Notification API      │
│                                      │
│  Notification ID                     │
│  AppUserModelId                      │
│  Notification payload                │
└──────────────────┬───────────────────┘
                   │
                   │ Only identification
                   ▼
┌──────────────────────────────────────┐
│       Signal Notification Detector   │
│                                      │
│  Is this Signal Desktop?             │
└──────────────────┬───────────────────┘
                   │
                   │ Event only
                   ▼
┌──────────────────────────────────────┐
│        Pet Animation Controller      │
│                                      │
│  "A Signal notification arrived."   │
└──────────────────┬───────────────────┘
                   │
                   ▼
             🐈 Tuxedo Cat
```

The cat does not need access to the notification itself.

---

# 🔐 Privacy First

Privacy is a core architectural constraint rather than an additional feature.

Signal Pet does **not** read notification content to decide what animation to show.

The detector only accesses the minimum notification metadata required for its job.

### Accessed

* `notification.Id`

  * Used for notification identification and management.

* `notification.AppInfo.AppUserModelId`

  * Used to determine whether the notification originated from Signal Desktop.

The Signal Desktop AppUserModelId currently used by the detector is:

```text
org.whispersystems.signal-desktop
```

### Not accessed

Signal Pet does **not** inspect:

* Message body
* Sender name
* Conversation name
* Notification title
* Notification text
* Images
* Attachments
* Notification actions
* Message contents
* Conversation history

The application also does not attempt to access Signal's local message database.

---

## 🧱 Privacy Boundary

The notification detector and animation system are intentionally separated.

The detector receives the Windows notification event.

It determines whether the notification belongs to Signal.

If it does, it raises an application-level event.

Conceptually:

```csharp
SignalToastReceived?.Invoke(this, EventArgs.Empty);
```

The animation layer receives **no `UserNotification` object**.

This means the animation system does not have access to:

```text
sender
message
conversation
notification title
notification body
notification payload
```

It simply knows:

```text
A matching Signal notification occurred.
```

This separation makes the privacy boundary explicit in the architecture rather than relying solely on developer discipline.

---

# 🐈 The Pet

The visual component is an animated tuxedo cat designed to remain lightweight and unobtrusive.

The pet appears as a transparent desktop overlay and can walk across the screen when a Signal notification is detected.

The animation system is independent from the notification detector.

This means the pet implementation can be replaced without rewriting the notification system.

The visual layer is exposed through:

```text
IPetVisualFactory
```

while animation behavior is handled separately by:

```text
PetAnimationController
```

This separation makes it possible to replace the current cat with another visual implementation later without changing the notification-detection architecture.

---

# 🖱️ Click the Cat → Signal

Signal Pet is not intended to become another messaging interface.

When the cat appears, you can click it to return to Signal Desktop.

The interaction is:

```text
🐈
 │
 │ click
 ▼
SignalDesktopActivator
 │
 ├── Signal already running
 │        ↓
 │   Bring existing window
 │   to foreground
 │
 └── Signal not running
          ↓
      Launch Signal
```

The application attempts to activate an existing Signal Desktop window before launching a new instance.

Importantly, this interaction does **not** require access to the message that caused the notification.

Signal Pet currently brings Signal Desktop to the foreground rather than attempting to determine and open the exact conversation.

That is intentional.

---

# 🔔 Notification Behavior

There is an important Windows platform limitation.

Signal Pet uses:

```text
UserNotificationListener
```

to observe Windows toast notifications.

This API allows an application to observe and manage notifications after Windows has created them.

It does **not** provide a supported mechanism for a third-party application to intercept another application's toast before Windows displays it.

Therefore, Signal Pet cannot guarantee:

```text
Signal notification
       ↓
intercept before Windows
       ↓
prevent banner
       ↓
show cat
```

The actual supported flow is closer to:

```text
Signal notification
       ↓
Windows creates notification
       ↓
Signal Pet observes it
       ↓
Signal Pet reacts
       ↓
🐈 Cat appears
```

The application can remove the notification from Windows' notification state/history after detecting it, but this should not be treated as a guaranteed pre-display toast suppression mechanism.

---

# 🤫 Recommended Notification Setup

For the intended "cat instead of notification" experience, Signal's notification system should remain enabled so that Windows still generates a notification event.

At the same time, notification privacy settings can be configured so that the actual message content is not visually exposed.

The resulting experience is:

```text
                  Incoming message
                         │
                         ▼
              Signal notification event
                         │
                         ▼
                  Signal Pet detects
                         │
                         ▼
                     🐈 Cat
                         │
                         ▼
                  Click if needed
                         │
                         ▼
                 Signal Desktop
```

This allows Signal Pet to act as a visual notification companion without requiring access to the underlying message.

> **Important:** Windows controls the actual notification lifecycle. Signal Pet does not claim to provide a universal third-party toast-blocking mechanism.

---

# 🎯 Design Philosophy

Signal Pet follows three core principles.

## 1. Notification, not message

The application only needs to know:

> **Something happened in Signal.**

It does not need to know:

> **What happened in Signal.**

---

## 2. Minimum necessary data

Only the metadata necessary to identify and manage the notification is accessed.

Everything else stays outside the application.

---

## 3. Quiet interaction

A notification does not need to demand attention.

Signal Pet intentionally replaces a conventional visual interruption with something subtle:

```text
🐈
```

No sound.

No message preview.

No sender information.

No additional content.

Just an animated indication that something happened.

---

# ⚙️ Features

### Notification Detection

* Windows notification-listener integration
* Exact Signal Desktop AUMID matching
* Consent-based notification-listener access
* No message-content inspection
* No sender/message extraction

### Animated Pet

* Transparent desktop overlay
* Tuxedo cat artwork
* Configurable walking animation
* Configurable pause duration
* Configurable pet size
* Configurable screen edge

### Interaction

* Clickable cat
* Signal Desktop foreground activation
* Existing Signal window activation when possible
* Launch fallback when Signal is not running

### Settings

* Walk duration
* Pause duration
* Pet size
* Screen edge
* Optional startup on Windows sign-in

### Startup

Signal Pet can optionally start automatically when the user signs in.

Startup registration uses the current user's standard Windows startup mechanism and does not require administrator privileges.

### Local Configuration

Settings are stored locally at:

```text
%LOCALAPPDATA%\SignalPet\settings.json
```

No remote configuration service is required.

---

# 🏗️ Architecture

The application is divided into several independent responsibilities.

```text
                         ┌──────────────────────┐
                         │   Signal Desktop     │
                         └──────────┬───────────┘
                                    │
                                    │ Windows toast
                                    ▼
                    ┌───────────────────────────────┐
                    │ UserNotificationListener      │
                    └──────────────┬────────────────┘
                                   │
                                   ▼
                    ┌───────────────────────────────┐
                    │ SignalNotificationDetector    │
                    │                               │
                    │ Check AppUserModelId          │
                    │                               │
                    │ org.whispersystems.           │
                    │ signal-desktop                │
                    └──────────────┬────────────────┘
                                   │
                                   │ Event only
                                   ▼
                    ┌───────────────────────────────┐
                    │ PetAnimationController        │
                    └──────────────┬────────────────┘
                                   │
                                   ▼
                    ┌───────────────────────────────┐
                    │ PetOverlayWindow              │
                    │                               │
                    │       🐈 Tuxedo Cat           │
                    └──────────────┬────────────────┘
                                   │
                                   │ User click
                                   ▼
                    ┌───────────────────────────────┐
                    │ SignalDesktopActivator        │
                    └──────────────┬────────────────┘
                                   │
                                   ▼
                         ┌──────────────────────┐
                         │   Signal Desktop     │
                         └──────────────────────┘
```

---

# 🧩 Core Components

## `SignalNotificationDetector`

Responsible for:

* Requesting notification-listener access
* Subscribing to `NotificationChanged`
* Identifying newly added notifications
* Checking the Signal Desktop AUMID
* Managing notification IDs
* Raising the Signal notification event

It forms the primary privacy boundary of the application.

---

## `PetAnimationController`

Responsible for:

* Starting and stopping animations
* Walking behavior
* Pause timing
* Animation configuration
* Coordinating the overlay lifecycle

It has no need to know anything about Signal notifications.

---

## `PetOverlayWindow`

Responsible for:

* Transparent desktop rendering
* Overlay positioning
* Pet interaction
* Mouse hit testing
* Passing cat clicks to the Signal activator

The transparent overlay remains non-interactive outside the pet's clickable region.

---

## `IPetVisualFactory`

Provides an abstraction between the animation system and the actual pet artwork.

This allows different pets or visual implementations to be introduced without rewriting the notification detector.

---

## `TuxedoCatVisualFactory`

Provides the current cat visual implementation.

The artwork is stored under:

```text
src/SignalPet/Assets/Cat/
```

---

## `SignalDesktopActivator`

Responsible for returning the user to Signal Desktop after clicking the pet.

It attempts to:

1. Find an existing Signal process/window.
2. Restore it if necessary.
3. Bring it to the foreground.
4. Launch Signal if no existing instance can be activated.

It does not inspect Signal messages or conversations.

---

## `SettingsService`

Stores user preferences locally.

Current settings include:

* Walk duration
* Pause duration
* Pet size
* Screen edge

---

## `StartupRegistrationService`

Handles optional startup behavior using the current user's Windows startup configuration.

No administrator privileges are required.

---

# 📁 Project Structure

```text
signal-pet/
│
├── docs/
│   └── STAGE-1-RESEARCH.md
│
├── scripts/
│   └── Package-Development.ps1
│
├── src/
│   │
│   ├── SignalPet/
│   │   │
│   │   ├── Assets/
│   │   │   └── Cat/
│   │   │
│   │   ├── IPetVisualFactory.cs
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── PetAnimationController.cs
│   │   ├── PetOverlayWindow.xaml
│   │   ├── PetOverlayWindow.xaml.cs
│   │   ├── SettingsService.cs
│   │   ├── SignalDesktopActivator.cs
│   │   ├── SignalNotificationDetector.cs
│   │   ├── SignalPet.csproj
│   │   ├── StartupRegistrationService.cs
│   │   └── TuxedoCatVisualFactory.cs
│   │
│   └── SignalPet.Package/
│       └── Package.appxmanifest
│
├── .gitignore
└── README.md
```

---

# 🛠️ Technology Stack

| Technology                       | Purpose                                   |
| -------------------------------- | ----------------------------------------- |
| C#                               | Application logic                         |
| .NET 8                           | Runtime and framework                     |
| WPF                              | Desktop UI and overlay                    |
| Windows UserNotificationListener | Notification detection                    |
| Win32 interop                    | Window activation and overlay interaction |
| MSIX                             | Required packaged application identity    |
| Windows Registry                 | Optional user startup registration        |

---

# 📦 Why MSIX?

Windows notification-listener functionality requires packaged application identity and the appropriate manifest capability.

The project therefore uses an MSIX package containing:

```text
userNotificationListener
```

along with the required full-trust capability for the desktop application.

The package manifest is located at:

```text
src/SignalPet.Package/Package.appxmanifest
```

Development packaging is automated through:

```text
scripts/Package-Development.ps1
```

---

# 🚀 Development Setup

## Requirements

* Windows 10/11
* .NET 8 SDK
* Visual Studio or another C# development environment
* Windows SDK
* PowerShell
* A Signal Desktop installation
* Permission to grant Windows notification-listener access

Because the notification listener requires packaged identity, running only the unpackaged executable is not sufficient for the complete notification-detection flow.

---

# 🔨 Build

From the repository root:

```powershell
dotnet build -c Release
```

A successful build should report:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

---

# 📦 Development Packaging

The repository contains a development packaging script:

```powershell
.\scripts\Package-Development.ps1
```

The development package is intended for local testing.

The generated package should not be committed to the repository.

Generated artifacts such as:

```text
bin/
obj/
work/
outputs/
*.msix
*.appx
*.appxbundle
*.pri
```

are excluded through `.gitignore`.

---

# 🧪 Testing

The application currently includes a proof-of-concept window with a:

```text
Test pet animation
```

control.

This allows the visual/animation system to be tested independently of Signal notification delivery.

A typical development test flow is:

```text
1. Build
   ↓
2. Package
   ↓
3. Install MSIX
   ↓
4. Launch Signal Pet
   ↓
5. Grant notification-listener access
   ↓
6. Test pet animation
   ↓
7. Send a Signal message
   ↓
8. Confirm the cat appears
   ↓
9. Click the cat
   ↓
10. Confirm Signal Desktop is foregrounded
```

---

# 🔬 Development Stages

## Stage 1 — Research & Architecture

**Status: Complete**

Stage 1 established:

* Whether Windows exposes notification-listener functionality
* The need for packaged application identity
* The required manifest capability
* Signal Desktop identification
* The minimum notification metadata required
* The privacy boundary
* The limitations of Windows toast interception
* The architecture for the notification-to-pet pipeline

Detailed findings are documented in:

[`docs/STAGE-1-RESEARCH.md`](docs/STAGE-1-RESEARCH.md)

---

## Stage 2 — Independent Pet Overlay

**Status: Complete**

Stage 2 implemented the pet independently from Signal detection.

Implemented:

* Transparent overlay window
* Pet animation controller
* Configurable animation parameters
* Screen-edge positioning
* Replaceable visual factory
* Tuxedo cat visual implementation
* Test animation control

This separation allowed the animation system to be developed without depending on notification delivery.

---

## Stage 3 — Signal Integration

**Status: Complete**

Stage 3 connected the Windows notification listener to the pet.

Implemented:

* Notification-listener access flow
* `NotificationChanged` subscription
* Signal Desktop AUMID identification
* Notification ID handling
* Signal-specific event generation
* Notification-to-animation integration
* Privacy-preserving data flow

The animation layer receives only the Signal notification event.

---

## Stage 4 — User Interaction

**Status: Complete**

Stage 4 added interaction between the pet and Signal Desktop.

Implemented:

* Clickable cat
* Transparent click-through overlay behavior
* Signal Desktop foreground activation
* Existing Signal window restoration
* Launch fallback

The user can now move from:

```text
🐈
```

back to:

```text
Signal Desktop
```

without the pet needing access to the conversation itself.

---

# 🔒 Security & Privacy Considerations

Signal Pet intentionally avoids several approaches that would provide more information than the application needs.

It does not attempt to:

* Read Signal's local database
* Parse Signal message history
* Scrape Signal Desktop's UI
* Extract message text from notifications
* Identify the sender
* Identify the conversation
* Store Signal notification contents
* Upload notification information to a server

The application is designed so that notification content is unnecessary for its core functionality.

This is important because the application is intended to coexist with a private messaging application rather than become another component that processes private messages.

---

# ⚠️ Known Limitations

## 1. Windows toast interception

The biggest platform limitation is that Windows does not expose a supported pre-display interception mechanism for another application's toast notifications through `UserNotificationListener`.

Signal Pet therefore cannot guarantee that the original Windows banner was never rendered.

The application can react quickly and can manage notification state, but it should not be described as a system-level toast blocker.

---

## 2. Exact conversation opening

Clicking the cat currently opens/foregrounds Signal Desktop.

It does not automatically navigate to the exact conversation that generated the notification.

This is both a technical limitation of the current integration and a deliberate privacy decision.

The application does not need sender or conversation information to perform its core function.

---

## 3. Signal Desktop dependency

Signal Pet currently relies on Signal Desktop generating a Windows notification that can be observed by the Windows notification-listener API.

If Signal or Windows changes the notification mechanism, the detection layer may need to be updated.

---

## 4. Packaged application requirement

Notification-listener functionality requires the application to run with the appropriate packaged identity.

A normal unpackaged `.exe` build is therefore insufficient for the complete notification-detection functionality.

---

# 🧠 Why Not Read Signal Directly?

A tempting approach would be to inspect Signal Desktop's local files, database, UI, or internal application state.

Signal Pet deliberately does not do this.

The notification-listener architecture provides a much smaller and easier-to-understand privacy boundary:

```text
                 Signal Desktop
                       │
                       │ Windows notification
                       ▼
             ┌─────────────────────┐
             │ Windows notification│
             │      listener       │
             └──────────┬──────────┘
                        │
                 minimal metadata
                        │
                        ▼
                 Signal Pet
                        │
                        │ event only
                        ▼
                     🐈
```

The pet does not need to understand the message.

It only needs to know that there is a notification worth reacting to.

---

# 🎨 Extensibility

The current implementation uses a tuxedo cat, but the architecture is intentionally not cat-specific.

The visual layer is abstracted through:

```text
IPetVisualFactory
```

Future visual implementations could therefore introduce:

* Different cat breeds
* Dogs
* Pixel-art characters
* Animated creatures
* Seasonal characters
* Custom user artwork

without fundamentally changing the notification detector.

The goal is to keep:

```text
notification logic
```

separate from:

```text
visual identity
```

---

# 🗺️ Future Ideas

Potential future improvements include:

* More pet animation states
* Additional pet characters
* Custom animation packs
* Improved overlay positioning
* Better multi-monitor behavior
* More detailed pet configuration
* Optional idle animations
* Additional notification sources
* Better application lifecycle management
* More robust Signal Desktop activation
* Optional conversation navigation if a safe, privacy-preserving mechanism becomes available

Any future notification integration should preserve the project's core privacy boundary.

---

# 📊 Current Project Status

| Component                               | Status                   |
| --------------------------------------- | ------------------------ |
| Windows notification detection          | ✅ Complete               |
| Signal Desktop identification           | ✅ Complete               |
| Privacy-constrained notification access | ✅ Complete               |
| Independent pet animation               | ✅ Complete               |
| Transparent overlay                     | ✅ Complete               |
| Tuxedo cat visual                       | ✅ Complete               |
| Signal → pet integration                | ✅ Complete               |
| Click-to-Signal interaction             | ✅ Complete               |
| Local settings                          | ✅ Complete               |
| Optional startup                        | ✅ Complete               |
| MSIX packaging                          | ✅ Complete               |
| Development packaging script            | ✅ Complete               |
| Pre-display toast interception          | ❌ Windows API limitation |
| Exact conversation deep-linking         | ⏳ Not implemented        |

---

# 📌 Current User Experience

The intended final interaction is deliberately simple:

```text
                  Someone messages you
                          │
                          ▼
                  Signal Desktop
                          │
                          ▼
                Windows notification
                          │
                          ▼
                    Signal Pet
                          │
                          ▼
                    🐈 appears
                          │
                          │ click
                          ▼
                  Signal Desktop
```

The user does not need to read a notification preview.

They see a small animated companion instead.

---

# 📜 Project Philosophy

Signal Pet is based on a simple idea:

> **A notification should not necessarily reveal the information it is notifying you about.**

The application therefore treats the existence of a notification and the contents of a notification as two separate concepts.

Signal Pet needs the first.

It deliberately avoids the second.

---

# 📄 Documentation

Additional technical research and architectural decisions are available in:

* [`docs/STAGE-1-RESEARCH.md`](docs/STAGE-1-RESEARCH.md)

This document contains the research behind the Windows notification-listener approach and the platform limitations that shaped the implementation.

---

# 🤝 Contributing

Contributions are welcome, particularly around:

* Animation improvements
* Accessibility
* Multi-monitor support
* Visual/pet implementations
* Windows packaging
* Performance improvements
* Privacy-preserving notification integrations

When contributing notification-related functionality, please preserve the existing privacy boundary.

Avoid introducing code that reads, stores, transmits, or exposes notification content unless there is a clear architectural reason and explicit user-facing justification.

---

# 📜 License

See the repository license for the applicable terms.

---

<div align="center">

### 🐈 Signal Pet

**Notifications, without the notification overload.**

No message preview.
No sender information.
No sound.

**Just a cat.**

</div>
