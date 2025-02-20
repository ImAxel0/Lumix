using Lumix.ImGuiExtensions;
using Lumix.Views.Arrangement;
using Lumix.Views.Preferences.Audio;
using NAudio.Wave;
using System.Numerics;

namespace Lumix.Tracks.AudioTracks;

public class AudioTrack : Track
{
    public override TrackType TrackType => TrackType.Audio;

    public AudioTrack(string name)
    {
        Name = name;
        Vector4 trackCol = ImGuiTheme.DefaultColors[new Random().Next(0, ImGuiTheme.DefaultColors.Length)];
        Color = trackCol;
        Engine = new TrackAudioEngine(this, AudioSettings.SampleRate);
        Engine.VolumeMeasured += (sender, e) =>
        {
            // Get the maximum peak across all channels
            _leftChannelGain = e.MaxSampleValues[0];
            _rightChannelGain = e.MaxSampleValues[1];
            //CurrentVolume = e.MaxSampleValues.Max();
        };
        ArrangementView.MasterTrack.AudioEngine.AddTrack(Engine);
    }

    private AudioFileReader _draggedClip = null; // Track the currently dragged clip
    public AudioFileReader DraggedClip => _draggedClip;

    public void SetDraggedClip(AudioFileReader draggedClip)
    {
        _draggedClip = draggedClip;
    }

    protected override void OnDoubleClickLeft()
    {

    }
}
