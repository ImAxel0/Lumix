using Lumix.ImGuiExtensions;
using Lumix.Tracks.AudioTracks;
using Lumix.Views.Arrangement;
using Lumix.Views.Preferences.Audio;
using System.Numerics;
using Vanara.PInvoke;

namespace Lumix.Tracks.GroupTracks;

public class GroupTrack : Track
{
    public override TrackType TrackType => TrackType.Group;

    /// <summary>
    /// Tracks of this group
    /// </summary>
    public List<Track> Tracks { get; protected set; } = new();

    public GroupTrack(string name)
    {
        Name = name;
        Vector4 trackCol = ImGuiTheme.GetRandomColor();
        Color = trackCol;
        Engine = new TrackGroupEngine(this, AudioSettings.SampleRate);
        Engine.VolumeMeasured += (sender, e) =>
        {
            // Get the maximum peak across all channels
            _leftChannelGain = e.MaxSampleValues[0];
            _rightChannelGain = e.MaxSampleValues[1];
            //CurrentVolume = e.MaxSampleValues.Max();
        };
        ArrangementView.MasterTrack.AudioEngine.AddTrack(Engine); // Add the group track to the master mixer
    }

    public bool AddTrackToGroup(Track track)
    {
        if (!Tracks.Any()) // if group is empty add the track
        {
            ArrangementView.MasterTrack.AudioEngine.RemoveTrack(track); // remove track from master since it will point to group instead
            Engine.Mixer.AddMixerInput(track.Engine.GetTrackAudio()); // make track point group mixer
            Tracks.Add(track);
            return true;
        }

        if (Tracks[0].TrackType != track.TrackType) // Check if first track in group is same type as parameter
        {
            User32.MessageBox(IntPtr.Zero, "Can't add track of this type to group", "Warning", User32.MB_FLAGS.MB_ICONWARNING | User32.MB_FLAGS.MB_TOPMOST);
            return false;
        }
        else
        {
            ArrangementView.MasterTrack.AudioEngine.RemoveTrack(track); // remove track from master since it will point to group instead
            Engine.Mixer.AddMixerInput(track.Engine.GetTrackAudio()); // make track point group mixer
            Tracks.Add(track);
        }
        return true;
    }

    protected override void OnDoubleClickLeft()
    {

    }
}
