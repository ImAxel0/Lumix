using ImGuiNET;
using System.Numerics;

namespace Lumix.Views;

public class InfoBox
{
    private static float _timeFromLastHover;
    private static string _infoTitle = "Info Tab";
    private static string _infoDescription = string.Empty;

    public static void SetInfoData(string title, string description, bool force = false)
    {
        if (force)
        {
            _infoTitle = title;
            _infoDescription = description;
            _timeFromLastHover = 0f;
        }

        if (ImGui.IsItemHovered())
        {
            _infoTitle = title;
            _infoDescription = description;
            _timeFromLastHover = 0f;
        }
        else if (_timeFromLastHover > 1f)
        {
            _infoTitle = "Info Tab";
            _infoDescription = string.Empty;
        }
    }

    public static void Render()
    {
        _timeFromLastHover += ImGui.GetIO().DeltaTime;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.18f, 0.18f, 0.18f, 1f));
        float rectWidth = Math.Min(250f, ImGui.GetContentRegionAvail().Y);
        if (ImGui.BeginChild("info_box", new Vector2(rectWidth, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border))
        {
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            Vector3 border = new Vector3(0.13f, 0.14f, 0.17f) * 0.7f;
            ImGui.GetForegroundDrawList().AddRect(windowPos, windowPos + windowSize,
                ImGui.GetColorU32(new Vector4(border.X, border.Y, border.Z, 1.00f)), 4f, ImDrawFlags.None, 4f);

            ImGui.SeparatorText(_infoTitle);
            ImGui.TextWrapped(_infoDescription);
            ImGui.EndChild();
        }
        ImGui.PopStyleColor();
    }
}
