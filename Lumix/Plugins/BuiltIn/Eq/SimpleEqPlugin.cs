using IconFonts;
using ImGuiNET;
using Lumix.ImGuiExtensions;
using Lumix.Views.Preferences.Audio;
using NAudio.Dsp;
using System.Numerics;

namespace Lumix.Plugins.BuiltIn.Eq;

public class SimpleEqPlugin : BuiltInPlugin, IAudioProcessor
{
    private string _icon = string.Empty;
    private EqType _eqType;
    private enum EqType
    {
        None,
        LowPass,
        HighPass,
        LowShelf,
        HighShelf
    }

    private float _frequency = 4000f;
    private float _q = 0.71f;
    private float _gain = 0f;

    public T? GetPlugin<T>() where T : class
    {
        return null;
    }

    public void Process(float[] input, float[] output, int samplesRead)
    {
        if (_eqType == EqType.None)
        {
            _icon = string.Empty;
            return;
        }


        BiQuadFilter eq = null;
        switch (_eqType)
        {
            case EqType.LowPass:
                eq = BiQuadFilter.LowPassFilter(AudioSettings.SampleRate, _frequency, _q);
                _icon = Fontaudio.FilterLowpass;
                break;
            case EqType.HighPass:
                eq = BiQuadFilter.HighPassFilter(AudioSettings.SampleRate, _frequency, _q);
                _icon = Fontaudio.FilterHighpass;
                break;
            case EqType.LowShelf:
                eq = BiQuadFilter.LowShelf(AudioSettings.SampleRate, _frequency, _q, _gain);
                _icon = Fontaudio.FilterShelvingLo;
                break;
            case EqType.HighShelf:
                eq = BiQuadFilter.HighShelf(AudioSettings.SampleRate, _frequency, _q, _gain);
                _icon = Fontaudio.FilterShelvingHi;
                break;
        }

        for (int i = 0; i < samplesRead; i += 2)
        {
            output[i] = eq.Transform(input[i]);
            output[i + 1] = eq.Transform(input[i + 1]);
        }
    }

    public override void RenderRectContent()
    {
        ImGuiKnobs.Knob("Freq", ref _frequency, 0, 22000f, 10f, "%.0f", ImGuiKnobVariant.WiperOnly, 40, ImGuiKnobFlags.AlwaysClamp);
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _frequency = 4000f;
        }
        ImGui.SameLine(0, 30);
        ImGuiKnobs.Knob("Q", ref _q, 0.1f, 18f, 0.01f, "%.2f", ImGuiKnobVariant.WiperOnly, 40, ImGuiKnobFlags.AlwaysClamp);
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _q = 0.71f;
        }

        ImGui.BeginDisabled(_eqType != EqType.LowShelf && _eqType != EqType.HighShelf);
        ImGui.SameLine(0, 30);
        ImGuiKnobs.Knob("Gain", ref _gain, -15f, 15f, 0.1f, "%.1f", ImGuiKnobVariant.Wiper, 40, ImGuiKnobFlags.AlwaysClamp);
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _gain = 0;
        }
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.BeginChild("combo_controls", Vector2.Zero);
        ImGui.SeparatorText("Filter mode");

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        Fontaudio.Push();
        if (ImGui.BeginCombo("##Filter", $"{_icon} {_eqType}"))
        {
            foreach (var eqType in Enum.GetValues<EqType>())
            {
                string icon = string.Empty;
                switch (eqType)
                {
                    case EqType.LowPass:
                        icon = Fontaudio.FilterLowpass;
                        break;
                    case EqType.HighPass:
                        icon = Fontaudio.FilterHighpass;
                        break;
                    case EqType.LowShelf:
                        icon = Fontaudio.FilterShelvingLo;
                        break;
                    case EqType.HighShelf:
                        icon = Fontaudio.FilterShelvingHi;
                        break;
                }

                if (UiElement.SelectableColored($"{icon} {eqType}", eqType == _eqType))
                {
                    _eqType = eqType;
                }
            }
            ImGui.EndCombo();
        }
        Fontaudio.Pop();
        ImGui.EndChild();
    }

    public bool Enabled { get; set; } = true;
    public bool DeleteRequested { get; set; }
    public bool DuplicateRequested { get; set; }
    public override string PluginName => "SimpleEq";
    public override BuiltInCategory Category => BuiltInCategory.EQ;
}
