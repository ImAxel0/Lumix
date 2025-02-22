using IconFonts;
using ImGuiNET;
using Lumix.Clips;
using Lumix.Clips.AudioClips;
using Lumix.Clips.MidiClips;
using Lumix.Tracks;
using Lumix.Tracks.AudioTracks;
using Lumix.Tracks.Master;
using Lumix.Tracks.MidiTracks;
using System.Numerics;

namespace Lumix.Views.Arrangement;

public static class ArrangementView
{
    public static MasterTrack MasterTrack { get; set; } = new();
    public static List<Track> Tracks { get; private set; } = new();

    private static float _beatsPerBar = 1;
    public static float BeatsPerBar => _beatsPerBar;

    private static float _zoom = 0.1f;
    public static float Zoom => _zoom;
    public static List<Clip> SelectedClips { get; set; } = new();

    public static void NewZoomChange(float value)
    {
        // Calculate the mouse position within the arrangement view
        float mousePosInWindowX = ImGui.GetMousePos().X - _windowPos.X;
        float mousePosInContentX = mousePosInWindowX + _arrangementScrollX;

        // Store the current zoom value
        float _previousValue = _zoom;

        // Update the zoom level
        if (value > 0)
        {
            _zoom = Math.Clamp(_zoom + 0.1f, 0.1f, 2f);
        }
        else
        {
            _zoom = Math.Clamp(_zoom - 0.1f, 0.1f, 2f);
        }

        if (_previousValue != _zoom)
        {
            // Calculate the new scroll offset to keep the content under the mouse consistent          
            float zoomFactor = _zoom / _previousValue;
            float target = Math.Clamp(mousePosInContentX * zoomFactor - mousePosInWindowX, 0, float.PositiveInfinity);
            ImGui.SetScrollX(target);
            _arrangementScrollX = target;

            // Resize waveforms or other elements if necessary
            Tracks.ToList().ForEach(track =>
            {
                if (track.TrackType == TrackType.Audio)
                {
                    track.Clips.ForEach(clip =>
                    {
                        if (clip is AudioClip audioClip)
                        {
                            audioClip.ResizeWaveformData();
                        }
                    });
                }
            });
        }
    }

    public static void ZoomChange(float value)
    {
        const float epsilon = 0.0001f;

        // Calculate the mouse position within the arrangement view
        float mousePosInWindowX = ImGui.GetMousePos().X - _windowPos.X;
        float mousePosInContentX = mousePosInWindowX + _arrangementScrollX;
        float t = _zoom <= 0.1f + epsilon && value < 0 || _zoom < 0.1f - epsilon && value > 0 ? 0.01f : 0.1f;

        // Store the current zoom value
        float _previousValue = _zoom;

        // Update the zoom level
        if (value > 0)
        {
            _zoom = Math.Clamp(_zoom + t, 0.05f, 2f);
        }
        else
        {
            _zoom = Math.Clamp(_zoom - t, 0.05f, 2f);
        }

        if (_previousValue != _zoom)
        {
            // Calculate the new scroll offset to keep the content under the mouse consistent          
            float zoomFactor = _zoom / _previousValue;
            ImGui.SetScrollX(Math.Clamp(mousePosInContentX * zoomFactor - mousePosInWindowX, 0, float.PositiveInfinity));

            // Resize waveforms or other elements if necessary
            Tracks.ToList().ForEach(track =>
            {
                if (track.TrackType == TrackType.Audio)
                {
                    track.Clips.ForEach(clip =>
                    {
                        if (clip is AudioClip audioClip)
                        {
                            audioClip.ResizeWaveformData();
                        }
                    });
                }
            });
        }
    }

    public static void Init()
    {
        for (int i = 0; i < 5; i++)
        {
            NewAudioTrack($"Track {Tracks.Count}");
        }
        for (int i = 0; i < 5; i++)
        {
            NewMidiTrack($"Track {Tracks.Count}");
        }
    }

    public static AudioTrack NewAudioTrack(string name, int index = -1)
    {
        AudioTrack track = new(name);
        if (index == -1)
        {
            Tracks.Add(track);
            return track;
        }
        Tracks.Insert(index, track);
        return track;
    }

