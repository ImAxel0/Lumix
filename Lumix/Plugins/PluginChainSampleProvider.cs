using Lumix.Plugins.BuiltIn.Eq;
using Lumix.Plugins.BuiltIn.Utilities;
using Lumix.Plugins.VST;
using NAudio.Wave;

namespace Lumix.Plugins;

public class PluginChainSampleProvider : ISampleProvider
{
    private IPlugin _pluginInstrument;
    /// <summary>
    /// The Instrument of this chain if it's a midi track
    /// </summary>
    public IPlugin PluginInstrument => _pluginInstrument;

    private List<IPlugin> _fxPlugins = new() { new UtilityPlugin(), new SimpleEqPlugin() };
    /// <summary>
    /// Effects plugins chain
    /// </summary>
    public List<IPlugin> FxPlugins => _fxPlugins;

    private readonly ISampleProvider source;
    public WaveFormat WaveFormat => source.WaveFormat;

    public PluginChainSampleProvider(ISampleProvider source)
    {
        this.source = source;
    }

    public void AddPlugin(IPlugin plugin)
    {
        if (plugin is VstPlugin vstPlugin && vstPlugin.PluginType == PluginType.Instrument)
        {
            // Dispose of the current instrument if it exists
            if (_pluginInstrument != null && _pluginInstrument is VstPlugin currentVstInstrument)
            {
                currentVstInstrument.DisposeVST(vstPlugin.PluginWindow.Handle != currentVstInstrument.PluginWindow.Handle);
            }

            _pluginInstrument = plugin;
        }
        else if (plugin.PluginType == PluginType.Instrument)
        {
            _pluginInstrument?.Dispose();
            _pluginInstrument = plugin;
        }
        else
        {
            _fxPlugins.Add(plugin);
        }
    }

    public void RemovePlugin(IPlugin target)
    {
        target.Dispose();
        if (target == _pluginInstrument)
            _pluginInstrument = null;
        else
            _fxPlugins.Remove(target);
    }

    public void RemoveAllPlugins()
    {
        // Dispose and remove instrument
        if (_pluginInstrument is VstAudioProcessor vstInstrument)
        {
            vstInstrument.DeleteRequested = true;
            vstInstrument.VstPlugin.Dispose();
        }
        _pluginInstrument = null;

        // Dispose and remove all effect plugins
        foreach (var fxPlugin in _fxPlugins.ToList())
        {
            _fxPlugins.Remove(fxPlugin);
            if (fxPlugin is VstAudioProcessor vstFxPlugin)
            {
                vstFxPlugin.DeleteRequested = true;
                vstFxPlugin.VstPlugin.Dispose();
            }
        }
    }

    private void ProcessAudio(IPlugin plugin, ref float[] buffer, int offset, int count, int samplesRead)
    {
        // Create a temporary buffer to hold the processed data
        float[] tempBuffer = new float[count];

        // Copy the current buffer data to the temporary buffer
        Array.Copy(buffer, offset, tempBuffer, 0, samplesRead);

        // Process the data through the plugin
        plugin.Process(tempBuffer, tempBuffer, samplesRead);

        // Copy the processed data back to the original buffer
        Array.Copy(tempBuffer, 0, buffer, offset, samplesRead);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = source.Read(buffer, offset, count);

        if (_pluginInstrument != null)
        {
            // Process VSTi sound only if plugin is enabled
            if (_pluginInstrument.Enabled)
            {
                ProcessAudio(_pluginInstrument, ref buffer, offset, count, samplesRead);
            }
        }

        // Apply vst's audio processing in the list order
        foreach (var plugin in _fxPlugins.ToList())
        {
            // Skip plugin audio processing if not enabled
            if (!plugin.Enabled)
                continue;

            ProcessAudio(plugin, ref buffer, offset, count, samplesRead);
        }

        return samplesRead;
    }
}
