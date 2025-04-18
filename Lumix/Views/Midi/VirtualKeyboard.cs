using ImGuiNET;
using Lumix.Tracks.MidiTracks;
using Lumix.Views.Arrangement;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;

namespace Lumix.Views.Midi;

public static class VirtualKeyboard
{
    public static bool Enabled { get; set; } = true;
    public static int Velocity { get; set; } = 100;
    public static int OctaveShift { get; set; } = 0;

    private static readonly Dictionary<ImGuiKey, int> _keyNoteMap = new()
    {
        { ImGuiKey.A, 60 }, // C4
        { ImGuiKey.W, 61 }, // C#4
        { ImGuiKey.S, 62 }, // D4
        { ImGuiKey.E, 63 }, // D#4
        { ImGuiKey.D, 64 }, // E4
        { ImGuiKey.F, 65 }, // F4
        { ImGuiKey.T, 66 }, // F#4
        { ImGuiKey.G, 67 }, // G4
        { ImGuiKey.Y, 68 }, // G#4
        { ImGuiKey.H, 69 }, // A4
        { ImGuiKey.U, 70 }, // A#4
        { ImGuiKey.J, 71 }, // B4
        { ImGuiKey.K, 72 }, // C5
    };

    public static void Toggle()
    {
        Enabled = !Enabled;
        ArrangementView.Tracks.ForEach(track =>
        {
            if (track.Engine is TrackMidiEngine midiEngine)
            {
                midiEngine.PluginChainSampleProvider.PluginInstrument?.ReceiveMidiEvent(
                    new ControlChangeEvent(ControlUtilities.AsSevenBitNumber(ControlName.AllNotesOff), SevenBitNumber.MinValue));
            }
        });
    }

    public static void ShiftOctave(int amount)
    {
        ArrangementView.Tracks.ForEach(track =>
        {
            if (track.Engine is TrackMidiEngine midiEngine)
            {
                midiEngine.PluginChainSampleProvider.PluginInstrument?.ReceiveMidiEvent(
                    new ControlChangeEvent(ControlUtilities.AsSevenBitNumber(ControlName.AllNotesOff), SevenBitNumber.MinValue));
            }
        });

        OctaveShift += amount;
        OctaveShift = Math.Clamp(OctaveShift, -36, 36);
        InfoBox.SetInfoData("Octave adjusted", $"Octave shift: {OctaveShift}", true);
    }

    public static void ShiftVelocity(int amount)
    {
        Velocity += amount;
        Velocity = Math.Clamp(Velocity, 7, 127);
        InfoBox.SetInfoData("Velocity adjusted", $"Velocity: {Velocity}", true);
    }

    public static void ListenForKeyPresses()
    {
        foreach (var key in _keyNoteMap.Keys)
        {
            if (ImGui.IsKeyPressed(key, false))
            {
                ArrangementView.Tracks.ForEach(track =>
                {
                    if (track.RecordOnStart && track.Engine is TrackMidiEngine midiEngine)
                    {
                        midiEngine.PluginChainSampleProvider.PluginInstrument?.ReceiveMidiEvent(
                            new NoteOnEvent((SevenBitNumber)(_keyNoteMap[key] + OctaveShift), (SevenBitNumber)Velocity));
                    }
                });
            }

            if (ImGui.IsKeyReleased(key))
            {
                ArrangementView.Tracks.ForEach(track =>
                {
                    if (track.RecordOnStart && track.Engine is TrackMidiEngine midiEngine)
                    {
                        midiEngine.PluginChainSampleProvider.PluginInstrument?.ReceiveMidiEvent(
                            new NoteOffEvent((SevenBitNumber)(_keyNoteMap[key] + OctaveShift), SevenBitNumber.MinValue));
                    }
                });
            }
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Z, false))
        {
            ShiftOctave(-12);
        }

        if (ImGui.IsKeyPressed(ImGuiKey.X, false))
        {
            ShiftOctave(+12);
        }

        if (ImGui.IsKeyPressed(ImGuiKey.C, false))
        {
            ShiftVelocity(-10);
        }

        if (ImGui.IsKeyPressed(ImGuiKey.V, false))
        {
            ShiftVelocity(+10);
        }
    }
}
