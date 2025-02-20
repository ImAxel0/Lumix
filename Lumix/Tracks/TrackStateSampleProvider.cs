using Lumix.Tracks.AudioTracks;
using Lumix.Tracks.MidiTracks;
using NAudio.Wave;

namespace Lumix.Tracks;

/// <summary>
/// Simple provider which fills the buffer with zeroes if the parent track is disabled
/// </summary>
public class TrackStateSampleProvider : ISampleProvider
{
    private AudioTrack _audioTrack;
    private MidiTrack _midiTrack;
    private readonly ISampleProvider source;
    public WaveFormat WaveFormat => source.WaveFormat;

    public TrackStateSampleProvider(ISampleProvider source, AudioTrack audioTrack)
    {
        this.source = source;
        _audioTrack = audioTrack;
    }

    public TrackStateSampleProvider(ISampleProvider source, MidiTrack midiTrack)
    {
        this.source = source;
        _midiTrack = midiTrack;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = source.Read(buffer, offset, count);
        for (int i = 0; i < samplesRead; i += 2)
        {
            float leftChannel = buffer[offset + i];
            float rightChannel = buffer[offset + i + 1];

            if (_audioTrack != null)
                if (!_audioTrack.Enabled)
                {
                    leftChannel = 0;
                    rightChannel = 0;
                }

            if (_midiTrack != null)
                if (!_midiTrack.Enabled)
                {
                    leftChannel = 0;
                    rightChannel = 0;
                }

            buffer[offset + i] = leftChannel;
            buffer[offset + i + 1] = rightChannel;
        }
        return samplesRead;
    }
}
