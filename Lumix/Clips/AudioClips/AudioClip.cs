using ImGuiNET;
using Lumix.Clips.Renderers;
using Lumix.Tracks.AudioTracks;
using Lumix.Views.Arrangement;
using NAudio.Wave;
using System.Numerics;

namespace Lumix.Clips.AudioClips;

public class AudioClip : Clip
{
    private AudioClipData _clip;
    public AudioClipData Clip => _clip;

    private (List<float> samples, float[] peaks, float[] valleys) _waveformData;

    public IntPtr WaveFormImage;

    public AudioClip(AudioTrack audioTrack, AudioClipData clip, long startingTick = 0)
    {
        Name = Path.GetFileNameWithoutExtension(clip.AudioFileReader.FileName);
        Track = audioTrack;
        Color = audioTrack.Color;
        _clip = clip;
        //TimeLinePosition = startingTime;
        StartTick = startingTick;
        _waveformData = WaveformRenderer.GetWaveformPeaks(_clip.AudioFileReader.FileName, (int)GetClipWidth());
    }

    public void ResizeWaveformData()
    {
        int targetWidth = (int)GetClipWidth();
        var data = WaveformRenderer.Resize(targetWidth, _waveformData.samples);
        _waveformData.peaks = data.peaks;
        _waveformData.valleys = data.valleys;
    }

    public void Split(float time)
    {
        /*
        // Ensure the split point is within bounds of the timeline position
        if (time <= Time || time >= Time + (float)_clip.AudioFileReader.TotalTime.TotalSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(time), "Split point must be within the clip's timeline range.");
        }

        // Calculate the offset from the start of the clip
        float offset = time - Time;

        // Calculate the split point in samples
        var sampleRate = _clip.AudioFileReader.WaveFormat.SampleRate;
        var channels = _clip.AudioFileReader.WaveFormat.Channels;
        int splitSamplePosition = (int)(offset * sampleRate);

        // Create buffers for split audio data
        var leftSamples = new List<float>();
        var rightSamples = new List<float>();

        // Read the audio data and split it
        float[] buffer = new float[1024];
        int samplesRead;
        int currentSample = 0;

        _clip.AudioFileReader.Position = 0; // Reset reader to the start
        while ((samplesRead = _clip.AudioFileReader.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                if (currentSample < splitSamplePosition * channels)
                {
                    leftSamples.Add(buffer[i]);
                }
                else
                {
                    rightSamples.Add(buffer[i]);
                }
                currentSample++;
            }
        }

        // Save the split parts as new audio files
        string leftFileName = $"{Name}_part1.wav";
        string rightFileName = $"{Name}_part2.wav";

        SaveAudio(leftFileName, leftSamples, sampleRate, channels);
        SaveAudio(rightFileName, rightSamples, sampleRate, channels);

        // Create new ClipAudioData for the split parts
        var leftClip = new AudioClipData(leftFileName);
        var rightClip = new AudioClipData(rightFileName);

        // Create new AudioClip instances for the split parts
        var leftAudioClip = new AudioClip(Track, leftClip, TimeLinePosition);
        var rightAudioClip = new AudioClip(Track, rightClip, TimeLinePosition + time * TopBarControls.Bpm - TimeLinePosition);

        // Add the new clips to the track
        Track.Clips.Add(leftAudioClip);
        Track.Clips.Add(rightAudioClip);

        // Mark the current clip for deletion
        DeleteRequested = true;
        */
    }

    private void SaveAudio(string fileName, List<float> samples, int sampleRate, int channels)
    {
        using (var writer = new WaveFileWriter(fileName, new WaveFormat(sampleRate, channels)))
        {
            float[] buffer = samples.ToArray();
            writer.WriteSamples(buffer, 0, buffer.Length);
        }
    }

    protected override long GetClipDuration()
    {
        return TimeLineV2.SecondsToTicks(_clip.AudioFileReader.TotalTime.TotalSeconds);
    }

    protected override float GetClipWidth()
    {
        return TimeLineV2.SecondsToTicks(_clip.AudioFileReader.TotalTime.TotalSeconds) * TimeLineV2.PixelsPerTick;
        //return (float)_clip.AudioFileReader.TotalTime.TotalSeconds * TopBarControls.Bpm * ArrangementView.Zoom;
    }

    protected override void RenderClipContent(float menuBarHeight, float clipHeight)
    {
        //ImGui.GetWindowDrawList().AddImage(WaveFormImage, ImGui.GetWindowPos() + new Vector2(0, menuBarHeight), ImGui.GetWindowPos() + ImGui.GetWindowSize() - new Vector2(0, 5));
        WaveformRenderer.RenderWaveformSafeArea((_waveformData.peaks, _waveformData.valleys),
            ImGui.GetWindowPos() + new Vector2(0, menuBarHeight), ClipWidth, clipHeight, Track.Enabled);
    }

    protected override void RenderClipContent(Vector2 pos, float width, float height)
    {
        //ImGui.GetWindowDrawList().AddImage(WaveFormImage, pos, pos + new Vector2(width, height));
        WaveformRenderer.RenderWaveformSafeArea((_waveformData.peaks, _waveformData.valleys),
            pos, width, height, Track.Enabled);
    }

    protected override void OnClipDoubleClickLeft()
    {

    }
}
