namespace Lumix.Plugins;

/// <summary>
/// Interface to manage built in and external plugins as one type
/// </summary>
public interface IAudioProcessor
{
    /// <summary>
    /// The plugin state.
    /// <para><see langword="True"/> if the plugin is on.</para>
    /// <para><see langword="False"/> false if plugin is off.</para>
    /// </summary>
    bool Enabled { get; set; }

    bool DeleteRequested { get; set; }
    bool DuplicateRequested { get; set; }

    /// <summary>
    /// Processes audio as defined in its implementation.
    /// </summary>
    /// <param name="input">The incoming unprocessed audio buffer</param>
    /// <param name="output">The processed audio buffer</param>
    /// <param name="samplesRead"></param>
    void Process(float[] input, float[] output, int samplesRead);

    /// <summary>
    /// Generic method to retrieve the underlying plugin (if applicable)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T? GetPlugin<T>() where T : class;

    /// <summary>
    /// Toggles the plugin state.
    /// </summary>
    public void Toggle()
    {
        Enabled = !Enabled;
    }
}

