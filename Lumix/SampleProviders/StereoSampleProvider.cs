using NAudio.Wave;

namespace Lumix.SampleProviders;

public class StereoSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    public float LeftVolume { get; set; } = 1.0f;
    public float RightVolume { get; set; } = 1.0f;
    public float Pan { get; set; } = 0.0f; // -1.0f (left) to 1.0f (right)

    public StereoSampleProvider(ISampleProvider source)
    {
        if (source.WaveFormat.Channels != 2)
            throw new ArgumentException("Source must be stereo");
        this.source = source;
    }

    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = source.Read(buffer, offset, count);
        for (int i = 0; i < samplesRead; i += 2)
        {
            float left = buffer[offset + i];
            float right = buffer[offset + i + 1];

            // Apply panning
            float panLeft = Pan <= 0 ? 1.0f : 1.0f - Pan;
            float panRight = Pan >= 0 ? 1.0f : 1.0f + Pan;

            // Apply volume and panning
            buffer[offset + i] = left * LeftVolume * panLeft;
            buffer[offset + i + 1] = right * RightVolume * panRight;
        }
        return samplesRead;
    }

    public void SetGain(float gain)
    {
        LeftVolume = gain;
        RightVolume = gain;
    }
}