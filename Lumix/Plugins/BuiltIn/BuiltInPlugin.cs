using ImGuiNET;
using Lumix.ImGuiExtensions;
using Lumix.Views;
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
public abstract class BuiltInPlugin
{
    private string _pluginId = Guid.NewGuid().ToString();
    public string PluginId => _pluginId;
    public abstract string PluginName { get; }
    public abstract BuiltInCategory Category { get; }

    /// <summary>
    /// Renders the devices view rectangle
    /// </summary>
    public void RenderRect(IAudioProcessor AudioProcessor)
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
