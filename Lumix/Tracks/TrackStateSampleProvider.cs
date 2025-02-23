using Lumix.Tracks.AudioTracks;
using Lumix.Tracks.GroupTracks;
using Lumix.Tracks.MidiTracks;
using NAudio.Wave;

namespace Lumix.Tracks;

/// <summary>
/// Simple provider which fills the buffer with zeroes if the parent track is disabled
/// </summary>
public class TrackStateSampleProvider : ISampleProvider
{
    private Track _parentTrack;
    private readonly ISampleProvider source;
    public WaveFormat WaveFormat => source.WaveFormat;

    public TrackStateSampleProvider(ISampleProvider source, Track track)
    {
        this.source = source;
        _parentTrack = track;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = source.Read(buffer, offset, count);
        for (int i = 0; i < samplesRead; i += 2)
        {
            float leftChannel = buffer[offset + i];
            float rightChannel = buffer[offset + i + 1];

            if (!_parentTrack.Enabled)
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
