using IconFonts;
using ImGuiNET;
using Lumix.ImGuiExtensions;
using Lumix.Views;
using Lumix.Views.Arrangement;
using Lumix.Views.Preferences.Audio;
using System.Numerics;

namespace Lumix.Tracks.Master;

public class MasterTrack
{
    private MasterAudioEngine _masterEngine;
    private const string _name = "Master";
    private const bool _enabled = true;
    private float _volume;
    private float _pan;
    private Vector4 _color = new(0.26f, 0.26f, 0.26f, 1f);
    private float _length = 10000; // temporary length of imgui child window

    public MasterAudioEngine AudioEngine => _masterEngine;
    public string Name => _name;
    public bool Enabled => _enabled;
    public float Volume => _volume;
    public float Pan => _pan;
    public Vector4 Color => _color;
    public float Length => _length;

    private float _leftChannelGain;
    private float _rightChannelGain;

    public MasterTrack()
    {
        _masterEngine = new MasterAudioEngine(CoreAudioEngine.SampleRate);
        _masterEngine.VolumeMeasured += (sender, e) =>
        {
            // Get the maximum peak across all channels
            _leftChannelGain = e.MaxSampleValues[0];
            _rightChannelGain = e.MaxSampleValues[1];
        };
    }

    private float _trackTopPos;
    public float TrackTopPos => _trackTopPos;

    private bool _trackHasCursor;
    public bool TrackHasCursor => _trackHasCursor;

    public void RenderArrangement()
    {
        _trackTopPos = ImGui.GetCursorPosY();

        _trackHasCursor = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
        /*
        uint gridColorSecondary = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.3f)); // Minor grid lines

        Vector2 position = ImGui.GetWindowPos();
        float startX = position.X;
        float endX = startX + Length;

        float spacing = AdaptiveGrid.CalculateGridSpacing(ArrangementView.Zoom, AdaptiveGrid.MinSpacing, AdaptiveGrid.MaxSpacing);

        float currentTime = 0f; // Starting time in beats (or seconds, as desired)
        float currentPosition = startX;

        while (currentPosition <= endX)
        {
            // Draw the grid line
            ImGui.GetWindowDrawList().AddLine(new Vector2(currentPosition, position.Y), new Vector2(currentPosition, position.Y + 125), gridColorSecondary, 1);

            // Increment position and time
            currentPosition += spacing;
            currentTime += spacing / AdaptiveGrid.BaseSpacing; // Convert spacing to beat time
        }
        */
        RenderGridLines(ArrangementView.ArrangementWidth, 70);

        /*
        uint gridColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.5f));
        float gridThickness = 1.0f;
        Vector2 position = ImGui.GetWindowPos();
        float startX = position.X;
        float endX = startX + _length;
        float seconds = 0f;
        for (float x = startX; x <= endX; x += 120 * ArrangementView.Zoom * ArrangementView.BeatsPerBar)
        {
            if (seconds == 9)
                seconds = -1;

            if (seconds % 2 != 0)
            {
                seconds++;
                continue;
            }
            var textSize = ImGui.CalcTextSize(seconds.ToString()).X;
            //ImGui.GetWindowDrawList().AddText(new Vector2(x - (textSize / 3), position.Y), ImGui.GetColorU32(Vector4.One), $"{seconds}");
            ImGui.GetWindowDrawList().AddLine(new Vector2(x, position.Y), new Vector2(x, position.Y + 70), gridColor, gridThickness);
            seconds += 1;
        }
        */
    }

    private void RenderGridLines(float viewportWidth, float trackHeight)
    {
        long startTick = TimeLine.PositionToTime(ArrangementView.ArrangementScroolX);
        long endTick = TimeLine.PositionToTime(ArrangementView.ArrangementScroolX + viewportWidth);

        float pixelsPerTick = TimeLine.PixelsPerTick;
        long beatSpacing = TimeLine.PPQ;
        long barSpacing = (long)(beatSpacing * TimeLine.BeatsPerBar);

        long gridSpacing = barSpacing;
        //if (pixelsPerTick > 0.5f) gridSpacing = beatSpacing; // Zoomed in: Draw every beat
        //else if (pixelsPerTick < 0.01f) gridSpacing = barSpacing * 4; // Zoomed out: Draw every 4 bars

        for (long tick = (startTick / gridSpacing) * gridSpacing; tick <= endTick; tick += gridSpacing)
        {
            float xPosition = TimeLine.TimeToPosition(tick) - ArrangementView.ArrangementScroolX;

            if (tick % barSpacing == 0)
                DrawGridLine(ArrangementView.WindowPos.X + xPosition, new Vector4(0f, 0f, 0f, 0.3f), thickness: 1); // Bar line
            else if (gridSpacing == barSpacing)
                DrawGridLine(ArrangementView.WindowPos.X + xPosition, new Vector4(1, 0, 0, 0.5f), thickness: 1); // Beat line
        }
    }

