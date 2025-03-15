using ImGuiNET;
using System.Numerics;

namespace Lumix.ImGuiExtensions;

public class ImGuiTheme
{
    public static Vector4 SelectionCol = new(0.55f, 0.79f, 0.85f, 1);

    public static Vector4[] DefaultColors = {
        HtmlToVec4("#19B75B"),
        HtmlToVec4("#2467C7"),
        HtmlToVec4("#BD2E99"),
        HtmlToVec4("#AF1E59"),
        HtmlToVec4("#AFA721"),
        HtmlToVec4("#D57222"),
        HtmlToVec4("#1CA9B8"),
        HtmlToVec4("#7517C3"),
    };

    public static Vector4 GetRandomColor()
    {
        return DefaultColors[new Random().Next(0, ImGuiTheme.DefaultColors.Length)];
    }

    public static Vector4 HtmlToVec4(string htmlColor, float alpha = 1f)
    {
        if (htmlColor == null || htmlColor.Length != 7 || htmlColor[0] != '#')
            throw new ArgumentException("Invalid HTML color code");

        int r = Convert.ToInt32(htmlColor.Substring(1, 2), 16);
        int g = Convert.ToInt32(htmlColor.Substring(3, 2), 16);
        int b = Convert.ToInt32(htmlColor.Substring(5, 2), 16);

        return new Vector4(r / 255f, g / 255f, b / 255f, alpha);
    }

    public static void PushGreyTheme()
    {
        var style = ImGui.GetStyle();
        var colours = style.Colors;

        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 4f);

        colours[(int)ImGuiCol.Text] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
        colours[(int)ImGuiCol.TextDisabled] = new Vector4(0.36f, 0.36f, 0.36f, 1.00f);
        colours[(int)ImGuiCol.WindowBg] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
        colours[(int)ImGuiCol.Border] = new Vector4(0.10f, 0.10f, 0.10f, 1.00f); // changed
        colours[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colours[(int)ImGuiCol.FrameBg] = new Vector4(0.12f, 0.12f, 0.12f, 1.00f);
        colours[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.32f, 0.32f, 0.32f, 1.00f);
        colours[(int)ImGuiCol.FrameBgActive] = new Vector4(0.45f, 0.45f, 0.45f, 1.00f);
        colours[(int)ImGuiCol.TitleBg] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
        colours[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
        colours[(int)ImGuiCol.TitleBgActive] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
        //colours[(int)(int)(int)ImGuiCol.MenuBarBg]            = new Vector4(0.92f, 0.18f, 0.29f, 1.00f);
        colours[(int)ImGuiCol.MenuBarBg] = new Vector4(0.22f, 0.22f, 0.22f, 1.00f);
        colours[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.12f, 0.12f, 0.12f, 1.00f);
        colours[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
        colours[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colours[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colours[(int)ImGuiCol.CheckMark] = new Vector4(0.95f, 0.58f, 0.13f, 1f);
        colours[(int)ImGuiCol.SliderGrab] = new Vector4(0.47f, 0.77f, 0.83f, 1.00f);
        colours[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.92f, 0.18f, 0.29f, 1.00f);
        colours[(int)ImGuiCol.Button] = new Vector4(0.08f, 0.08f, 0.08f, 1.00f);
        colours[(int)ImGuiCol.ButtonHovered] = new Vector4(0.32f, 0.32f, 0.32f, 1.00f);
        colours[(int)ImGuiCol.ButtonActive] = new Vector4(0.45f, 0.45f, 0.45f, 1.00f);
        colours[(int)ImGuiCol.Header] = new Vector4(0.85f, 0.48f, 0.03f, 1f);
        colours[(int)ImGuiCol.HeaderHovered] = new Vector4(0.85f, 0.48f, 0.03f, 1f);
        colours[(int)ImGuiCol.HeaderActive] = new Vector4(0.85f, 0.48f, 0.03f, 1f);
        colours[(int)ImGuiCol.ResizeGrip] = new Vector4(0.47f, 0.77f, 0.83f, 1.00f);
        colours[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.92f, 0.18f, 0.29f, 1.00f);
        colours[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.92f, 0.18f, 0.29f, 1.00f);
        colours[(int)ImGuiCol.PlotLines] = new Vector4(0.86f, 0.93f, 0.89f, 1.00f);
        colours[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.92f, 0.18f, 0.29f, 1.00f);
        colours[(int)ImGuiCol.PlotHistogram] = new Vector4(0.86f, 0.93f, 0.89f, 1.00f);
        colours[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.92f, 0.18f, 0.29f, 1.00f);
        colours[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.85f, 0.48f, 0.03f, 1.00f);
        colours[(int)ImGuiCol.PopupBg] = new Vector4(0.16f, 0.16f, 0.16f, 1.00f);
        colours[(int)ImGuiCol.Separator] = new Vector4(0.08f, 0.08f, 0.08f, 1.00f);
        colours[(int)ImGuiCol.DragDropTarget] = new Vector4(0.95f, 0.58f, 0.13f, 1f);
    }
}
