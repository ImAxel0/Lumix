using IconFonts;
using ImGuiNET;
using System.Numerics;

namespace Lumix.ImGuiExtensions;

public static class UiElement
{
    public static bool Toggle(string label, bool state, Vector4 colorON, Vector2 size)
    {
        bool result = false;
        Vector2 ButtonPos = ImGui.GetCursorScreenPos();
        Vector2 ButtonSize = size;

        var col = state ? colorON : new Vector4(0.27f, 0.27f, 0.27f, 1f);
        ImGui.PushStyleColor(ImGuiCol.Button, col);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, col);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, col);
        ImGui.PushStyleColor(ImGuiCol.Text, state ? new Vector4(0, 0, 0, 1f) : Vector4.One);
        if (ImGui.Button(label, ButtonSize))
        {
            result = true;
        }
        Vector4 borderColor = new Vector4(0, 0, 0, 1);
        float borderThickness = 1f;
        ImGui.GetWindowDrawList().AddRect(
            ButtonPos,
            new Vector2(ButtonPos.X + ButtonSize.X, ButtonPos.Y + ButtonSize.Y),
            ImGui.GetColorU32(borderColor),
            0.0f,
            ImDrawFlags.None,
            borderThickness
        );
        ImGui.PopStyleColor(4);
        return result;
    }

    public static bool RoundToggle(bool state, Vector4 colorON)
    {
        bool result = false;
        Vector2 ButtonPos = ImGui.GetCursorScreenPos() + new Vector2(0, 2f);
        Vector2 ButtonSize = ImGui.CalcTextSize(FontAwesome6.Circle);
        var col = state ? colorON : new Vector4(0.27f, 0.27f, 0.27f, 1f);
        ImGui.PushStyleColor(ImGuiCol.Text, col);
        ImGui.Text(FontAwesome6.Circle);
        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            result = true;
        }
        Vector4 borderColor = new Vector4(0, 0, 0, 1);
        float borderThickness = 1f;
        ImGui.GetWindowDrawList().AddRect(
            ButtonPos,
            new Vector2(ButtonPos.X + ButtonSize.X, ButtonPos.Y + ButtonSize.Y),
            ImGui.GetColorU32(borderColor),
            10f,
            ImDrawFlags.None,
            borderThickness
        );
        ImGui.PopStyleColor();
        return result;
    }

    public static bool Button(string label, Vector2 size)
    {
        Vector2 ButtonPos = ImGui.GetCursorScreenPos();

        const float rounding = 15f;
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.27f, 0.27f, 0.27f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.27f, 0.27f, 0.27f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.95f, 0.58f, 0.13f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, rounding);
        bool result = ImGui.Button(label, size);
        Vector2 ButtonSize = ImGui.GetItemRectSize();
        Vector4 borderColor = new Vector4(0, 0, 0, 1);
        float borderThickness = 1f;
        ImGui.GetWindowDrawList().AddRect(
            ButtonPos,
            new Vector2(ButtonPos.X + ButtonSize.X, ButtonPos.Y + ButtonSize.Y),
            ImGui.GetColorU32(borderColor),
            rounding,
            ImDrawFlags.None,
            borderThickness
        );
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar();
        return result;
    }

    public static bool DragSlider(string label, float width, ref float value, float step, float min, float max, string format, ImGuiSliderFlags flags)
    {
        bool result = false;
        Vector2 sliderPos = ImGui.GetCursorScreenPos();
        Vector2 sliderSize = new Vector2(width, ImGui.GetFrameHeight());
        Vector4 borderCol = new Vector4(0, 0, 0, 1f);

        ImGui.SetNextItemWidth(width);
        if (ImGui.DragFloat(label, ref value, step, min, max, format, flags))
        {
            result = true;
        }
        ImGui.GetWindowDrawList().AddRect(sliderPos, sliderPos + sliderSize, ImGui.GetColorU32(borderCol));
        return result;
    }

    public static bool SelectableColored(string label, bool selected, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
    {
        bool result;
        Vector4 textCol = selected ? new Vector4(0, 0, 0, 1) : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        ImGui.PushStyleColor(ImGuiCol.Text, textCol);
        result = ImGui.Selectable(label, selected, flags);
        ImGui.PopStyleColor();
        return result;
    }

    public static void Tooltip(string description)
    {
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.None))
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(description);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}