    void DrawGridLine(float xPosition, Vector4 color, float thickness = 1f)
    {
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(xPosition, ImGui.GetWindowPos().Y),         // Start point
            new Vector2(xPosition, ImGui.GetWindowPos().Y + 125),       // End point
            ImGui.ColorConvertFloat4ToU32(color), // Convert color
            thickness
        );
    }

    public void RenderControls()
    {
        ImGui.Columns(3, "controls_column", false);
        ImGui.SetColumnWidth(0, 125);
        ImGui.TextColored(Vector4.One, _name);
        InfoBox.SetInfoData("Track name", "Name of the track.");

        ImGui.SetCursorPosY(ImGui.GetWindowSize().Y - ImGui.GetFrameHeightWithSpacing() - 5);
        ImGui.Text(FontAwesome6.Route);

        ImGui.GetWindowDrawList().AddLine(ImGui.GetWindowPos() + new Vector2(123, 0),
            ImGui.GetWindowPos() + new Vector2(123, 70),
            ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), 4f);

        float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }

        ImGui.NextColumn();
        ImGui.SetColumnWidth(1, 45);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg]);
        if (ImGui.BeginChild("volume_meter", new Vector2(20, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.FrameStyle))
        {
            var dl = ImGui.GetWindowDrawList();
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();
            float halfWidth = windowSize.X / 2;
            Vector4 vCol = Enabled ? new Vector4(0.58f, 0.98f, 0.29f, 1f) : new Vector4(0.49f, 0.49f, 0.49f, 1f);
            uint col = ImGui.GetColorU32(vCol);
            float alphaCol = 0.5f;

            // Define the smoothing factor
            float smoothingFactor = 0.1f; // Adjust this value to control the smoothing speed

            // Smooth gain variables (adjusted for smoother transitions)
            _smoothLeftChannelGain = Lerp(_smoothLeftChannelGain, _leftChannelGain, smoothingFactor);
            _smoothRightChannelGain = Lerp(_smoothRightChannelGain, _rightChannelGain, smoothingFactor);

            Vector2 lstart = new Vector2(windowPos.X, windowPos.Y + windowSize.Y * (1 - _smoothLeftChannelGain));
            Vector2 lend = new Vector2(windowPos.X + halfWidth, windowPos.Y + windowSize.Y);
            dl.AddRectFilledMultiColor(lstart, lend, col, col, ImGui.GetColorU32(col, alphaCol), ImGui.GetColorU32(col, alphaCol));

            Vector2 rstart = new Vector2(windowPos.X + halfWidth, windowPos.Y + windowSize.Y * (1 - _smoothRightChannelGain));
            Vector2 rend = new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y);
            dl.AddRectFilledMultiColor(rstart, rend, col, col, ImGui.GetColorU32(col, alphaCol), ImGui.GetColorU32(col, alphaCol));

            Vector2 lineStart = new Vector2(windowPos.X + halfWidth, windowPos.Y);
            Vector2 lineEnd = new Vector2(windowPos.X + halfWidth, windowPos.Y + windowSize.Y);
            dl.AddLine(lineStart, lineEnd, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), 1f);
        }
        ImGui.EndChild();
        InfoBox.SetInfoData("Gain meter", "Shows realtime audio gain");
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        ImGui.GetWindowDrawList().AddLine(ImGui.GetWindowPos() + new Vector2(165, 0),
            ImGui.GetWindowPos() + new Vector2(165, 70),
            ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), 2f);

        ImGui.NextColumn();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg]);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 2f);
        if (ImGui.BeginChild("button_controls", Vector2.Zero, ImGuiChildFlags.Border))
        {
            // volume control
            if (UiElement.DragSlider($"{FontAwesome6.VolumeHigh}##audio_track_volume", 40, ref _volume, 0.1f, -90f, 6f, "%.1f", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput))
            {
                float linearVolume = (float)Math.Pow(10, _volume / 20);
                _masterEngine.StereoSampleProvider.SetGain(linearVolume);
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _volume = 0f;
                float linearVolume = (float)Math.Pow(10, _volume / 20);
                _masterEngine.StereoSampleProvider.SetGain(linearVolume);
            }
            InfoBox.SetInfoData("Volume slider", "Controls track volume");
            ImGui.SameLine();
            // pan control
            if (UiElement.DragSlider($"{FontAwesome6.RightLeft}##audio_track_pan", 40, ref _pan, 0.1f, -50f, 50f, "%.0f", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput))
            {
                float mappedPan = _pan / 50f;
                _masterEngine.StereoSampleProvider.Pan = mappedPan;
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _pan = 0f;
                float mappedPan = _pan / 50f;
                _masterEngine.StereoSampleProvider.Pan = mappedPan;
            }
            InfoBox.SetInfoData("Panning slider", "Controls track panning");
        }
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
        ImGui.EndChild();
    }

    private float _smoothLeftChannelGain = 0.0f;
    private float _smoothRightChannelGain = 0.0f;
}
