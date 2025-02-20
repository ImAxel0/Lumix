using ImGuiNET;
using Lumix.Views.Arrangement;
using NAudio.Wave;
using System.Numerics;

namespace Lumix.Clips.Renderers;

public class WaveformRenderer
{
    public static (List<float> samples, float[] peaks, float[] valleys) GetWaveformPeaks(string filePath, int targetWidth)
    {
        using var reader = new AudioFileReader(filePath);

        List<float> samples = new List<float>();
        float[] buffer = new float[1024];

        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            samples.AddRange(buffer.Take(read));
        }

        // Normalize and calculate peaks/valleys
        int samplesPerPixel = samples.Count / targetWidth;
        float[] peaks = new float[targetWidth];
        float[] valleys = new float[targetWidth];

        Parallel.For(0, targetWidth, i =>
        {
            int start = i * samplesPerPixel;
            int end = Math.Min(start + samplesPerPixel, samples.Count);

            float max = float.MinValue;
            float min = float.MaxValue;

            for (int j = start; j < end; j++)
            {
                float sample = samples[j];
                if (sample > max) max = sample;
                if (sample < min) min = sample;
            }

            peaks[i] = max;
            valleys[i] = min;
        });

        return (samples, peaks, valleys);
    }

    public static (float[] peaks, float[] valleys) Resize(int targetWidth, List<float> samples)
    {
        int samplesPerPixel = samples.Count / targetWidth;
        float[] peaks = new float[targetWidth];
        float[] valleys = new float[targetWidth];

        Parallel.For(0, targetWidth, i =>
        {
            int start = i * samplesPerPixel;
            int end = Math.Min(start + samplesPerPixel, samples.Count);

            float max = float.MinValue;
            float min = float.MaxValue;

            for (int j = start; j < end; j++)
            {
                float sample = samples[j];
                if (sample > max) max = sample;
                if (sample < min) min = sample;
            }

            peaks[i] = max;
            valleys[i] = min;
        });

        return (peaks, valleys);
    }

    public static void RenderWaveform((float[] peaks, float[] valleys) waveformData, Vector2 position, float width, float height, bool enabled)
    {
        var drawList = ImGui.GetWindowDrawList();
        Vector4 color = enabled ? Vector4.One : new Vector4(0.8f, 0.8f, 0.8f, 1f);
        float centerY = position.Y + height / 2f; // Middle of the waveform
        float scale = height / 2f; // Scale amplitude to fit height
        float pixelWidth = width / waveformData.peaks.Length;

        for (int i = 0; i < waveformData.peaks.Length; i++)
        {
            float x = position.X + i * pixelWidth;
            float peakY = centerY - waveformData.peaks[i] * scale;
            float valleyY = centerY - waveformData.valleys[i] * scale;

            // Draw line from valley to peak
            drawList.AddLine(new Vector2(x, valleyY), new Vector2(x, peakY), ImGui.ColorConvertFloat4ToU32(color));
        }

        // Draw center line
        drawList.AddLine(new Vector2(position.X, centerY), new Vector2(position.X + width, centerY), ImGui.ColorConvertFloat4ToU32(color));
    }

    public static void RenderWaveformSafeArea((float[] peaks, float[] valleys) waveformData, Vector2 position, float width, float height, bool enabled)
    {
        var drawList = ImGui.GetWindowDrawList();
        Vector4 color = enabled ? Vector4.One : new Vector4(0.8f, 0.8f, 0.8f, 1f);
        uint colorU32 = ImGui.ColorConvertFloat4ToU32(color);

        float centerY = position.Y + height / 2f; // Middle of the waveform
        float scale = height / 2f;               // Scale amplitude to fit height
        float pixelWidth = width / waveformData.peaks.Length;

        // Calculate the visible range
        float viewStartX = ArrangementView.WindowPos.X; // Left edge of the arrangement view
        float viewEndX = ArrangementView.ArrangementWidth + ArrangementView.WindowPos.X; // Right edge of the arrangement view

        int startIndex = Math.Max(0, (int)((viewStartX - position.X) / pixelWidth));
        int endIndex = Math.Min(waveformData.peaks.Length, (int)((viewEndX - position.X) / pixelWidth));

        // Render only visible lines
        for (int i = startIndex; i < endIndex; i++)
        {
            float x = position.X + i * pixelWidth;
            float peakY = centerY - waveformData.peaks[i] * scale;
            float valleyY = centerY - waveformData.valleys[i] * scale;

            // Draw line from valley to peak
            drawList.AddLine(new Vector2(x, valleyY), new Vector2(x, peakY), colorU32);
        }

        // Draw center line only if part of it is visible
        if (viewStartX < position.X + width && viewEndX > position.X)
        {
            float clippedStartX = Math.Max(position.X, viewStartX);
            float clippedEndX = Math.Min(position.X + width, viewEndX);
            drawList.AddLine(new Vector2(clippedStartX, centerY), new Vector2(clippedEndX, centerY), colorU32);
        }
    }
}
