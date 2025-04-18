namespace Lumix.Plugins;

/// <summary>
/// The type of the plugin.
/// </summary>
public enum PluginType
{
    /// <summary>
    /// Plugin is an effect. (e.g VST)
    /// </summary>
    Effect,

    /// <summary>
    /// Plugin is an instrument. (e.g VSTi)
    /// </summary>
    Instrument
}
