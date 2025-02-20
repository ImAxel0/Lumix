using Lumix.Plugins;
using Lumix.SampleProviders;
using Melanchall.DryWetMidi.Core;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Lumix.Tracks;

public abstract class TrackEngine : IDisposable
{
    public MixingSampleProvider Mixer { get; protected set; }
    public PluginChainSampleProvider PluginChainSampleProvider { get; protected set; }
    public StereoSampleProvider StereoSampleProvider { get; protected set; }
    public TrackStateSampleProvider TrackStateSampleProvider { get; protected set; }
    public MeteringSampleProvider MeteringSampleProvider { get; protected set; }

    public abstract event EventHandler<StreamVolumeEventArgs> VolumeMeasured;
    public bool IsRecording { get; protected set; }

    /// <summary>
    /// Returns the Track final sample provider
    /// </summary>
    public ISampleProvider GetTrackAudio()
    {
        return TrackStateSampleProvider;
    }

    /// <summary>
    /// Fired when audio clip start playing
    /// </summary>
    public abstract void Fire(AudioFileReader audioFile, float offset, float endOffset);

    /// <summary>
    /// Fired when midi clip start playing
    /// </summary>
    public abstract void Fire(MidiFile midiFile, float offset, float endOffset);

    /// <summary>
    /// Called when track starts recording data
    /// </summary>
    public abstract void StartRecording();

    /// <summary>
    /// Called when track stopped recording data
    /// </summary>
    /// <param name="destTrack">The destination track in which the clip will be created</param>
    public abstract void StopRecording(Track destTrack);

    /// <summary>
    /// Called when timeline is stopped
    /// </summary>
    public abstract void StopSounds();

    public void Dispose()
    {

    }
}
