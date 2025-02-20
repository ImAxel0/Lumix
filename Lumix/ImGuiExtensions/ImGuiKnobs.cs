// Modified port of https://github.com/altschuler/imgui-knobs

using ImGuiNET;
using System.Numerics;

namespace Lumix.ImGuiExtensions;

public enum ImGuiKnobFlags
{
    NoTitle = 1 << 0,
    NoInput = 1 << 1,
    ValueTooltip = 1 << 2,
    DragHorizontal = 1 << 3,
    DragVertical = 1 << 4,
    Logarithmic = 1 << 5,
    AlwaysClamp = 1 << 6
}

public enum ImGuiKnobVariant
{
    Tick = 1 << 0,
    Dot = 1 << 1,
    Wiper = 1 << 2,
    WiperOnly = 1 << 3,
    WiperDot = 1 << 4,
    Stepped = 1 << 5,
    Space = 1 << 6,
}

public class ImGuiKnobs
{
    public struct ColorSet
    {
        public Vector4 idle;
        public Vector4 hovered;
        public Vector4 active;

        public ColorSet(Vector4 idle, Vector4 hovered, Vector4 active)
        {
            this.idle = idle;
            this.hovered = hovered;
            this.active = active;
        }

        public ColorSet(Vector4 color)
        {
            idle = color;
            hovered = color;
            active = color;
        }
    }

    private static void draw_arc2(Vector2 center, float radius, float start_angle, float end_angle, float thickness, Vector4 color)
    {
        var drawList = ImGui.GetWindowDrawList();

        drawList.PathArcTo(center, radius, start_angle, end_angle);
        drawList.PathStroke(ImGui.GetColorU32(color), ImDrawFlags.None, thickness);
    }

    private static ColorSet GetPrimaryColorSet()
    {
        var colors = ImGui.GetStyle().Colors;

        return new ColorSet(colors[(int)ImGuiCol.ButtonActive],
            colors[(int)ImGuiCol.ButtonHovered],
            colors[(int)ImGuiCol.ButtonActive]);
    }

    private static ColorSet GetSecondaryColorSet()
    {
        var colors = ImGui.GetStyle().Colors;
        var active = new Vector4(colors[(int)ImGuiCol.ButtonActive].X * 0.5f,
            colors[(int)ImGuiCol.ButtonActive].Y * 0.5f,
            colors[(int)ImGuiCol.ButtonActive].Z * 0.5f,
            colors[(int)ImGuiCol.ButtonActive].W);

        var hovered = new Vector4(colors[(int)ImGuiCol.ButtonHovered].X * 0.5f,
            colors[(int)ImGuiCol.ButtonHovered].Y * 0.5f,
            colors[(int)ImGuiCol.ButtonHovered].Z * 0.5f,
            colors[(int)ImGuiCol.ButtonHovered].W);

        return new ColorSet(active, hovered, hovered);
    }

    private static ColorSet GetTrackColorSet()
    {
        var colors = ImGui.GetStyle().Colors;

        return new ColorSet(colors[(int)ImGuiCol.Button]);
    }

    private static bool DragBehavior(string label, ref float value, float min, float max, float speed, string format, float radius, ImGuiKnobFlags flags)
    {
        ImGui.PushID(label);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, radius);

        // Save the cursor position to restore later
        Vector2 originalCursorPos = ImGui.GetCursorPos();

        // Calculate the position for the invisible button
        Vector2 screenPos = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(screenPos - new Vector2(radius - radius, radius + 5));

        //ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 1));
        ImGui.InvisibleButton(label, new Vector2((float)radius)); // Invisible interaction area
        //ImGui.PopStyleColor();

        // Restore the cursor position
        ImGui.SetCursorPos(originalCursorPos);

        ImGui.PopStyleVar();

        bool isActive = ImGui.IsItemActive();
        bool isHovered = ImGui.IsItemHovered();

