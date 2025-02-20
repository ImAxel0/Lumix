using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using MidiFile = Melanchall.DryWetMidi.Core.MidiFile;
using Melanchall.DryWetMidi.Common;
using Lumix.Plugins.VST;
using Lumix.Plugins;
using Lumix.Views;
using Lumix.SampleProviders;

namespace Lumix.Tracks.MidiTracks;

public class TrackMidiEngine : TrackEngine, IDisposable
{
    private Playback _playback;
    public Playback Playback => _playback;
    private bool isPlaying;

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

    public void SendNoteOnEvent(int channel, SevenBitNumber noteNumber, SevenBitNumber velocity)
    {
        var vstPlugin = PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
        vstPlugin?.SendNoteOn(channel, noteNumber, velocity);
        //VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOn(channel, noteNumber, velocity);
    }

    public void SendNoteOffEvent(int channel, SevenBitNumber noteNumber, SevenBitNumber velocity)
    {
        var vstPlugin = PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
        vstPlugin?.SendNoteOff(channel, noteNumber, velocity);
        //VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOff(channel, noteNumber, velocity);
    }

    public void SendSustainPedalEvent(int channel, bool state)
    {
        var vstPlugin = PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
        vstPlugin?.SendSustainPedal(channel, state);
    }

    public override void Fire(MidiFile midiFile, float offset, float endOffset)
    {
        if (isPlaying) return; // Prevent multiple playbacks

        _playback?.Dispose();

        _playback = midiFile.GetPlayback();
        _playback.TrackProgram = true;
        _playback.TrackNotes = true;
        _playback.PlaybackStart = new MetricTimeSpan(TimeSpan.FromSeconds(offset));
        _playback.Speed = TopBarControls.Bpm / 120f;

        // Send MIDI events to VSTi's
        _playback.EventPlayed += (sender, e) =>
        {
            if (e.Event is NoteOnEvent noteOn)
            {
                SendNoteOnEvent(0, noteOn.NoteNumber, noteOn.Velocity);
            }
            else if (e.Event is NoteOffEvent noteOff)
            {
                SendNoteOffEvent(0, noteOff.NoteNumber, noteOff.Velocity);
            }
            else if (e.Event is ControlChangeEvent ccEvent)
            {
                if (ccEvent.ControlNumber == 64) // Sustain pedal (CC 64)
                {
                    sustainPedalActive = ccEvent.ControlValue >= 64;
                    SendSustainPedalEvent(0, sustainPedalActive);
                }
                else if (ccEvent.ControlNumber == 7) // Volume (CC 7)
                {
                    float volume = ccEvent.ControlValue / 127f;
                }
                // handle other CC events like panning, modulation, etc. here.
            }
        };

        _playback.Start();
        isPlaying = true;
    }

    public override void Fire(AudioFileReader audioFile, float offset, float endOffset)
    {
        throw new NotImplementedException();
    }

    public override void StopSounds()
    {
        _playback?.Stop();
        _playback?.MoveToStart();
        isPlaying = false;
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
