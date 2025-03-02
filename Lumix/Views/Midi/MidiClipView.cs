using IconFonts;
using ImGuiNET;
using Lumix.Clips.MidiClips;
using Lumix.ImGuiExtensions;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Tools;
using System.Numerics;

namespace Lumix.Views.Midi;

public static class MidiClipView
{
    public enum MidiClipViewTabs
    {
        Notes,
        Envelopes
    }

    public static MidiClip SelectedMidiClip { get; set; }
    private static MidiClipViewTabs SelectedTab { get; set; } = MidiClipViewTabs.Notes;

    public static void Render()
    {
        if (SelectedMidiClip != null)
        {
            // If the clip was deleted nullify it and switch do devices view
            if (SelectedMidiClip.DeleteRequested)
            {
                SelectedMidiClip = null;
                BottomView.RenderedWindow = BottomViewWindows.DevicesView;
                return;
            }
        }

        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.18f, 0.18f, 0.18f, 1f));
        if (ImGui.BeginChild("midi_clip_view", ImGui.GetContentRegionAvail(), ImGuiChildFlags.Border, ImGuiWindowFlags.MenuBar))
        {
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            if (ImGui.BeginMenuBar())
            {
                var clipName = string.IsNullOrWhiteSpace(SelectedMidiClip.Name) ? "Midi Clip" : SelectedMidiClip.Name;
                ImGui.Text(clipName);
                ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 60);
                if (UiElement.Toggle($"{FontAwesome6.Xmark}", true, new Vector4(0.95f, 0.58f, 0.13f, 1f), new Vector2(50, 25)))
                {
                    BottomView.RenderedWindow = BottomViewWindows.DevicesView;
                }

                ImGui.EndMenuBar();
            }

            Vector3 border = new Vector3(0.13f, 0.14f, 0.17f) * 0.7f;
            ImGui.GetForegroundDrawList().AddRect(windowPos, windowPos + windowSize,
                ImGui.GetColorU32(new Vector4(border.X, border.Y, border.Z, 1.00f)), 4f, ImDrawFlags.None, 4f);

            ImGui.Columns(3, "columns", true);
            ImGui.SetColumnWidth(0, 220);
            ImGui.SetColumnWidth(1, 220);

            string start = $"{SelectedMidiClip.StartMusicalTime.Bars}:{SelectedMidiClip.StartMusicalTime.Beats}:{SelectedMidiClip.StartMusicalTime.Ticks}";
            ImGui.InputText("Start", ref start, 100, ImGuiInputTextFlags.ReadOnly);

            string end = $"{SelectedMidiClip.EndMusicalTime.Bars}:{SelectedMidiClip.EndMusicalTime.Beats}:{SelectedMidiClip.EndMusicalTime.Ticks}";
            ImGui.InputText("End", ref end, 100, ImGuiInputTextFlags.ReadOnly);

            var length = SelectedMidiClip.EndMusicalTime - SelectedMidiClip.StartMusicalTime;
            string duration = $"{length.Bars}:{length.Beats}:{length.Ticks}";
            ImGui.InputText("Length", ref duration, 100, ImGuiInputTextFlags.ReadOnly);

            if (UiElement.Toggle($"{FontAwesome6.Repeat} Loop", false, new Vector4(0.95f, 0.58f, 0.13f, 1f), new Vector2(ImGui.GetContentRegionAvail().X, 25)))
            {
            }

            ImGui.NextColumn();
            if (ImGui.BeginChild("midi_clip_view_tabs", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.None))
            {
                if (UiElement.Toggle($"{FontAwesome6.Music}", SelectedTab == MidiClipViewTabs.Notes, new Vector4(0.95f, 0.58f, 0.13f, 1f), new Vector2(ImGui.GetContentRegionAvail().X / 2, 25)))
                {
                    SelectedTab = MidiClipViewTabs.Notes;
                }
                ImGui.SameLine(0, 5);
                if (UiElement.Toggle($"{FontAwesome6.Timeline}", SelectedTab == MidiClipViewTabs.Envelopes, new Vector4(0.95f, 0.58f, 0.13f, 1f), new Vector2(ImGui.GetContentRegionAvail().X, 25)))
                {
                    SelectedTab = MidiClipViewTabs.Envelopes;
                }
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (SelectedTab == MidiClipViewTabs.Notes)
                {
                    if (UiElement.Button($"{FontAwesome6.Divide}2", new Vector2(ImGui.GetWindowSize().X / 2, 0)))
                    {
                        SelectedMidiClip.MidiClipData.MidiFile.Resize(0.5f);
                        SelectedMidiClip.PianoRollEditor._notes = SelectedMidiClip.MidiClipData.MidiFile.GetNotes().ToList();
                        SelectedMidiClip.UpdateClipData(new MidiClipData(SelectedMidiClip.PianoRollEditor.ToMidiFile()));
                    }
                    ImGui.SameLine();
                    if (UiElement.Button($"{FontAwesome6.X}2", new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                    {
                        SelectedMidiClip.MidiClipData.MidiFile.Resize(2);
                        SelectedMidiClip.PianoRollEditor._notes = SelectedMidiClip.MidiClipData.MidiFile.GetNotes().ToList();
                        SelectedMidiClip.UpdateClipData(new MidiClipData(SelectedMidiClip.PianoRollEditor.ToMidiFile()));
                    }
                    if (UiElement.Button("Reverse", new Vector2(ImGui.GetWindowSize().X / 2, 0)))
                    {

                    }
                    ImGui.SameLine();
                    UiElement.Button("Invert", new Vector2(ImGui.GetContentRegionAvail().X, 0));
                    ImGui.Spacing();
                    UiElement.Button("Legato", new Vector2(ImGui.GetWindowSize().X / 2, 0));
                    ImGui.SameLine();
                    UiElement.Button("Duplicate", new Vector2(ImGui.GetContentRegionAvail().X, 0));
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                    ImGui.Text("Velocity Range");
                    ImGui.SameLine();
                    UiElement.DragSlider("##Velocity Range", ImGui.GetContentRegionAvail().X, ref _velocityRange, 2, -127, 127, "%.0f", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput);
                }

                ImGui.EndChild();
            }
            ImGui.NextColumn();
            SelectedMidiClip?.PianoRollEditor.Render();

            ImGui.EndChild();
        }
        ImGui.PopStyleColor();
    }

    private static float _velocityRange;
}
