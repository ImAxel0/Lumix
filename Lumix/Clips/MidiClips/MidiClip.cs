using ImGuiNET;
using Lumix.Clips.Renderers;
using Lumix.Tracks.MidiTracks;
using Lumix.Views;
using Lumix.Views.Arrangement;
using Lumix.Views.Midi;
using Melanchall.DryWetMidi.Interaction;
using System.Numerics;
using MidiFile = Melanchall.DryWetMidi.Core.MidiFile;

namespace Lumix.Clips.MidiClips;

public class MidiClip : Clip
{
    private MidiClipData _midiClipData;
    public MidiClipData MidiClipData => _midiClipData;

    private PianoRoll _pianoRollEditor;
    public PianoRoll PianoRollEditor => _pianoRollEditor;

    public MidiClip(MidiTrack parent, string filePath, long startingTime = 0)
    {
        Track = parent;
        Name = Path.GetFileNameWithoutExtension(filePath);
        Color = parent.Color;
        var midiFile = MidiFile.Read(filePath);
        _midiClipData = new MidiClipData(midiFile);
        _pianoRollEditor = new PianoRoll(this, Track as MidiTrack);
        StartTick = startingTime;
    }

    public MidiClip(MidiTrack parent, MidiClipData midiData, long startingTime = 0)
    {
        Track = parent;
        Color = parent.Color;
        _midiClipData = midiData;
        _pianoRollEditor = new PianoRoll(this, Track as MidiTrack);
        StartTick = startingTime;
    }

    public MidiClip(MidiTrack parent, long startingTime = 0)
    {
        Track = parent;
        Color = parent.Color;
        _midiClipData = new MidiClipData();
        _pianoRollEditor = new PianoRoll(this, Track as MidiTrack);
        StartTick = startingTime;
    }

    public void UpdateClipData(MidiClipData newdata)
    {
        _midiClipData = newdata;
    }

    protected override long GetClipDuration()
    {
        return TimeLineV2.SecondsToTicks(_midiClipData.MidiFile.GetDuration<MetricTimeSpan>().TotalSeconds);
        //return (float)_midiClipData.MidiFile.GetDuration<MetricTimeSpan>().TotalSeconds;
    }

    protected override float GetClipWidth()
    {
        return TimeLineV2.SecondsToTicks(_midiClipData.MidiFile.GetDuration<MetricTimeSpan>().TotalSeconds, false) * TimeLineV2.PixelsPerTick;
        return (float)(_midiClipData.MidiFile.GetDuration<MetricTimeSpan>().TotalSeconds * 120f /** TopBarControls.Bpm*/ * ArrangementView.Zoom);
    }

    protected override void RenderClipContent(float menuBarHeight, float clipHeight)
    {
        MidiRenderer.RenderMidiData(_midiClipData, ImGui.GetWindowPos() + new Vector2(0, menuBarHeight), ClipWidth, clipHeight, Track.Enabled);
    }

    protected override void RenderClipContent(Vector2 pos, float width, float height)
    {
        throw new NotImplementedException();
    }

    protected override void OnClipDoubleClickLeft()
    {
        // open clip edit view
        ArrangementView.SelectedClips.Clear();
        MidiClipView.SelectedMidiClip = this;
        _pianoRollEditor._notes = _midiClipData.Notes.ToList();
        BottomView.RenderedWindow = BottomViewWindows.MidiClipView;
    }
}
