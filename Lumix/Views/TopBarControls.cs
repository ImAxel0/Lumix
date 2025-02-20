using IconFonts;
using ImGuiNET;
using Lumix.Clips.AudioClips;
using Lumix.ImGuiExtensions;
using Lumix.Tracks.AudioTracks;
using Lumix.Tracks.MidiTracks;
using Lumix.Views.Arrangement;
using Lumix.Views.Midi;
using System.Numerics;

namespace Lumix.Views;

public class TopBarControls
{
    private const float _defaultBpm = 120f;
    private static float _bpm = _defaultBpm;
    public static float Bpm => _bpm;
    private static bool _metronome;

    public static void Render()
    {
        if (ImGui.BeginChild("top_bar_controls", new(ImGui.GetContentRegionAvail().X, 30)))
        {
            //if (ImGui.BeginChild("##left_controls"))
            {
                if (UiElement.DragSlider("BPM", 80, ref _bpm, 1f, 20f, 999f, "%.2f", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput))
                {
                    ArrangementView.Tracks.ForEach(track =>
                    {
                        if (track is AudioTrack audioTrack)
                        {
                            audioTrack.Clips.ForEach(clip =>
                            {
                                if (clip is AudioClip audioClip)
                                    audioClip.ResizeWaveformData();
                            });
                        }
                        else if (track is MidiTrack midiTrack && track.Engine is TrackMidiEngine midiEngine)
                        {
                            var playback = midiEngine.Playback;
                            if (playback != null)
                            {
                                playback.Speed = _bpm / 120f; // adjusting speed of midi playbacks to new bpm's
                            }
                        }
                    });
                }
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    _bpm = _defaultBpm;
                    ArrangementView.Tracks.ForEach(track =>
                    {
                        if (track is AudioTrack audioTrack)
                        {
                            audioTrack.Clips.ForEach(clip =>
                            {
                                if (clip is AudioClip audioClip)
                                    audioClip.ResizeWaveformData();
                            });
                        }
                        else if (track is MidiTrack midiTrack && track.Engine is TrackMidiEngine midiEngine)
                        {
                            var playback = midiEngine.Playback;
                            if (playback != null)
                            {
                                playback.Speed = _bpm / 120f; // adjusting speed of midi playbacks to new bpm's
                            }
                        }
                    });
                }
                InfoBox.SetInfoData("BPM slider", "Allows to change BPM's.");
                ImGui.SameLine();

                Fontaudio.Push();
                if (UiElement.Toggle($"{Fontaudio.Metronome}", _metronome, new Vector4(0.95f, 0.58f, 0.13f, 1f), new(50, 25)))
                {
                    _metronome = !_metronome;
                }
                Fontaudio.Pop();
                InfoBox.SetInfoData("Metronome toggle", "Toggles metronome on or off.");
                ImGui.SameLine();
                ImGui.PushFont(Fontaudio.IconFontPtr);
                if (UiElement.Toggle($"{Fontaudio.Keyboard}", VirtualKeyboard.Enabled, new Vector4(0.95f, 0.58f, 0.13f, 1f), new(50, 25)))
                {
                    VirtualKeyboard.Toggle();
                }
                ImGui.PopFont();
                InfoBox.SetInfoData("Virtual keyboard", "Toggles virtual keyboard on or off, which allows to play notes directly from computer keyboard.\n\n" +
                    "[Z]/[X] Adjust octave range\n" +
                    "[C]/[V] Adjust velocity");

                //ImGui.SameLine();
                //float time = TimeLine.CurrentTime / Bpm;
                //ImGui.TextDisabled($"Time: {time:n1}");

                ImGui.SameLine();

                ImGui.BeginChild("ticks", Vector2.Zero, ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.FrameStyle);
                ImGui.TextDisabled($"Ticks: {TimeLineV2.GetCurrentTick()}");
                ImGui.EndChild();

                ImGui.SameLine();

                ImGui.BeginChild("sec", Vector2.Zero, ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.FrameStyle);
                ImGui.TextDisabled($"{TimeLineV2.TicksToSeconds(TimeLineV2.GetCurrentTick()):n1}s");
                ImGui.EndChild();

                ImGui.SameLine();

                ImGui.BeginChild("bar_beat_tick", Vector2.Zero, ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.FrameStyle);
                var beatsBarTicks = TimeLineV2.TicksToMusicalTime(TimeLineV2.GetCurrentTick(), true);
                ImGui.TextDisabled($"{beatsBarTicks.Bars}:{beatsBarTicks.Beats}:{beatsBarTicks.Ticks}");
                ImGui.EndChild();
                //ImGui.EndChild();
            }

            ImGui.SameLine();

            ImGui.SetCursorPosX(ImGui.GetIO().DisplaySize.X / 2 - ImGui.CalcTextSize("Play").X / 2);
            //if (ImGui.BeginChild("##playback_controls"))
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
                if (ImGui.Button($"{FontAwesome6.Play}"))
                {
                    TimeLineV2.StartPlayback();
                }
                InfoBox.SetInfoData("Play button", "Start playback.");
                ImGui.SameLine();
                if (ImGui.Button($"{FontAwesome6.Stop}"))
                {
                    TimeLineV2.StopPlayback(true);
                }
                InfoBox.SetInfoData("Play button", "Stop playback.");
                ImGui.SameLine();
                Fontaudio.Push();
                if (ImGui.Button($"{Fontaudio.Armrecording}"))
                {
                    TimeLine.StartRecording();
                }
                Fontaudio.Pop();
                InfoBox.SetInfoData("Play button", "Start playback and recording on recording enabled tracks.");
                ImGui.PopStyleColor(3);

                //ImGui.EndChild();
            }
            ImGui.EndChild();
        }
    }
}
