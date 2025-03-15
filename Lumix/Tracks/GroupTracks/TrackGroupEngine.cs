using Lumix.Plugins;
using Lumix.SampleProviders;
using Melanchall.DryWetMidi.Core;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Lumix.Tracks.GroupTracks;

public class TrackGroupEngine : TrackEngine
{
    public TrackGroupEngine(GroupTrack groupTrack, int sampleRate = 44100, int channelCount = 2)
    {
        Mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };

        // Initialize Vst's chain to apply vst's effects (returns audio processed by all track vst's)
        PluginChainSampleProvider = new PluginChainSampleProvider(Mixer);

        // Initialize custom StereoSampleProvider for volume and pan control
        StereoSampleProvider = new StereoSampleProvider(PluginChainSampleProvider);

        // Initialize MeteringSampleProvider for gain feedback
        MeteringSampleProvider = new MeteringSampleProvider(StereoSampleProvider, 100);
        MeteringSampleProvider.StreamVolume += (s, e) => VolumeMeasured?.Invoke(this, e);

        // Fills the buffer with zeroes if track is disabled
        TrackStateSampleProvider = new TrackStateSampleProvider(MeteringSampleProvider, groupTrack);
    }

    public override event EventHandler<StreamVolumeEventArgs> VolumeMeasured;

    public override void Fire(AudioFileReader audioFile, float offset, float endOffset)
    {

    }

    public override void Fire(MidiFile midiFile, float offset, float endOffset)
    {

    }

    public override void StartRecording()
    {

    }

    public override void StopRecording(Track destTrack)
    {

    }

    public override void StopSounds()
    {

    }
}