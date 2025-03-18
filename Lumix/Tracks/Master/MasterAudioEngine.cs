using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using Lumix.Tracks.AudioTracks;
using Lumix.Tracks.MidiTracks;
using Lumix.SampleProviders;
using Lumix.Views.Preferences.Audio;

namespace Lumix.Tracks.Master;

public class MasterAudioEngine : IDisposable
{
    //private readonly IWavePlayer outputDevice;
    private readonly MixingSampleProvider masterMixer;
    public MixingSampleProvider MasterMixer => masterMixer;
    private readonly MeteringSampleProvider meteringProvider;
    private readonly StereoSampleProvider stereoSampleProvider;
    public StereoSampleProvider StereoSampleProvider => stereoSampleProvider;

    public event EventHandler<StreamVolumeEventArgs> VolumeMeasured;

    public MasterAudioEngine(int sampleRate = 44100, int channelCount = 2)
    {
        CoreAudioEngine.Init();

        masterMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };

        stereoSampleProvider = new StereoSampleProvider(masterMixer);

        // Add metering for the master track
        meteringProvider = new MeteringSampleProvider(stereoSampleProvider, 100);
        meteringProvider.StreamVolume += (s, e) => VolumeMeasured?.Invoke(this, e);
        CoreAudioEngine.AudioDevice.OutputDevice.Init(meteringProvider);
        CoreAudioEngine.AudioDevice.OutputDevice.Play();
    }

    public void AddTrack(TrackEngine trackEngine)
    {
        masterMixer.AddMixerInput(trackEngine.GetTrackAudio());
    }

    public void AddAudioTrack(TrackAudioEngine track)
    {
        // Add the track's metering-enabled audio to the master mixer
        masterMixer.AddMixerInput(track.GetTrackAudio());
    }

    public void RemoveAudioTrack(TrackAudioEngine track)
    {
        // Remove the track's audio from the master mixer
        masterMixer.RemoveMixerInput(track.GetTrackAudio());
    }

    public void RemoveAllTracks()
    {
        masterMixer.RemoveAllMixerInputs();
    }

    public void RemoveTrack(Track track)
    {
        masterMixer.RemoveMixerInput(track.Engine.GetTrackAudio());
    }

    public void AddMidiTrack(TrackMidiEngine track)
    {
        // Add the track's metering-enabled audio to the master mixer
        masterMixer.AddMixerInput(track.GetTrackAudio());
    }

    public void RemoveMidiTrack(TrackMidiEngine track)
    {
        // Remove the track's audio from the master mixer
        masterMixer.RemoveMixerInput(track.GetTrackAudio());
    }

    public void Dispose()
    {

    }
}

