using Microsoft.Win32;

namespace SignalPet;

public sealed class StartupRegistrationService
{
    private const string ValueName = "SignalPet";
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

    public bool IsEnabled() => Registry.CurrentUser.OpenSubKey(RunKeyPath)?.GetValue(ValueName) is string;

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
            key.SetValue(ValueName, $"\"{Environment.ProcessPath}\" --background");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