    public static MidiTrack NewMidiTrack(string name, int index = -1)
    {
        MidiTrack track = new(name);
        if (index == -1)
        {
            Tracks.Add(track);
            return track;
        }
        Tracks.Insert(index, track);
        return track;
    }

    private static void ListenForShortcuts()
    {
        if (ImGui.IsKeyPressed(ImGuiKey.Delete, false))
        {
            SelectedClips.ForEach(c => c.DeleteRequested = true);
        }

        if (ImGui.IsKeyDown(ImGuiKey.ReservedForModCtrl) && ImGui.IsKeyPressed(ImGuiKey.D, false))
        {
            SelectedClips.ForEach(c => c.DuplicateRequested = true);
        }

        if (ImGui.IsKeyPressed(ImGuiKey._0, false))
        {
            SelectedClips.ForEach(c => c.Enabled = !c.Enabled);
        }

        if (ImGui.IsKeyPressed(ImGuiKey.RightArrow, true))
        {
            SelectedClips.ForEach(c =>
            {

            });
        }

        if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow, true))
        {
            SelectedClips.ForEach(c =>
            {

            });
        }

        // Create new audio track after selected track
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && !ImGui.IsKeyDown(ImGuiKey.ModShift) && ImGui.IsKeyPressed(ImGuiKey.T, false))
        {
            DevicesView.SelectedTrack = NewAudioTrack($"Audio Track {Tracks.Count}", Tracks.IndexOf(DevicesView.SelectedTrack) + 1);
        }

        // Create new midi track after selected track
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyDown(ImGuiKey.ModShift) && ImGui.IsKeyPressed(ImGuiKey.T, false))
        {
            DevicesView.SelectedTrack = NewMidiTrack($"Midi Track {Tracks.Count}", Tracks.IndexOf(DevicesView.SelectedTrack) + 1);
        }
    }

    public static void Render()
    {
        ListenForShortcuts();

        if (ImGui.BeginChild("arrangement_view", new(ImGui.GetContentRegionAvail().X - 20, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.None, ImGuiWindowFlags.MenuBar))
        {
            Vector2 windowPos = _windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            Vector3 border = new Vector3(0.13f, 0.14f, 0.17f) * 0.7f;
            ImGui.GetForegroundDrawList().AddRect(windowPos, windowPos + windowSize,
                ImGui.GetColorU32(new Vector4(border.X, border.Y, border.Z, 1.00f)), 4f, ImDrawFlags.None, 4f);

            if (ImGui.BeginMenuBar())
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
                if (ImGui.Button($"{FontAwesome6.Plus} Audio track"))
                {
                    NewAudioTrack($"Audio Track {Tracks.Count}");
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.Button($"{FontAwesome6.Plus} Midi track"))
                {
                    NewMidiTrack($"Midi Track {Tracks.Count}");
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                ImGui.PopStyleColor(3);

                ImGui.EndMenuBar();
            }
            float menuBarHeight = ImGui.GetFrameHeight();

            if (ImGui.BeginChild("timeline_bars", new Vector2(ImGui.GetContentRegionAvail().X - 10, 20), ImGuiChildFlags.FrameStyle))
            {
                long startTick = TimeLineV2.PositionToTime(ArrangementView.ArrangementScroolX);
                long endTick = TimeLineV2.PositionToTime(ArrangementView.ArrangementScroolX + ArrangementWidth);

                float pixelsPerTick = TimeLineV2.PixelsPerTick;
                long beatSpacing = TimeLineV2.PPQ;
                long barSpacing = (long)(beatSpacing * TimeLineV2.BeatsPerBar);

                float minTextSpacing = 60f; 
                long gridSpacing = barSpacing;

                if (pixelsPerTick * gridSpacing < minTextSpacing)
                {
                    while (pixelsPerTick * gridSpacing < minTextSpacing)
                    {
                        gridSpacing += barSpacing;
                    }
                }

                for (long tick = (startTick / gridSpacing) * gridSpacing; tick <= endTick; tick += gridSpacing)
                {
                    float xPosition = TimeLineV2.TimeToPosition(tick) - ArrangementView.ArrangementScroolX + ArrangementView.WindowPos.X;
                    var musicalTime = TimeLineV2.TicksToMusicalTime(tick, true);
                    xPosition -= ImGui.CalcTextSize($"{musicalTime.Bars}.{musicalTime.Beats}").X / 2;

                    if (tick % barSpacing == 0)
                    {
                        ImGui.GetWindowDrawList().AddText(new(xPosition, ImGui.GetWindowPos().Y),
                            ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]),
                            $"{musicalTime.Bars}.{musicalTime.Beats}");
                    }
                }
                ImGui.EndChild();
            }

            if (ImGui.BeginChild("arrangement_scroll_view", new(ImGui.GetContentRegionAvail().X - 10, ImGui.GetContentRegionAvail().Y),
                ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                //float scrollY = ImGui.GetScrollY(); 

                bool _zoomedThisFrame = false;
                List<Vector2> trackArrangementPos = new();
                float width = ImGui.GetContentRegionAvail().X - 360;
                ImGui.BeginChild("arrangement_horizontal_scroll", new(width, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysHorizontalScrollbar);
                _arrangementScrollX = ImGui.GetScrollX();
                _arrangementScrollY = ImGui.GetScrollY();
                _arrangementWidth = width;

                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows | ImGuiHoveredFlags.NoPopupHierarchy))
                {
                    // change timeline position
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !TimeLineV2.IsPlaying() && !ImGui.IsKeyDown(ImGuiKey.ReservedForModCtrl))
                    {
                        long newTime = TimeLineV2.SnapToGrid(TimeLineV2.PositionToTime(ImGui.GetMousePos().X + _arrangementScrollX - windowPos.X));
                        TimeLineV2.SetCurrentTick(newTime);    
                        /*
                        float mousePosX = ImGui.GetMousePos().X - windowPos.X;
                        float adjustedMousePosX = mousePosX + _arrangementScrollX;
                        float newTime = adjustedMousePosX / Zoom;
                        float snappedPosition = AdaptiveGrid.GetSnappedPosition(newTime);
                        TimeLine.SetTime(snappedPosition);
                        */
                        /*
                        // Get the mouse position within the window
                        float mousePosX = ImGui.GetMousePos().X - windowPos.X;

                        // Adjust the mouse position based on the scroll offset
                        float adjustedMousePosX = mousePosX + _arrangementScrollX;                      

                        // Convert the mouse position in pixels to time, considering the zoom factor
                        float newTime = adjustedMousePosX / Zoom;                        
                        float stepLength = 120 * ArrangementView.BeatsPerBar * 2;                     
                        float snappedPosition = MathF.Round(newTime / stepLength) * stepLength;
                        // Set the timeline's time to the new value
                        TimeLine.SetTime(snappedPosition);
                        */
                    }

                    float scrollDelta = ImGui.GetIO().MouseWheel;
                    if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && scrollDelta != 0)
                    {
                        NewZoomChange(scrollDelta);
                        _zoomedThisFrame = true;
                        //ZoomChange(scrollDelta);
                    }

                    // Scrolling controls
                    if (ImGui.IsMouseDown(ImGuiMouseButton.Middle))
                    {
                        var io = ImGui.GetIO();
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                        ImGui.SetScrollX(-io.MouseDelta.X + _arrangementScrollX);
                        ImGui.SetScrollY(-io.MouseDelta.Y + _arrangementScrollY);
                    }
                }
                /*
                if (ImGui.BeginChild("timeline_seconds", new Vector2(ImGui.GetContentRegionAvail().X + _arrangementScrollX, 20), ImGuiChildFlags.FrameStyle))
                {
                    long startTick = TimeLineV2.PositionToTime(ArrangementView.ArrangementScroolX);
                    long endTick = TimeLineV2.PositionToTime(ArrangementView.ArrangementScroolX + ArrangementWidth);

                    float pixelsPerTick = TimeLineV2.PixelsPerTick;
                    long beatSpacing = TimeLineV2.PPQ;
                    long barSpacing = beatSpacing * TimeLineV2.BeatsPerBar;

                    long gridSpacing = barSpacing;

                    for (long tick = (startTick / gridSpacing) * gridSpacing; tick <= endTick; tick += gridSpacing)
                    {
                        float xPosition = TimeLineV2.TimeToPosition(tick) - ArrangementView.ArrangementScroolX + ArrangementView.WindowPos.X;
                        var sec = (float)TimeLineV2.TicksToSeconds(tick);
                        xPosition -= ImGui.CalcTextSize($"{sec:n2}s").X / 2;

                        if (sec == 0)
                            continue;

                        if (tick % barSpacing == 0)
                        {
                            ImGui.GetWindowDrawList().AddText(new Vector2(xPosition, ImGui.GetWindowPos().Y),
                                ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]),
                                $"{sec:n2}s");
                        }
                    }
                }
                ImGui.EndChild();
                */
                /*
                if (ImGui.BeginChild("timeline_bars", new Vector2(ImGui.GetContentRegionAvail().X + _arrangementScrollX, 20), ImGuiChildFlags.FrameStyle))
                {
                    long startTick = TimeLineV2.PositionToTime(ArrangementView.ArrangementScroolX);
                    long endTick = TimeLineV2.PositionToTime(ArrangementView.ArrangementScroolX + ArrangementWidth);

                    float pixelsPerTick = TimeLineV2.PixelsPerTick;
                    long beatSpacing = TimeLineV2.PPQ;
                    long barSpacing = beatSpacing * TimeLineV2.BeatsPerBar;

                    long gridSpacing = barSpacing;

                    for (long tick = (startTick / gridSpacing) * gridSpacing; tick <= endTick; tick += gridSpacing)
                    {
                        float xPosition = TimeLineV2.TimeToPosition(tick) - ArrangementView.ArrangementScroolX + ArrangementView.WindowPos.X;
                        var musicalTime = TimeLineV2.TicksToMusicalTime(tick, true);
                        xPosition -= ImGui.CalcTextSize($"{musicalTime.Bars}.{musicalTime.Beats}").X / 2;

                        if (tick % barSpacing == 0)
                        {
                            ImGui.GetWindowDrawList().AddText(new(xPosition, ImGui.GetWindowPos().Y), 
                                ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]),
                                $"{musicalTime.Bars}.{musicalTime.Beats}");
                        }
                    }
                }
                ImGui.EndChild();
                */
                /*
                if (ImGui.BeginChild("timeline_seconds", new Vector2(ImGui.GetContentRegionAvail().X + _arrangementScrollX, 20), ImGuiChildFlags.FrameStyle))
                {
                    Vector4 color = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
                    Vector2 position = ImGui.GetWindowPos();
                    float startX = position.X;
                    float endX = startX + 10000;
                    float seconds = 0f;
                    for (float x = startX; x <= endX; x += TopBarControls.Bpm * Zoom)
                    {
                        //if (seconds == 9)
                        //seconds = -1;

                        if (seconds % 4 != 0 || seconds == 0)
                        {
                            seconds++;
                            continue;
                        }
                        var textSize = ImGui.CalcTextSize(seconds.ToString()).X;
                        ImGui.GetWindowDrawList().AddText(new Vector2(x - textSize / 3, position.Y), ImGui.GetColorU32(color), $"{seconds}");
                        seconds += 1;
                    }
                }
                ImGui.EndChild();
                */
                // Getting longest clip in arrangement to calculate arrangement width
                List<Clip> clips = new();
                Tracks.ToList().ForEach(t => clips.AddRange(t.Clips));
                float minLength = ImGui.GetContentRegionAvail().X;
                _maxClipLength = minLength;
                if (clips.Count > 0)
                {
                    _maxClipLength = clips.Max(clip => TimeLineV2.TimeToPosition(clip.StartTick) + clip.ClipWidth); // clip.TimeLinePosition * Zoom + 
                }
                if (_maxClipLength < minLength)
                    _maxClipLength = minLength;

                Vector2 masterTrackArrangementPos;
                ImGui.PushStyleColor(ImGuiCol.ChildBg, MasterTrack.Color);
                ImGui.BeginChild($"master_track_arrangement", new(_maxClipLength, 70), ImGuiChildFlags.None);
                masterTrackArrangementPos = ImGui.GetCursorScreenPos();
                MasterTrack.RenderArrangement();
                ImGui.EndChild();
                ImGui.PopStyleColor();

                foreach (var track in Tracks.ToList())
                {
                    if (!track.Enabled)
                        ImGui.BeginDisabled();

                    ImGui.PushStyleColor(ImGuiCol.MenuBarBg, track.Color);
                    ImGui.PushStyleColor(ImGuiCol.Border, track.Color);
                    float bgCol = DevicesView.SelectedTrack == track && track.Enabled ? 0.26f : 0.22f;
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(bgCol, bgCol, bgCol, 1.00f));
                    ImGui.BeginChild($"track_arrangement{track.Id}", new(_maxClipLength, 125), ImGuiChildFlags.None);
                    trackArrangementPos.Add(ImGui.GetCursorScreenPos());

                    if (ImGui.BeginPopupContextWindow("arrangement_popup", ImGuiPopupFlags.MouseButtonRight))
                    {
                        RenderPopupMenu();
                        ImGui.EndPopup();
                    }

                    track.RenderArrangement();

                    ImGui.EndChild();
                    ImGui.PopStyleColor(3);

                    if (!track.Enabled)
                        ImGui.EndDisabled();
                }

                ImGui.EndChild(); // end of "arrangement_horizontal_scroll" child window ???

                if (!_zoomedThisFrame) // If zoomed on this frame, skip rendering cause of visible artifact
                {
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, MasterTrack.Color);
                    ImGui.SetCursorScreenPos(new Vector2(masterTrackArrangementPos.X + _arrangementScrollX + width, masterTrackArrangementPos.Y));
                    ImGui.BeginChild($"master_track_controls", new(ImGui.GetContentRegionAvail().X, 70), ImGuiChildFlags.Border, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                    MasterTrack.RenderControls();
                    ImGui.EndChild();
                    ImGui.PopStyleColor();
                }

                int trackIndex = 0;
                foreach (var track in Tracks.ToList())
                {
                    if (_zoomedThisFrame) // If zoomed on this frame, skip controls rendering cause of visible artifact
                        continue;

                    ImGui.PushStyleColor(ImGuiCol.ChildBg, track.Color);
                    ImGui.SetCursorScreenPos(new Vector2(trackArrangementPos[trackIndex].X + _arrangementScrollX + width, trackArrangementPos[trackIndex].Y));
                    ImGui.BeginChild($"track_controls{track.Id}", new(ImGui.GetContentRegionAvail().X, 125), ImGuiChildFlags.Border, ImGuiWindowFlags.NoScrollbar);

                    // Draw selection line
                    if (DevicesView.SelectedTrack == track)
                    {
                        ImGui.GetWindowDrawList().AddLine(
                            ImGui.GetWindowPos(),
                            ImGui.GetWindowPos() + new Vector2(0, ImGui.GetWindowSize().Y),
                            ImGui.GetColorU32(Vector4.One), 5);
                    }

                    track.RenderControls();

                    ImGui.EndChild();
                    ImGui.PopStyleColor();

                    foreach (var clip in track.Clips)
                    {
                        if (TimeLineV2.IsPlaying() && TimeLineV2.GetCurrentTick() >= clip.StartTick && !clip.HasPlayed)
                        {
                            float timeOffset = (float)(TimeLineV2.TicksToSeconds(TimeLineV2.GetCurrentTick()) - TimeLineV2.TicksToSeconds(clip.StartTick));
                            if (clip is AudioClip audioClip && clip.Enabled)
                            {
                                clip.Play(audioClip.Clip.AudioFileReader, timeOffset, 0);
                            }
                            else if (clip is MidiClip midiClip && clip.Enabled)
                            {
                                clip.Play(midiClip.MidiClipData.MidiFile, timeOffset * TopBarControls.Bpm / 120f);
                            }
                            clip.HasPlayed = true;
                        }
                    }
                    trackIndex++;
                }

                ImGui.EndChild();
            }

            /* --------------------- Update TimeLine --------------------------*/
            TimeLineV2.UpdatePlayback();
            //float xOffset = windowPos.X + TimeLine.CurrentTime * Zoom - _arrangementScrollX;
            float xOffset = windowPos.X + TimeLineV2.TimeToPosition(TimeLineV2.GetCurrentTick()) - _arrangementScrollX;
            if (TimeLineV2.GetCurrentTick() > 0 && xOffset > windowPos.X && xOffset < windowPos.X + _arrangementWidth)
                ImGui.GetForegroundDrawList().AddLine(new(xOffset, windowPos.Y + menuBarHeight + 32), new(xOffset, windowPos.Y + windowSize.Y),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, 0.8f)));

            if (xOffset < windowPos.X + _arrangementWidth)
                ImGui.GetWindowDrawList().AddTriangleFilled(new Vector2(xOffset, windowPos.Y + menuBarHeight) - new Vector2(-6f, 0),
                    new Vector2(xOffset, windowPos.Y + menuBarHeight) - new Vector2(6f, 0),
                    new Vector2(xOffset, windowPos.Y + menuBarHeight) - new Vector2(0, -8f),
                    ImGui.GetColorU32(new Vector4(0.95f, 0.58f, 0.13f, 1f)));

            ImGui.EndChild();
        }
    }

    private static void RenderPopupMenu()
    {
        ImGui.PushStyleColor(ImGuiCol.Separator, Vector4.One);
        /*
        ImGui.SeparatorText("Adaptive Grid");
        if (ImGui.BeginTable("adaptive_grid_settings", 6, ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            int index = 1;
            foreach (var adaptiveGridOption in Enum.GetValues<AdaptiveGridOptions>())
            {
                if (ImGui.MenuItem(adaptiveGridOption.ToString(), string.Empty, AdaptiveGrid.GridOption == adaptiveGridOption))
                    AdaptiveGrid.SetGridOption(adaptiveGridOption);

                ImGui.TableSetColumnIndex(index);
                index++;
            }
            ImGui.EndTable();
        }
        */
        ImGui.SeparatorText("Fixed Grid");
        if (ImGui.BeginTable("fixed_grid_settings", 5, ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            if (ImGui.MenuItem("8 Bars", string.Empty, TimeLineV2.BeatsPerBar == 32))
                TimeLineV2.BeatsPerBar = 32;
            ImGui.TableSetColumnIndex(1);
            if (ImGui.MenuItem("4 Bars", "", TimeLineV2.BeatsPerBar == 16))
                TimeLineV2.BeatsPerBar = 16;
            ImGui.TableSetColumnIndex(2);
            if (ImGui.MenuItem("2 Bars", "", TimeLineV2.BeatsPerBar == 8))
                TimeLineV2.BeatsPerBar = 8;
            ImGui.TableSetColumnIndex(3);
            if (ImGui.MenuItem("1 Bar", "", TimeLineV2.BeatsPerBar == 4))
                TimeLineV2.BeatsPerBar = 4;
            ImGui.TableSetColumnIndex(4);
            if (ImGui.MenuItem("1/2", "", TimeLineV2.BeatsPerBar == 2))
                TimeLineV2.BeatsPerBar = 2;
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            if (ImGui.MenuItem("1/4", "", TimeLineV2.BeatsPerBar == 1))
                TimeLineV2.BeatsPerBar = 1;
            ImGui.TableSetColumnIndex(1);
            if (ImGui.MenuItem("1/8", "", TimeLineV2.BeatsPerBar == 1 / 2f))
                TimeLineV2.BeatsPerBar = 1 / 2f;
            ImGui.TableSetColumnIndex(2);
            if (ImGui.MenuItem("1/16", "", TimeLineV2.BeatsPerBar == 1 / 4f))
                TimeLineV2.BeatsPerBar = 1 / 4f;
            ImGui.TableSetColumnIndex(3);
            if (ImGui.MenuItem("1/32", "", TimeLineV2.BeatsPerBar == 1 / 8f))
                TimeLineV2.BeatsPerBar = 1 / 8f;
            ImGui.TableSetColumnIndex(4);
            if (ImGui.MenuItem("Off"))
                TimeLineV2.BeatsPerBar = 4;
            ImGui.EndTable();
        }
        ImGui.PopStyleColor();
    }

    private static Vector2 _windowPos;
    public static Vector2 WindowPos => _windowPos;

    private static float _arrangementScrollX;
    public static float ArrangementScroolX => _arrangementScrollX;

    private static float _arrangementScrollY;

    private static float _arrangementWidth;
    public static float ArrangementWidth => _arrangementWidth;

    private static float _maxClipLength;
    /// <summary>
    /// Length of the longest clip in the arrangement
    /// </summary>
    public static float MaxClipLength => _maxClipLength;
}
