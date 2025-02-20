using ImGuiNET;
using Lumix.Views.Arrangement;

namespace Lumix.Views;

public static class Debug
{
    public static bool _show;

    public static void Render()
    {
        if (!_show)
            return;

        if (ImGui.Begin("Debug"))
        {
            if (ImGui.TreeNodeEx("Audio Tracks"))
            {
                for (int i = 0; i < ArrangementView.Tracks.Count; i++)
                {
                    var track = ArrangementView.Tracks[i];
                    if (ImGui.TreeNodeEx($"{i}: {track.Name}"))
                    {
                        if (ImGui.TreeNodeEx("Audio Clips"))
                        {
                            for (int j = 0; j < track.Clips.Count; j++)
                            {
                                var clip = track.Clips[j];
                                if (ImGui.TreeNodeEx($"{Path.GetFileName(clip.Name)}"))
                                {
                                    ImGui.Text($"Pos: {TimeLineV2.TimeToPosition(clip.StartTick)}");
                                    ImGui.Text($"Sec: {TimeLineV2.TicksToSeconds(clip.StartTick)}");
                                    ImGui.TreePop();
                                }
                            }
                            ImGui.TreePop();
                        }
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }
            ImGui.End();
        }
    }
}
