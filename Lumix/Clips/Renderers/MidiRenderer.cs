using ImGuiNET;
using Lumix.Clips.MidiClips;
using Lumix.Views.Arrangement;
using Melanchall.DryWetMidi.Interaction;
using System.Numerics;

namespace Lumix.Clips.Renderers;

public class MidiRenderer
{
    private static int _minNote = 21;          // Minimum MIDI note to display (e.g., A0)
    private static int _maxNote = 108;         // Maximum MIDI note to display (e.g., C8)

    public static void RenderMidiDataPreview(MidiClipData midiClipData, Vector2 pos, float width, float height)
    {
        // Calculate vertical scaling dynamically
        int noteRange = _maxNote - _minNote + 1;
        float pixelsPerNote = height / noteRange;

        // Adjust _pixelsPerSecond based on the total clip duration and the width
        float clipDuration = (float)midiClipData.MidiFile.GetDuration<MetricTimeSpan>().TotalSeconds;
        float pixelsPerSecond = width / clipDuration;

        // Get the draw list
        var drawList = ImGui.GetWindowDrawList();
        Vector2 containerPos = pos;

        // Draw MIDI notes
        foreach (var note in midiClipData.Notes)
        {
            float noteStartTime = (float)note.Data.TimeAs<MetricTimeSpan>(midiClipData.TempoMap).TotalSeconds;
            float noteDuration = (float)note.Data.LengthAs<MetricTimeSpan>(midiClipData.TempoMap).TotalSeconds;

            float x = containerPos.X + noteStartTime * pixelsPerSecond;
            float y = containerPos.Y + (_maxNote - note.Data.NoteNumber) * pixelsPerNote;
            float noteWidth = noteDuration * pixelsPerSecond;
            float noteHeight = pixelsPerNote * 4f;

            // Clamp y to prevent notes from exceeding the child window
            if (y < containerPos.Y || y + noteHeight > containerPos.Y + height)
                continue;

            Vector4 color = Vector4.One; // Note color
            drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + noteWidth, y + noteHeight), ImGui.ColorConvertFloat4ToU32(color));
            //drawList.AddRect(new Vector2(x, y) - Vector2.One, new Vector2(x + noteWidth, y + noteHeight) + Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.4f))); // Border
        }

    }

    public static void RenderMidiData(MidiClipData midiClipData, Vector2 pos, float width, float height, bool enabled)
    {
        // Calculate vertical scaling dynamically
        int noteRange = _maxNote - _minNote + 1;
        float pixelsPerNote = height / noteRange;

        // Adjust _pixelsPerSecond based on the total clip duration and the width
        float clipDuration = (float)midiClipData.MidiFile.GetDuration<MetricTimeSpan>().TotalSeconds;
        float pixelsPerSecond = width / clipDuration / ArrangementView.Zoom;

        // Get the draw list
        var drawList = ImGui.GetWindowDrawList();
        Vector2 containerPos = pos;
        Vector4 color = enabled ? Vector4.One : new Vector4(0.8f, 0.8f, 0.8f, 1f); // Note color

        // Draw MIDI notes
        foreach (var note in midiClipData.Notes)
        {
            float noteStartTime = (float)note.Data.TimeAs<MetricTimeSpan>(midiClipData.TempoMap).TotalSeconds;
            float noteDuration = (float)note.Data.LengthAs<MetricTimeSpan>(midiClipData.TempoMap).TotalSeconds;

            float x = containerPos.X + noteStartTime * pixelsPerSecond * ArrangementView.Zoom;
            float y = containerPos.Y + (_maxNote - note.Data.NoteNumber) * pixelsPerNote;
            float noteWidth = noteDuration * pixelsPerSecond * ArrangementView.Zoom;
            float noteHeight = pixelsPerNote * 2f;

            // Don't render notes outside of the view
            //if (x < ArrangementView.WindowPos.X || x > ArrangementView.WindowPos.X + ArrangementView.ArrangementWidth)
            //continue;

            // Clamp y to prevent notes from exceeding the child window
            //if (y < containerPos.Y || y + noteHeight > containerPos.Y + height)
                //continue;

            drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + noteWidth, y + noteHeight), ImGui.ColorConvertFloat4ToU32(color));
            drawList.AddRect(new Vector2(x, y) - Vector2.One, new Vector2(x + noteWidth, y + noteHeight) + Vector2.One, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.4f))); // Border
        }
    }
}
