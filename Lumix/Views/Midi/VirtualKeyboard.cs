using ImGuiNET;
using Lumix.Tracks.MidiTracks;
using Lumix.Views.Arrangement;
using Melanchall.DryWetMidi.Common;
using Veldrid;

namespace Lumix.Views.Midi;

public static class VirtualKeyboard
{
    private static bool _enabled = true;
    public static bool Enabled => _enabled;

    private static int _octaveShift = 0;
    private static int _velocity = 100;
    private static bool _isKeyDown;
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

    private static readonly Dictionary<Key, int> _veldridKeyNoteMap = new()
    {
        { Key.A, 60 }, // C4
        { Key.W, 61 }, // C#4
        { Key.S, 62 }, // D4
        { Key.E, 63 }, // D#4
        { Key.D, 64 }, // E4
        { Key.F, 65 }, // F4
        { Key.T, 66 }, // F#4
        { Key.G, 67 }, // G4
        { Key.Y, 68 }, // G#4
        { Key.H, 69 }, // A4
        { Key.U, 70 }, // A#4
        { Key.J, 71 }, // B4
        { Key.K, 72 }, // C5
    };

    public static void Toggle()
    {
        _enabled = !_enabled;
    }

    private static void ShiftOctave(int amount)
    {
        _octaveShift += amount;
        _octaveShift = Math.Clamp(_octaveShift, -36, 36);
        InfoBox.SetInfoData("Octave adjusted", $"Octave shift: {_octaveShift}", true);
    }

    private static void ShiftVelocity(int amount)
    {
        _velocity += amount;
        _velocity = Math.Clamp(_velocity, 7, 127);
        InfoBox.SetInfoData("Velocity adjusted", $"Velocity: {_velocity}", true);
    }

    public static void KeyDownFromPlugin(KeyEvent ev)
    {
        if (ev.Repeat)
            return;

        if (ev.Key == Key.Space)
        {
            if (TimeLine.IsPlaying())
                TimeLine.StopPlayback();
            else
                TimeLine.StartPlayback();
        }

        if (!_enabled)
            return;

        if (_veldridKeyNoteMap.ContainsKey(ev.Key))
        {
            ArrangementView.Tracks.ForEach(track =>
            {
                if (track.RecordOnStart && track.Engine is TrackMidiEngine midiEngine)
                {
                    midiEngine.SendNoteOnEvent(0,
                        new SevenBitNumber((byte)(_veldridKeyNoteMap[ev.Key] + _octaveShift)),
                        new SevenBitNumber((byte)_velocity));
                }
            });
        }

        if (ev.Key == Key.Z)
        {
            ShiftOctave(-12);
        }

        if (ev.Key == Key.X)
        {
            ShiftOctave(+12);
        }

        if (ev.Key == Key.C)
        {
            ShiftVelocity(-10);
        }

        if (ev.Key == Key.V)
        {
            ShiftVelocity(+10);
        }
    }

    public static void KeyUpFromPlugin(KeyEvent ev)
    {
        if (!_enabled)
            return;

        if (_veldridKeyNoteMap.ContainsKey(ev.Key))
        {
            ArrangementView.Tracks.ForEach(track =>
            {
                if (track.RecordOnStart && track.Engine is TrackMidiEngine midiEngine)
                {
                    midiEngine.SendNoteOffEvent(0,
                        new SevenBitNumber((byte)(_veldridKeyNoteMap[ev.Key] + _octaveShift)),
                        new SevenBitNumber(0));
                }
            });
        }
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
                        midiEngine.SendNoteOnEvent(0,
                            new SevenBitNumber((byte)(_keyNoteMap[key] + _octaveShift)),
                            new SevenBitNumber((byte)_velocity));
                    }
                });
                _isKeyDown = true;
            }

            if (ImGui.IsKeyReleased(key))
            {
                ArrangementView.Tracks.ForEach(track =>
                {
                    if (track.RecordOnStart && track.Engine is TrackMidiEngine midiEngine)
                    {
                        midiEngine.SendNoteOffEvent(0,
                            new SevenBitNumber((byte)(_keyNoteMap[key] + _octaveShift)),
                            new SevenBitNumber(0));
                    }
                });
                _isKeyDown = false;
            }
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Z, false) && !_isKeyDown)
        {
            ShiftOctave(-12);
        }

        if (ImGui.IsKeyPressed(ImGuiKey.X, false) && !_isKeyDown)
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
