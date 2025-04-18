using ImGuiNET;
using Lumix.ImGuiExtensions;
using Lumix.Views;
using Melanchall.DryWetMidi.Core;
using System.Numerics;

namespace Lumix.Plugins.BuiltIn;

public enum BuiltInCategory
{
    EQ,
    Utilities,
}

/// <summary>
/// Built in plugin data blueprint
/// </summary>
public abstract class BuiltInPlugin : IPlugin
{
    public abstract BuiltInCategory Category { get; }

    /// <inheritdoc/>
    public abstract bool Enabled { get; set; }

    /// <inheritdoc/>
    public abstract string PluginName { get; set; }

    /// <inheritdoc/>
    public string PluginId { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public abstract PluginType PluginType { get; }

    /// <inheritdoc/>
    public PluginKeyboard VKeyboard { get; }

    /// <inheritdoc/>
    public bool IsVst { get; } = false;

    public abstract void Dispose();
    public abstract void Process(float[] input, float[] output, int samplesRead);
    public abstract void ReceiveMidiEvent(MidiEvent midiEvent);

    /// <summary>
    /// Renders the devices view rectangle
    /// </summary>
    public void RenderRect(IPlugin AudioProcessor)
    {
        bool selected = DevicesView.SelectedPlugins.Contains(AudioProcessor);
        Vector4 menuBarCol = selected ? ImGuiTheme.SelectionCol : new Vector4(0.28f, 0.28f, 0.28f, 1);
        ImGui.PushStyleColor(ImGuiCol.MenuBarBg, menuBarCol);
        ImGui.BeginChild($"plugin_rect{PluginId}", Vector2.Zero, ImGuiChildFlags.Border | ImGuiChildFlags.AutoResizeX, ImGuiWindowFlags.MenuBar);

        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) && !ImGui.IsAnyItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
            {
                DevicesView.SelectedPlugins.Add(AudioProcessor);
            }
            else
            {
                DevicesView.SelectedPlugins.Clear();
                DevicesView.SelectedPlugins.Add(AudioProcessor);
            }
        }

        if (ImGui.BeginMenuBar())
        {
            var textCol = selected ? new Vector4(0, 0, 0, 1) : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            if (UiElement.RoundToggle(AudioProcessor.Enabled, new Vector4(0.95f, 0.58f, 0.13f, 1f)))
            {
                var processor = AudioProcessor;
                processor.Toggle();
            }
            ImGui.TextColored(textCol, PluginName);

            ImGui.EndMenuBar();
        }

        RenderRectContent();

        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Render the devices view rectangle content
    /// </summary>
    public abstract void RenderRectContent();
}