        // Handle dragging behavior
        if (isActive)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            float delta;
            if (flags.HasFlag(ImGuiKnobFlags.DragVertical) && !flags.HasFlag(ImGuiKnobFlags.DragHorizontal))
            {
                delta = -io.MouseDelta.Y;
            }
            else if (flags.HasFlag(ImGuiKnobFlags.DragHorizontal) && !flags.HasFlag(ImGuiKnobFlags.DragVertical))
            {
                delta = io.MouseDelta.X;
            }
            else
            {
                delta = io.MouseDelta.X - io.MouseDelta.Y;
            }

            value += delta * speed;
            value = Math.Clamp(value, min, max);
        }

        if (isHovered)
        {
            ImGui.BeginTooltip();
            ImGui.Text(value.ToString("n1"));
            ImGui.EndTooltip();
        }

        ImGui.PopID();
        return isActive;
    }

    private struct knob
    {
        public float radius;
        public bool value_changed;
        public Vector2 center;
        public bool is_active;
        public bool is_hovered;
        public float angle_min;
        public float angle_max;
        public float t;
        public float angle;
        public float angle_cos;
        public float angle_sin;

        public void Knob(string label,
            ref float p_value,
            float v_min,
            float v_max,
            float speed,
            float _radius,
            string format,
            ImGuiKnobFlags flags,
            float _angle_min,
            float _angle_max)
        {
            radius = _radius;
            if (flags.HasFlag(ImGuiKnobFlags.Logarithmic))
            {
                float v = Math.Max(Math.Min(p_value, v_max), v_min);
                t = (float)((Math.Log(Math.Abs(v)) - Math.Log(Math.Abs(v_min))) / (Math.Log(Math.Abs(v_max)) - Math.Log(Math.Abs(v_min))));
            }
            else
            {
                t = ((float)p_value - v_min) / (v_max - v_min);
            }
            var screenPos = ImGui.GetCursorScreenPos();

            ImGui.Dummy(new Vector2(radius * 2f));

            var gid = ImGui.GetID(label);
            ImGuiSliderFlags dragBehaviourFlags = ImGuiSliderFlags.None;
            if (flags.HasFlag(ImGuiKnobFlags.AlwaysClamp))
            {
                dragBehaviourFlags |= ImGuiSliderFlags.AlwaysClamp;
            }
            if (flags.HasFlag(ImGuiKnobFlags.Logarithmic))
            {
                dragBehaviourFlags |= ImGuiSliderFlags.Logarithmic;
            }
            value_changed = DragBehavior(
                gid.ToString(),
                ref p_value,
                v_min,
                v_max,
                speed,
                format,
                _radius * 2,
                flags);

            angle_min = (float)(_angle_min < 0 ? Math.PI * 0.75f : _angle_min);
            angle_max = (float)(_angle_max < 0 ? Math.PI * 2.25f : _angle_max);

            center = new Vector2(screenPos.X + radius, screenPos.Y + radius);
            is_active = ImGui.IsItemActive();
            is_hovered = ImGui.IsItemHovered();
            angle = angle_min + (angle_max - angle_min) * t;
            angle_cos = (float)Math.Cos(angle);
            angle_sin = (float)Math.Sin(angle);
        }

        public void draw_dot(float size, float radius, float angle, ColorSet color, bool filled, int segments)
        {
            var dot_size = size * this.radius;
            var dot_radius = radius * this.radius;

            ImGui.GetWindowDrawList().AddCircleFilled(
                new Vector2((float)(center.X + Math.Cos(angle) * dot_radius),
                (float)(center.Y + Math.Sin(angle) * dot_radius)),
                dot_size,
                ImGui.GetColorU32(is_active ? color.active : is_hovered ? color.hovered : color.idle),
                segments);
        }

        public void draw_tick(float start, float end, float width, float angle, ColorSet color)
        {
            var tick_start = start * radius;
            var tick_end = end * radius;
            var angle_cos = Math.Cos(angle);
            var angle_sin = Math.Sin(angle);

            ImGui.GetWindowDrawList().AddLine(
                new Vector2((float)(center.X + angle_cos * tick_end), (float)(center.Y + angle_sin * tick_end)),
                new Vector2((float)(center.X + angle_cos * tick_start),
                (float)(center.Y + angle_sin * tick_start)),
                ImGui.GetColorU32(is_active ? color.active : is_hovered ? color.hovered : color.idle),
                width * radius);
        }

        public void draw_circle(float size, ColorSet color, bool filled, int segments)
        {
            var circle_radius = size * radius;

            ImGui.GetWindowDrawList().AddCircleFilled(
                center,
                circle_radius,
                ImGui.GetColorU32(is_active ? color.active : is_hovered ? color.hovered : color.idle));
        }

        public void draw_arc(float radius, float size, float start_angle, float end_angle, ColorSet color)
        {
            var track_radius = radius * this.radius;
            var track_size = size * this.radius * 0.5f + 0.0001f;

            draw_arc2(center, track_radius, start_angle, end_angle, track_size, is_active ? color.active : is_hovered ? color.hovered : color.idle);
        }
    }

    private static knob KnobWithDrag(
        string label,
        ref float p_value,
        float v_min,
        float v_max,
        float _speed,
        string format,
        float size,
        ImGuiKnobFlags flags,
        float angle_min,
        float angle_max)
    {
        if (flags.HasFlag(ImGuiKnobFlags.Logarithmic) && v_min <= 0 && v_max >= 0)
        {
            float decimalPrecision = 2;
            v_min = (float)Math.Pow(0.1f, decimalPrecision);
            v_max = Math.Max(v_min, v_max);
            p_value = Math.Max(Math.Min(p_value, v_max), v_min);
        }

        var speed = _speed == 0 ? (v_max - v_min) / 250f : _speed;
        ImGui.PushID(label);
        var width = size == 0 ? ImGui.GetTextLineHeight() * 4.0f : size * ImGui.GetIO().FontGlobalScale;
        ImGui.PushItemWidth(width);

        var drawList = ImGui.GetWindowDrawList();

        ImGui.BeginGroup();
        ImGui.BeginChild(label, Vector2.Zero, ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY);

        if (!flags.HasFlag(ImGuiKnobFlags.NoTitle))
        {
            var titleSize = ImGui.CalcTextSize(label, 0, false);

            ImGui.SetCursorPosX((width - titleSize.X) * 0.5f);
            drawList.AddText(ImGui.GetCursorScreenPos(), ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]), label);
            ImGui.TextColored(Vector4.Zero, label);
        }

        var k = new knob();
        k.Knob(label, ref p_value, v_min, v_max, speed, width * 0.5f, format, flags, angle_min, angle_max);

        if (flags.HasFlag(ImGuiKnobFlags.ValueTooltip) &&
            ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) ||
            ImGui.IsItemActivated())
        {
            ImGui.BeginTooltip();
            ImGui.Text(p_value.ToString());
            ImGui.EndTooltip();
        }

        if (!flags.HasFlag(ImGuiKnobFlags.NoInput))
        {
            ImGuiSliderFlags dragScalarFlags = ImGuiSliderFlags.None;
            if (flags.HasFlag(ImGuiKnobFlags.AlwaysClamp))
            {
                dragScalarFlags |= ImGuiSliderFlags.AlwaysClamp;
            }
            if (flags.HasFlag(ImGuiKnobFlags.Logarithmic))
            {
                dragScalarFlags |= ImGuiSliderFlags.Logarithmic;
            }
            ImGui.SetNextItemWidth(width);
            var changed = ImGui.DragFloat("###knob_drag", ref p_value, speed, v_min, v_max, format, dragScalarFlags | ImGuiSliderFlags.NoInput);
            if (changed)
            {
                k.value_changed = true;
            }
        }

        ImGui.EndChild();
        ImGui.EndGroup();
        ImGui.PopItemWidth();
        ImGui.PopID();

        return k;
    }

    private static bool BaseKnob(
        string label,
        ref float p_value,
        float v_min,
        float v_max,
        float speed,
        string format,
        ImGuiKnobVariant variant,
        float size,
        ImGuiKnobFlags flags,
        int steps,
        float angle_min,
        float angle_max)
    {
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.55f, 0.79f, 0.85f, 1));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.55f, 0.79f, 0.85f, 1));

        var knob = KnobWithDrag(
            label,
            ref p_value,
            v_min,
            v_max,
            speed,
            format,
            size,
            flags,
            angle_min,
            angle_max);

        switch (variant)
        {
            case ImGuiKnobVariant.Tick:
                knob.draw_circle(0.85f, GetSecondaryColorSet(), true, 32);
                knob.draw_tick(0.5f, 0.85f, 0.08f, knob.angle, GetPrimaryColorSet());
                break;
            case ImGuiKnobVariant.Dot:
                knob.draw_circle(0.85f, GetSecondaryColorSet(), true, 32);
                knob.draw_dot(0.12f, 0.6f, knob.angle, GetPrimaryColorSet(), true, 12);
                break;
            case ImGuiKnobVariant.Wiper:
                {
                    knob.draw_circle(0.7f, GetSecondaryColorSet(), true, 32);
                    knob.draw_arc(0.8f, 0.41f, knob.angle_min, knob.angle_max, GetTrackColorSet());

                    if (knob.t > 0.01f)
                    {
                        knob.draw_arc(0.8f, 0.43f, knob.angle_min, knob.angle, GetPrimaryColorSet());
                    }
                }
                break;
            case ImGuiKnobVariant.WiperOnly:
                {
                    knob.draw_arc(0.8f, 0.41f, knob.angle_min, knob.angle_max, GetTrackColorSet());

                    if (knob.t > 0.01f)
                    {
                        knob.draw_arc(0.8f, 0.43f, knob.angle_min, knob.angle, GetPrimaryColorSet());
                    }
                }
                break;
            case ImGuiKnobVariant.WiperDot:
                knob.draw_circle(0.6f, GetPrimaryColorSet(), true, 32);
                knob.draw_arc(0.85f, 0.41f, knob.angle_min, knob.angle_max, GetTrackColorSet());
                knob.draw_dot(0.1f, 0.85f, knob.angle, GetPrimaryColorSet(), true, 12);
                break;
            case ImGuiKnobVariant.Stepped:
                {
                    for (float n = 0f; n < steps; n++)
                    {
                        var a = n / (steps - 1);
                        var angle = knob.angle_min + (knob.angle_max - knob.angle_min) * a;
                        knob.draw_tick(0.7f, 0.9f, 0.04f, angle, GetPrimaryColorSet());
                    }

                    knob.draw_circle(0.6f, GetSecondaryColorSet(), true, 32);
                    knob.draw_dot(0.12f, 0.4f, knob.angle, GetPrimaryColorSet(), true, 12);
                }
                break;
            case ImGuiKnobVariant.Space:
                {
                    knob.draw_circle(0.3f - knob.t * 0.1f, GetSecondaryColorSet(), true, 16);

                    if (knob.t > 0.01f)
                    {
                        knob.draw_arc(0.4f, 0.15f, knob.angle_min - 1.0f, knob.angle - 1.0f, GetPrimaryColorSet());
                        knob.draw_arc(0.6f, 0.15f, knob.angle_min + 1.0f, knob.angle + 1.0f, GetPrimaryColorSet());
                        knob.draw_arc(0.8f, 0.15f, knob.angle_min + 3.0f, knob.angle + 3.0f, GetPrimaryColorSet());
                    }
                }
                break;
        }

        ImGui.PopStyleColor(2);
        return knob.value_changed;
    }

    public static bool Knob(string label,
        ref float p_value,
        float v_min,
        float v_max,
        float speed,
        string format,
        ImGuiKnobVariant variant,
        float size,
        ImGuiKnobFlags flags,
        int steps = 5,
        float angle_min = -270f,
        float angle_max = -0.001f)
    {
        return BaseKnob(
            label,
            ref p_value,
            v_min,
            v_max,
            speed,
            format,
            variant,
            size,
            flags,
            steps,
            angle_min,
            angle_max);
    }
}
