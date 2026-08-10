using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SignalPet;

/// <summary>Activates Signal's existing desktop window, or starts its installed executable.</summary>
public sealed class SignalDesktopActivator
{
    private const int SwRestore = 9;

    public void ActivateOrLaunch()
    {
        if (TryActivateRunningWindow())
        {
            return;
        }

        foreach (var executablePath in GetInstalledExecutablePaths())
        {
            if (!File.Exists(executablePath))
            {
                continue;
            }

            TryStart(executablePath);
            return;
        }

        TryStart("Signal");
    }

    private static bool TryActivateRunningWindow()
    {
        foreach (var process in Process.GetProcessesByName("Signal"))
        {
            using (process)
            {
                try
                {
                    if (process.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    ShowWindowAsync(process.MainWindowHandle, SwRestore);
                    SetForegroundWindow(process.MainWindowHandle);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    // Signal exited while its process list was being inspected.
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> GetInstalledExecutablePaths()
    {
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "signal-desktop", "Signal.exe");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Signal", "Signal.exe");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Signal", "Signal.exe");
    }

    private static void TryStart(string fileName)
    {
        try
        {
            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Signal is not installed at this candidate path.
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr windowHandle, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);
}
