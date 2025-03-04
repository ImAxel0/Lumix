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
    private static bool _initialized;
    private readonly MixingSampleProvider masterMixer;
    public MixingSampleProvider MasterMixer => masterMixer;
    private readonly MeteringSampleProvider meteringProvider;
    private readonly StereoSampleProvider stereoSampleProvider;
    public StereoSampleProvider StereoSampleProvider => stereoSampleProvider;

    public event EventHandler<StreamVolumeEventArgs> VolumeMeasured;

    public MasterAudioEngine(int sampleRate = 44100, int channelCount = 2)
    {
        if (!_initialized)
        {
            AudioSettings.Init();
            _initialized = true;
        }

        masterMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };

        stereoSampleProvider = new StereoSampleProvider(masterMixer);

        // Add metering for the master track
        meteringProvider = new MeteringSampleProvider(stereoSampleProvider, 100);
        meteringProvider.StreamVolume += (s, e) => VolumeMeasured?.Invoke(this, e);
        AudioSettings.OutputDevice.Init(meteringProvider);
        AudioSettings.OutputDevice.Play();
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

