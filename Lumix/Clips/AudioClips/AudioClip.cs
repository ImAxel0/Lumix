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
        return TimeLine.SecondsToTicks(_clip.AudioFileReader.TotalTime.TotalSeconds);
    }

    protected override float GetClipWidth()
    {
        return TimeLine.SecondsToTicks(_clip.AudioFileReader.TotalTime.TotalSeconds) * TimeLine.PixelsPerTick;
    }

    protected override void RenderClipContent(float menuBarHeight, float clipHeight)
    {
        WaveformRenderer.RenderWaveformSafeArea((_waveformData.peaks, _waveformData.valleys),
            ImGui.GetWindowPos() + new Vector2(0, menuBarHeight), ClipWidth, clipHeight, Track.Enabled);
    }

    protected override void RenderClipContent(Vector2 pos, float width, float height)
    {
        WaveformRenderer.RenderWaveformSafeArea((_waveformData.peaks, _waveformData.valleys),
            pos, width, height, Track.Enabled);
    }

    protected override void OnClipDoubleClickLeft()
    {

    }
}
