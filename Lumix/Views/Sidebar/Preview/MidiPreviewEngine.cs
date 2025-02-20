using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Lumix.Views.Sidebar.Preview;

/// <summary>
/// Only used for midi files preview playback
/// </summary>
public static class MidiPreviewEngine
{
    public static IOutputDevice OutDevice { get; private set; } = OutputDevice.GetByIndex(0);
    public static Playback Playback { get; private set; }

    public static void PlayMidiPreview(string filePath)
    {
        StopPreview();
        Playback = MidiFile.Read(filePath).GetPlayback(OutDevice);
        Playback.Start();
    }

    public static void StopPreview()
    {
        Playback?.Stop();
        Playback?.MoveToStart();
    }
}
