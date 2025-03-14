using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using Lumix.Plugins;
using Melanchall.DryWetMidi.Core;
using Lumix.Clips.AudioClips;
using Lumix.SampleProviders;
using Vanara.PInvoke;
using Lumix.Views.Arrangement;

namespace Lumix.Tracks.AudioTracks;

public class TrackAudioEngine : TrackEngine, IDisposable
{
    private WaveInEvent inputDevice;
    private WaveFileWriter waveFileWriter;
    private string _tmpRecordId = string.Empty;

    public override event EventHandler<StreamVolumeEventArgs> VolumeMeasured;

    public TrackAudioEngine(AudioTrack audioTrack, int sampleRate = 44100, int channelCount = 2)
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
        TrackStateSampleProvider = new TrackStateSampleProvider(MeteringSampleProvider, audioTrack);
    }

    public override void Fire(AudioFileReader audioFile, float offset, float endOffset)
    {
        var input = new AudioFileReader(audioFile.FileName);
        input.CurrentTime = TimeSpan.FromSeconds(offset);
        var sample = new OffsetSampleProvider(input);
        //var skipped = sample.Skip(TimeSpan.FromSeconds(offset)); // it's slow
        var finalSample = sample.Take(TimeSpan.FromSeconds(input.TotalTime.TotalSeconds - endOffset - offset));
        if (!Mixer.WaveFormat.Equals(finalSample.WaveFormat))
        {
            User32.MessageBox(IntPtr.Zero, $"Can't play {Path.GetFileName(input.FileName)}", "WaveFormat exception", User32.MB_FLAGS.MB_ICONWARNING | User32.MB_FLAGS.MB_TOPMOST);
            return;
        }
        Mixer.AddMixerInput(ConvertToRightChannelCount(finalSample));
    }

    public override void Fire(MidiFile midiFile, float offset, float endOffset)
    {
        throw new NotImplementedException();
    }

    private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
    {
        if (input.WaveFormat.Channels == Mixer.WaveFormat.Channels)
        {
            return input;
        }
        if (input.WaveFormat.Channels == 1 && Mixer.WaveFormat.Channels == 2)
        {
            return new MonoToStereoSampleProvider(input);
        }
        throw new NotImplementedException("Not yet implemented this channel count conversion");
    }

    public override void StartRecording()
    {
        _tmpRecordId = Guid.NewGuid().ToString();
        _tmpRecordId += ".wav";

        inputDevice = new WaveInEvent
        {
            WaveFormat = new WaveFormat(Mixer.WaveFormat.SampleRate, Mixer.WaveFormat.Channels)
        };

        waveFileWriter = new WaveFileWriter(_tmpRecordId, inputDevice.WaveFormat);

        inputDevice.DataAvailable += (s, e) =>
        {
            waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
            waveFileWriter.Flush();
        };

        inputDevice.StartRecording();
        IsRecording = true;
    }

    public override void StopRecording(Track destTrack)
    {
        if (inputDevice != null)
        {
            inputDevice.StopRecording();
            inputDevice.Dispose();
            inputDevice = null;
        }

        if (waveFileWriter != null)
        {
            waveFileWriter.Dispose();
            waveFileWriter = null;
        }

        IsRecording = false;
        var audioClip = new AudioClip(destTrack as AudioTrack, new AudioClipData(_tmpRecordId), 0);
        destTrack.Clips.Add(audioClip);
    }

    public override void StopSounds()
    {
        Mixer.RemoveAllMixerInputs();
    }
}
