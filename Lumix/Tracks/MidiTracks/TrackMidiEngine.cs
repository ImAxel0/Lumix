using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using MidiFile = Melanchall.DryWetMidi.Core.MidiFile;
using Lumix.Plugins;
using Lumix.Views;
using Lumix.SampleProviders;

namespace Lumix.Tracks.MidiTracks;

public class TrackMidiEngine : TrackEngine, IDisposable
{
    private Playback _playback;
    public Playback Playback => _playback;

    private bool sustainPedalActive = false;

    public override event EventHandler<StreamVolumeEventArgs> VolumeMeasured;

    public TrackMidiEngine(MidiTrack midiTrack, int sampleRate = 44100, int channelCount = 2)
    {
        Mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };

        // Initialize Vst's chain to apply vst's effects (returns audio processed by all track vst's)
        PluginChainSampleProvider = new PluginChainSampleProvider(Mixer);

        StereoSampleProvider = new StereoSampleProvider(PluginChainSampleProvider);

        // Initialize MeteringSampleProvider for gain feedback
        MeteringSampleProvider = new MeteringSampleProvider(StereoSampleProvider, 100);
        MeteringSampleProvider.StreamVolume += (s, e) => VolumeMeasured?.Invoke(this, e);

        TrackStateSampleProvider = new TrackStateSampleProvider(MeteringSampleProvider, midiTrack);
    }

    public override void Fire(MidiFile midiFile, float offset, float endOffset)
    {
        _playback?.Dispose();

        _playback = midiFile.GetPlayback();
        _playback.TrackProgram = true;
        _playback.TrackNotes = true;
        _playback.TrackControlValue = true;
        _playback.TrackPitchValue = true;
        _playback.PlaybackStart = new MetricTimeSpan(TimeSpan.FromSeconds(offset));
        _playback.Speed = TopBarControls.Bpm / 120f;

        // Send MIDI events to VSTi's
        _playback.EventPlayed += (sender, e) =>
        {
            PluginChainSampleProvider.PluginInstrument?.ReceiveMidiEvent(e.Event);
        };
        
        _playback.Start();
    }

    public override void Fire(AudioFileReader audioFile, float offset, float endOffset)
    {
        throw new NotImplementedException();
    }

    public override void StopSounds()
    {
        _playback?.Stop();
        _playback?.MoveToStart();
    }

    public override void StartRecording()
    {
        throw new NotImplementedException();
    }

    public override void StopRecording(Track destTrack)
    {
        throw new NotImplementedException();
    }
}
