namespace Lumix.Views.Preferences.Plugins;

public static class PluginsPreferences
{
    public static string VST2PluginsPath { get; set; } = string.Empty;
    public static bool MultiplePluginWindows { get; set; } = true;
    public static bool AutoHidePluginWindows { get; set; } = false;
    public static bool AutoOpenPluginWindows { get; set; } = true;
}
