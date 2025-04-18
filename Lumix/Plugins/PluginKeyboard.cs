using ImGuiNET;
using Lumix.Views.Arrangement;
using Lumix.Views.Midi;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Veldrid;

namespace Lumix.Plugins;

public class PluginKeyboard
{
    private IPlugin _plugin;

    private readonly Dictionary<Key, int> _veldridKeyNoteMap = new()
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

    public PluginKeyboard(IPlugin plugin)
    {
        _plugin = plugin;
    }

    public void KeyDownFromPlugin(KeyEvent ev)
    {
        if (ev.Repeat || !VirtualKeyboard.Enabled)
            return;

        if (_veldridKeyNoteMap.ContainsKey(ev.Key))
        {
            _plugin?.ReceiveMidiEvent(
                new NoteOnEvent((SevenBitNumber)(_veldridKeyNoteMap[ev.Key] + VirtualKeyboard.OctaveShift),
                (SevenBitNumber)VirtualKeyboard.Velocity));
        }

        if (ev.Key == Key.Space)
        {
            if (TimeLine.IsPlaying())
                TimeLine.StopPlayback();
            else
                TimeLine.StartPlayback();
        }

        if (ev.Key == Key.Z)
        {
            VirtualKeyboard.ShiftOctave(-12);
        }

        if (ev.Key == Key.X)
        {
            VirtualKeyboard.ShiftOctave(+12);
        }

        if (ev.Key == Key.C)
        {
            VirtualKeyboard.ShiftVelocity(-10);
        }

        if (ev.Key == Key.V)
        {
            VirtualKeyboard.ShiftVelocity(+10);
        }
    }

    public void KeyUpFromPlugin(KeyEvent ev)
    {
        if (ev.Repeat || !VirtualKeyboard.Enabled)
            return;

        if (_veldridKeyNoteMap.ContainsKey(ev.Key))
        {
            _plugin?.ReceiveMidiEvent(
                new NoteOffEvent((SevenBitNumber)(_veldridKeyNoteMap[ev.Key] + VirtualKeyboard.OctaveShift),
                SevenBitNumber.MinValue));
        }
    }
}
