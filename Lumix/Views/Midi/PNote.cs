using Melanchall.DryWetMidi.Interaction;

namespace Lumix.Views.Midi;

/// <summary>
/// Piano roll note
/// </summary>
public class PNote
{
    public PNote(Note data)
    {
        Data = data;
    }

    /// <summary>
    /// Note data like time, length etc.
    /// </summary>
    public Note Data { get; set; }

    /// <summary>
    /// Note state
    /// </summary>
    public bool Enabled { get; set; } = true;
}
