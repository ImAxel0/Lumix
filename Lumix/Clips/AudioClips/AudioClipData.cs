using NAudio.Wave;

namespace Lumix.Clips.AudioClips;

public class AudioClipData
{
    public AudioFileReader AudioFileReader;
    public WaveChannel32 Wave32;

    public AudioClipData(string filePath)
    {
        AudioFileReader = new AudioFileReader(filePath);
        Wave32 = new WaveChannel32(AudioFileReader);
    }
}
