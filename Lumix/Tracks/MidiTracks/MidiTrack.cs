using ImGuiNET;
using Lumix.Clips.MidiClips;
using Lumix.ImGuiExtensions;
using Lumix.Views.Arrangement;
using Lumix.Views.Preferences.Audio;
using System.Numerics;

namespace Lumix.Tracks.MidiTracks;

public class MidiTrack : Track
{
    public override TrackType TrackType => TrackType.Midi;

    public MidiTrack(string name = "")
    {
        Name = name;
        Vector4 trackCol = ImGuiTheme.GetRandomColor();
        Color = trackCol;
        Engine = new TrackMidiEngine(this, AudioSettings.SampleRate);
        Engine.VolumeMeasured += (sender, e) =>
        {
            // Get the maximum peak across all channels
            _leftChannelGain = e.MaxSampleValues[0];
            _rightChannelGain = e.MaxSampleValues[1];
        };
        ArrangementView.MasterTrack.AudioEngine.AddTrack(Engine);
    }

    private MidiClipData _draggedClip = null; // Track the currently dragged clip
    public MidiClipData DraggedClip => _draggedClip;

    public void CreateMidiClip(long time)
    {
        var clip = new MidiClip(this, time);
        Clips.Add(clip);
    }

    public void CreateMidiClip(TimeSelection time)
    {
        var clip = new MidiClip(this, time);
        Clips.Add(clip);
    }

    protected override void OnDoubleClickLeft()
    {
        // Create midi clip only if no track clip is hovered
        if (!Clips.Any(clip => clip.ClipIsHovered))
        {
            // Get the mouse position within the window
            float mousePosX = ImGui.GetMousePos().X - ArrangementView.WindowPos.X;

            // Adjust the mouse position based on the scroll offset
            float adjustedMousePosX = mousePosX + ArrangementView.ArrangementScroolX;

            var newTime = TimeLineV2.SnapToGrid(TimeLineV2.PositionToTime(adjustedMousePosX));

            // Convert the mouse position in pixels to time, considering the zoom factor
            //float newTime = adjustedMousePosX / ArrangementView.Zoom;
            //float stepLength = /*TopBarControls.Bpm **/ 120 * ArrangementView.BeatsPerBar * 2;
            //float snappedPosition = MathF.Round(newTime / stepLength) * stepLength;
            CreateMidiClip(newTime);
        }
    }
}
