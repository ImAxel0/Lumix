using IconFonts;
using ImGuiNET;
using Lumix.ImGuiExtensions;
using System.Numerics;

namespace Lumix.Plugins.BuiltIn.Utilities;

public class UtilityPlugin : BuiltInPlugin, IAudioProcessor
{
    private float _volumeUi;
    private float _panUi;

    private float _leftVolume = 1.0f;
    private float _rightVolume = 1.0f;
    private float _pan = 0.0f;
    private float _width = 0.0f;
    private bool _mono;
    private bool _invertLeft;
    private bool _invertRight;

    private void SetGain(float gain)
    {
        _leftVolume = gain;
        _rightVolume = gain;
    }

    public T? GetPlugin<T>() where T : class
    {
        return null;
    }

    public void Process(float[] inputBuffer, float[] outputBuffer, int samplesRead)
    {
        for (int i = 0; i < samplesRead; i += 2)
        {
            float left = inputBuffer[i];
            float right = inputBuffer[i + 1];

            if (_mono)
            {
                // Mix left and right into mono
                float monoSample = (left + right) / 2;
                left = monoSample;
                right = monoSample;
            }
            else
            {
                // Apply width adjustment
                float mid = (left + right) / 2; // Mono (mid) component
                float side = (left - right) / 2; // Stereo (side) component

                // Adjust side component based on width
                side *= (_width + 100f) / 100f;

                left = mid + side;
                right = mid - side;
            }

            // Invert phase
            if (_invertLeft)
                left = -left;
            if (_invertRight)
                right = -right;

            // Apply panning
            float panLeft = _pan <= 0 ? 1.0f : 1.0f - _pan;
            float panRight = _pan >= 0 ? 1.0f : 1.0f + _pan;

            // Apply volume and panning
            outputBuffer[i] = left * _leftVolume * panLeft;
            outputBuffer[i + 1] = right * _rightVolume * panRight;
        }
    }

    public override void RenderRectContent()
    {
        // Volume
        if (ImGuiKnobs.Knob("Gain", ref _volumeUi, -90f, 35f, 0.1f, "%.1f", ImGuiKnobVariant.Wiper, 40, ImGuiKnobFlags.AlwaysClamp))
        {
            float linearVolume = (float)Math.Pow(10, _volumeUi / 20);
            SetGain(linearVolume);
        }
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _volumeUi = 0f;
            float linearVolume = (float)Math.Pow(10, _volumeUi / 20);
            SetGain(linearVolume);
        }
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _volumeUi = 0f;
            float linearVolume = (float)Math.Pow(10, _volumeUi / 20);
            SetGain(linearVolume);
        }
        ImGui.SameLine(0, 30);
        ImGui.BeginDisabled(_mono);
        ImGuiKnobs.Knob("Width", ref _width, -100f, 400f, 1f, "%.0f", ImGuiKnobVariant.Space, 40, ImGuiKnobFlags.AlwaysClamp);
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _width = 0f;
        }
        ImGui.EndDisabled();
        ImGui.SameLine(0, 30);
        // Pan
        if (ImGuiKnobs.Knob("Pan", ref _panUi, -50f, 50f, 0.1f, "%.0f", ImGuiKnobVariant.WiperDot, 40, ImGuiKnobFlags.AlwaysClamp))
        {
            float mappedPan = _panUi / 50f;
            _pan = mappedPan;
        }
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _panUi = 0f;
            float mappedPan = _panUi / 50f;
            _pan = mappedPan;
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        // Mono toggle button
        Fontaudio.Push();
        if (UiElement.Toggle(Fontaudio.Mute, _mono, new Vector4(0.95f, 0.58f, 0.13f, 1f), new Vector2(ImGui.GetContentRegionAvail().X, 30)))
        {
            _mono = !_mono;
        }
        if (UiElement.Toggle($"{Fontaudio.Stereo} L", _invertLeft, new Vector4(0.95f, 0.58f, 0.13f, 1f), new Vector2(ImGui.GetContentRegionAvail().X / 2, ImGui.GetContentRegionAvail().Y)))
        {
            _invertLeft = !_invertLeft;
        }
        ImGui.SameLine();
        if (UiElement.Toggle($"{Fontaudio.Stereo} R", _invertRight, new Vector4(0.95f, 0.58f, 0.13f, 1f), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y)))
        {
            _invertRight = !_invertRight;
        }
        Fontaudio.Pop();
    }

    public bool Enabled { get; set; } = true;
    public bool DeleteRequested { get; set; }
    public bool DuplicateRequested { get; set; }
    public override string PluginName => "Utility";
    public override BuiltInCategory Category => BuiltInCategory.Utilities;
}

