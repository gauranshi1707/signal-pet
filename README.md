# Signal Pet

Signal Pet is a Windows desktop companion that reacts to an incoming **Signal Desktop Windows notification** with a quiet animated pet. It never displays a sender, message content, notification text, or any other Signal data, and it makes no sound.

## Current stage

Stage 1 is complete: a privacy-constrained notification-detection proof of concept and the architecture decision are in the repository. See [the Stage 1 research findings](docs/STAGE-1-RESEARCH.md) before continuing to the overlay implementation.

Stage 2 is complete: the independent transparent pet overlay can be exercised with **Test pet animation** in the proof-of-concept window. Its artwork is a text-free vector placeholder and can be replaced through `IPetVisualFactory` without changing the animation or notification code.

Stage 3 connects a detected Signal toast to this animation. The integration carries no notification object or payload into the animation path—only the event that a matching Signal toast was added.

## Planned stack

- C# / .NET 8 WPF for the compact desktop UI and future transparent overlay.
- MSIX packaging, required for Windows' notification-listener capability.
- `UserNotificationListener` for consent-gated Windows toast lifecycle events.

## Important limitation

Signal Pet cannot programmatically intercept a Signal banner before Windows displays it. The supported fallback is to disable Signal's **notification banners** in Windows while leaving its notifications enabled, then let Signal Pet respond to the recorded toast. See [Stage 1 research](docs/STAGE-1-RESEARCH.md) for the details and limitations.
