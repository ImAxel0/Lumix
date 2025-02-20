using IconFonts;
using ImGuiNET;
using Lumix.Clips.AudioClips;
using Lumix.Clips.MidiClips;
using Lumix.ImGuiExtensions;
using Lumix.Tracks;
using Lumix.Views.Arrangement;
using Lumix.Views.Sidebar;
using Melanchall.DryWetMidi.Core;
using NAudio.Wave;
using System.Numerics;

namespace Lumix.Clips;

public abstract class Clip
{
    public string Id { get; } = Guid.NewGuid().ToString();

    private string _name = string.Empty;
    public string Name { get => _name; protected set { _name = value; } }

    /// <summary>
    /// Clip state
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The track of this clip
    /// </summary>
    public Track Track { get; protected set; }

    /// <summary>
    /// Clip width in pixels
    /// </summary>
    public float ClipWidth { get; protected set; }

    #region Time properties

    /// <summary>
    /// Arrangement clip starting time in ticks
    /// </summary>
    public long StartTick { get; protected set; }

    /// <summary>
    /// Arrangement clip ending time in ticks
    /// </summary>
    public long EndTick { get; protected set; }

    /// <summary>
    /// Duration of the clip in ticks
    /// </summary>
    public long DurationTicks { get; protected set; }

    /// <summary>
    /// Arrangement clip starting time in Musical Time
    /// </summary>
    public MusicalTime StartMusicalTime { get; protected set; }

    /// <summary>
    /// Arrangement clip ending time in MusicalTime
    /// </summary>
    public MusicalTime EndMusicalTime { get; protected set; }

    /// <summary>
    /// Duration of the clip in Musical Time
    /// </summary>
    public MusicalTime DurationMusicalTime { get; protected set; }
    
    #endregion

    /// <summary>
    /// Flag used to move clip between tracks
    /// </summary>
    public bool WantsToMove { get; protected set; }

    /// <summary>
    /// Flag used to determine if this clip is hovered (menubar included)
    /// </summary>
    public bool ClipIsHovered { get; protected set; }

    /// <summary>
    /// Flag used to determine if this clip menubar only is hovered
    /// </summary>
    public bool MenuBarIsHovered { get; protected set; }

    public bool HasPlayed { get; set; }

    private Vector4 _color;
    /// <summary>
    /// Clip color
    /// </summary>
    public Vector4 Color { get => _color; protected set { _color = value; } }

    /// <summary>
    /// Cutted portion in ticks from start of the clip
    /// </summary>
    public long StartOffset { get; protected set; }

    /// <summary>
    /// Cutted portion in ticks from end of the clip
    /// </summary>
    public long EndOffset { get; protected set; }

    // Resizing flags
    private bool _isLeftResizing;
    private bool _isRightResizing;
    private float _resizeMouseStartX;

    public void Play(AudioFileReader audioFile, float offset, float endOffset)
    {
        Track.Engine.Fire(audioFile, offset, endOffset);
    }

    public void Play(MidiFile midiFile, float offset)
    {
        Track.Engine.Fire(midiFile, offset, offset);
    }

    /// <summary>
    /// Set start time of the clip in ticks
    /// </summary>
    /// <param name="tick"></param>
    public void SetStartTick(long tick)
    {
        StartTick = tick;
    }

    /// <summary>
    /// Get start time of clip in Bars:Beats:Ticks
    /// </summary>
    /// <returns></returns>
    public MusicalTime GetStartTimeInMusicalTime()
    {
        return TimeLineV2.TicksToMusicalTime(StartTick + StartOffset, true);
    }

    /// <summary>
    /// Get end time of clip in Bars:Beats:Ticks
    /// </summary>
    /// <returns></returns>
    public MusicalTime GetEndTimeInMusicalTime()
    {
        var start = GetStartTimeInMusicalTime();
        var duration = GetDurationInMusicalTime();
        return new MusicalTime(start.Bars + duration.Bars, start.Beats + duration.Beats, start.Ticks + duration.Ticks);
    }

    /// <summary>
    /// Get duration of clip in Bars:Beats:Ticks
    /// </summary>
    /// <returns></returns>
    public MusicalTime GetDurationInMusicalTime()
    {
        return TimeLineV2.TicksToMusicalTime(DurationTicks - StartOffset - EndOffset);
    }

    /// <summary>
    /// Get duration of clip in seconds
    /// </summary>
    /// <returns></returns>
    public double GetDurationInSeconds()
    {
        return TimeLineV2.TicksToSeconds(DurationTicks - StartOffset - EndOffset);
    }

    /// <summary>
    /// Get clip duration in ticks
    /// </summary>
    /// <returns></returns>
    protected abstract long GetClipDuration();

    /// <summary>
    /// Get clip width in pixels
    /// </summary>
    /// <returns></returns>
    protected abstract float GetClipWidth();

    /// <summary>
    /// Render inner content of clip
    /// </summary>
    protected abstract void RenderClipContent(float menuBarHeight, float clipHeight);

    protected abstract void RenderClipContent(Vector2 pos, float width, float height);

    protected abstract void OnClipDoubleClickLeft();

    private void UpdateTimesData()
    {
        EndTick = StartTick + GetClipDuration();
        DurationTicks = GetClipDuration();
        StartMusicalTime = GetStartTimeInMusicalTime();
        EndMusicalTime = GetEndTimeInMusicalTime();
        DurationMusicalTime = GetDurationInMusicalTime();
    }

    public void Render2()
    {
        UpdateTimesData();

        var drawList = ImGui.GetWindowDrawList();
        Vector2 mousePos = ImGui.GetMousePos();

        // Unprocessed positions
        float xStart = ArrangementView.WindowPos.X + TimeLineV2.TimeToPosition(StartTick) - ArrangementView.ArrangementScroolX;
        float xEnd = ArrangementView.WindowPos.X + TimeLineV2.TimeToPosition(EndTick);

        // Apply processing offsets
        float xStartProc = xStart + TimeLineV2.TicksToPixels(StartOffset);
        float xEndProc = xEnd - TimeLineV2.TicksToPixels(EndOffset);

        Vector2 rectStart = new(xStartProc, ImGui.GetWindowPos().Y);
        Vector2 rectEnd = new(xEndProc, ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y);

        drawList.AddRectFilled(rectStart, rectEnd, ImGui.GetColorU32(Color), 4);
        drawList.AddRect(rectStart, rectEnd, ImGui.GetColorU32(Vector4.One), 4);

        var startOffBBT = TimeLineV2.TicksToMusicalTime(StartOffset);
        var endOffBBT = TimeLineV2.TicksToMusicalTime(EndOffset);
        UiElement.Tooltip($"Start: {StartMusicalTime.Bars}:{StartMusicalTime.Beats}:{StartMusicalTime.Ticks}\n" +
            $"End: {StartMusicalTime.Bars + DurationMusicalTime.Bars}:{StartMusicalTime.Beats + DurationMusicalTime.Beats}:{StartMusicalTime.Ticks + DurationMusicalTime.Ticks}\n" +
            $"Length: {DurationMusicalTime.Bars}:{DurationMusicalTime.Beats}:{DurationMusicalTime.Ticks}\n" +
            $"Duration: {GetDurationInSeconds():n3}\n" +
            $"Start ofs: {startOffBBT.Bars}:{startOffBBT.Beats}:{startOffBBT.Ticks}\n" +
            $"End ofs: {endOffBBT.Bars}:{endOffBBT.Beats}:{endOffBBT.Ticks}");

        float menuBarHeight = 25f;
        drawList.AddRectFilled(rectStart, new(rectEnd.X, rectStart.Y + menuBarHeight), ImGui.GetColorU32(Color * 0.8f), 4, ImDrawFlags.RoundCornersTop);
        bool isHoveringMenuBar = ImGui.IsMouseHoveringRect(rectStart, new(rectEnd.X, rectStart.Y + menuBarHeight));
        MenuBarIsHovered = isHoveringMenuBar;
        bool isClicked = isHoveringMenuBar && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        bool wasDoubleClicked = false;
        if (isHoveringMenuBar && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            OnClipDoubleClickLeft();
            wasDoubleClicked = true;
        }

        RenderClipContent(new(xStart, rectStart.Y + menuBarHeight), xEnd - xStart, rectEnd.Y - rectStart.Y - menuBarHeight);

        if (isHoveringMenuBar && !_isLeftResizing && !_isRightResizing)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.None);
            ImGui.GetForegroundDrawList().AddText(ImGui.GetMousePos() - Vector2.One - ImGui.CalcTextSize(FontAwesome6.Hand) / 2,
                ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), FontAwesome6.Hand);
            ImGui.GetForegroundDrawList().AddText(ImGui.GetMousePos() + Vector2.One - ImGui.CalcTextSize(FontAwesome6.Hand) / 2,
                ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), FontAwesome6.Hand);
            ImGui.GetForegroundDrawList().AddText(ImGui.GetMousePos() - ImGui.CalcTextSize(FontAwesome6.Hand) / 2,
                ImGui.GetColorU32(Vector4.One), FontAwesome6.Hand);
        }

        if (isClicked && Track.DraggedClip == null && !_isLeftResizing && !_isRightResizing)
        {
            Track.SetDraggedClip(this);
            Track.SetDragStartOffset(mousePos.X - TimeLineV2.TimeToPosition(StartTick) - ArrangementView.WindowPos.X);
        }

        // If dragging, update the clip's position
        if (Track.DraggedClip == this && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            long newTime = TimeLineV2.SnapToGrid(TimeLineV2.PositionToTime(mousePos.X - ArrangementView.WindowPos.X - Track.DragStartOffsetX));
            StartTick = Math.Clamp(newTime, 0, long.MaxValue);
        }

        // Stop dragging when the mouse button is released
        if (Track.DraggedClip != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left)) // _audioTrack.DraggedClip == _clip
        {
            Track.SetDraggedClip(null);
        }
    }

    public void Render()
    {
        UpdateTimesData();
        ClipWidth = GetClipWidth();

        /* I secondi a cui si trova la clip non cambiano con il livello di ZOOM, ma tengono conto dei BPM */
        //Time = TimeLinePosition / TopBarControls.Bpm + StartOffset /* / ArrangementView.Zoom */;
        //TimeLinePosition = TimeLineV2.TimeToPosition(StartTick);

        Vector2 mousePos = ImGui.GetMousePos();

        ImGui.SetCursorPosX(TimeLineV2.TimeToPosition(StartTick));
        ImGui.SetCursorPosY(Track.TrackTopPos);

        bool selected = ArrangementView.SelectedClips.Contains(this);
        Vector4 backgroundCol = selected ? new Vector4(0.55f, 0.79f, 0.85f, 1f) : Color;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, selected ? backgroundCol * 0.6f : Enabled ? backgroundCol * 0.6f : Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.MenuBarBg, Enabled ? Color : Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, Color);
        if (ImGui.BeginChild(Id, new(ClipWidth, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border, ImGuiWindowFlags.MenuBar))
        {
            var startOffBBT = TimeLineV2.TicksToMusicalTime(StartOffset);
            var endOffBBT = TimeLineV2.TicksToMusicalTime(EndOffset);
            UiElement.Tooltip($"Start: {StartMusicalTime.Bars}:{StartMusicalTime.Beats}:{StartMusicalTime.Ticks}\n" +
                $"End: {StartMusicalTime.Bars + DurationMusicalTime.Bars}:{StartMusicalTime.Beats + DurationMusicalTime.Beats}:{StartMusicalTime.Ticks + DurationMusicalTime.Ticks}\n" +
                $"Length: {DurationMusicalTime.Bars}:{DurationMusicalTime.Beats}:{DurationMusicalTime.Ticks}\n" +
                $"Duration: {GetDurationInSeconds():n3}\n" +
                $"Start ofs: {startOffBBT.Bars}:{startOffBBT.Beats}:{startOffBBT.Ticks}\n" +
                $"End ofs: {endOffBBT.Bars}:{endOffBBT.Beats}:{endOffBBT.Ticks}");

            var drawList = ImGui.GetWindowDrawList();
            Vector2 clipStart = ImGui.GetWindowPos();
            Vector2 clipEnd = ImGui.GetWindowPos() + ImGui.GetWindowSize();
            Vector2 clipSize = ImGui.GetWindowSize();
            uint trimmedColor = ImGui.GetColorU32(new Vector4(Color.X * 0.3f, Color.Y * 0.3f, Color.Z * 0.3f, 1));
            //drawList.AddRectFilled(clipStart, new Vector2(clipStart.X + StartTicksOffset * TopBarControls.Bpm * ArrangementView.Zoom, clipEnd.Y), trimmedColor);
            //drawList.AddRectFilled(new Vector2(clipEnd.X - EndTicksOffset * TopBarControls.Bpm * ArrangementView.Zoom, clipStart.Y), clipEnd, trimmedColor);

            ClipIsHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);
            if (ImGui.BeginPopupContextWindow("clip_popup", ImGuiPopupFlags.MouseButtonRight))
            {
                RenderPopupMenu();
                ImGui.EndPopup();
            }

            ImGui.BeginMenuBar();

            float menuBarHeight = ImGui.GetFrameHeightWithSpacing();
            bool isHoveringMenuBar = ImGui.IsMouseHoveringRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + new Vector2(ClipWidth, menuBarHeight));//ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);
            MenuBarIsHovered = isHoveringMenuBar;
            bool isClicked = isHoveringMenuBar && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

            // Is hovering top-left corner
            bool resizeHovered = false;
            /*
            float resizeGrabStart = clipStart.X + StartOffset * TopBarControls.Bpm * ArrangementView.Zoom;
            if (ImGui.IsMouseHoveringRect(new Vector2(resizeGrabStart, clipStart.Y), new Vector2(resizeGrabStart + 15, clipStart.Y + menuBarHeight)) 
                && Track.DraggedClip == null || _isLeftResizing)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left, false))
                {
                    _resizeMouseStartX = ImGui.GetMousePos().X - ArrangementView.WindowPos.X;
                    _isLeftResizing = true;
                }

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    _isLeftResizing = false;

                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    float gridSpacingWidth = AdaptiveGrid.CalculateGridSpacing(ArrangementView.Zoom, AdaptiveGrid.MinSpacing, AdaptiveGrid.MaxSpacing);
                    float gridSpacingTime = gridSpacingWidth / ArrangementView.Zoom / TopBarControls.Bpm;

                    if (mousePos.X - ArrangementView.WindowPos.X > _resizeMouseStartX + (gridSpacingWidth / 2))
                    {
                        StartOffset = Math.Clamp(StartOffset + gridSpacingTime, 0, GetClipDuration() - EndOffset - gridSpacingWidth / ArrangementView.Zoom / TopBarControls.Bpm);
                        if (StartOffset > 0)
                        {
                            _resizeMouseStartX += gridSpacingWidth;
                        }
                        else
                            _resizeMouseStartX = clipStart.X - ArrangementView.WindowPos.X;
                    }
                    else if (mousePos.X - ArrangementView.WindowPos.X < _resizeMouseStartX - (gridSpacingWidth / 2))
                    {
                        StartOffset = Math.Clamp(StartOffset - gridSpacingTime, 0, GetClipDuration() - EndOffset - gridSpacingWidth / ArrangementView.Zoom / TopBarControls.Bpm);
                        if (StartOffset > 0)
                        {
                            _resizeMouseStartX -= gridSpacingWidth;
                        }
                        else
                            _resizeMouseStartX = clipStart.X - ArrangementView.WindowPos.X; ;
                    }
                }
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                resizeHovered = true;
            }
            //ImGui.GetForegroundDrawList().AddRectFilled(new Vector2(resizeGrabStart, clipStart.Y), new Vector2(resizeGrabStart + 15, clipStart.Y + menuBarHeight)
            //, ImGui.GetColorU32(Vector4.One));

            // Is hovering top-right corner
            float resizeGrabEnd = EndOffset * TopBarControls.Bpm * ArrangementView.Zoom;
            if (ImGui.IsMouseHoveringRect(new Vector2(clipStart.X + clipSize.X - 15 - resizeGrabEnd, clipStart.Y), new Vector2(clipStart.X + clipSize.X - resizeGrabEnd, clipStart.Y + menuBarHeight)) 
                && Track.DraggedClip == null || _isRightResizing)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left, false))
                {
                    _resizeMouseStartX = ImGui.GetMousePos().X - ArrangementView.WindowPos.X;
                    _isRightResizing = true;
                }

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    _isRightResizing = false;

                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    float gridSpacingWidth = AdaptiveGrid.CalculateGridSpacing(ArrangementView.Zoom, AdaptiveGrid.MinSpacing, AdaptiveGrid.MaxSpacing);
                    float gridSpacingTime = gridSpacingWidth / ArrangementView.Zoom / TopBarControls.Bpm;

                    if (mousePos.X - ArrangementView.WindowPos.X < _resizeMouseStartX - (gridSpacingWidth / 2))
                    {
                        EndOffset = Math.Clamp(EndOffset + gridSpacingTime, 0, GetClipDuration() - StartOffset - gridSpacingWidth / ArrangementView.Zoom / TopBarControls.Bpm);
                        if (EndOffset > 0)
                        {
                            _resizeMouseStartX -= gridSpacingWidth;
                        }
                        else
                            _resizeMouseStartX = clipStart.X + clipSize.X - ArrangementView.WindowPos.X;
                    }
                    else if (mousePos.X - ArrangementView.WindowPos.X > _resizeMouseStartX + (gridSpacingWidth / 2))
                    {
                        EndOffset = Math.Clamp(EndOffset - gridSpacingTime, 0, GetClipDuration() - StartOffset - gridSpacingWidth / ArrangementView.Zoom / TopBarControls.Bpm);
                        if (EndOffset > 0)
                        {
                            _resizeMouseStartX += gridSpacingWidth;
                        }
                        else
                            _resizeMouseStartX = clipStart.X + clipSize.X - ArrangementView.WindowPos.X; ;
                    }
                }
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                resizeHovered = true;
            }
            //ImGui.GetForegroundDrawList().AddRectFilled(new Vector2(clipStart.X + clipSize.X - 15 - resizeGrabEnd, clipStart.Y), new Vector2(clipStart.X + clipSize.X - resizeGrabEnd, clipStart.Y + menuBarHeight)
                //, ImGui.GetColorU32(Vector4.One));
            */

            if (isHoveringMenuBar && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right)))
            {
                if (!ArrangementView.SelectedClips.Contains(this))
                {
                    ArrangementView.SelectedClips.Add(this);
                }
            }

            bool wasDoubleClicked = false;
            if (isHoveringMenuBar && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                OnClipDoubleClickLeft();
                wasDoubleClicked = true;
            }

            if (isClicked)
            {
                WantsToMove = true;
            }
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                WantsToMove = false;

            if (!isHoveringMenuBar && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right)) && !ImGui.IsKeyDown(ImGuiKey.ModShift))
                if (ArrangementView.SelectedClips.Contains(this))
                    ArrangementView.SelectedClips.Remove(this);

            if (WantsToMove && !Track.TrackHasCursor)
            {
                // moving clip between tracks
                if (ImGui.BeginDragDropSource())
                {
                    DeleteRequested = true;
                    if (this is AudioClip audioClip)
                    {
                        SidebarView.DraggedFilePath = audioClip.Clip.AudioFileReader.FileName;
                    }

                    if (this is MidiClip midiClip)
                    {
                        midiClip.MidiClipData.MidiFile.Write("dragged_midi_clip.mid", true);
                        SidebarView.DraggedFilePath = "dragged_midi_clip.mid"; // NEED TO DELETE THE TEMP MIDI FILE AFTER BEING MOVED
                    }
                    ImGui.SetDragDropPayload("CLIP", IntPtr.Zero, 0); // Set a payload for the drag event
                    ImGui.EndDragDropSource();
                }
            }

            ImGui.PushStyleColor(ImGuiCol.FrameBg, Vector4.Zero);
            if (_renameRequested)
            {
                if (ImGui.InputText("##name", ref _name, 1000, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue)
                    || ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (string.IsNullOrWhiteSpace(_name))
                    {
                        _name = "Blank";
                    }
                    _renameRequested = false;
                }
                ImGui.SetKeyboardFocusHere(-1);
            }
            else
            {
                var textCol = Enabled ? new Vector4(0, 0, 0, 1) : Vector4.One;
                ImGui.TextColored(textCol, _name);
            }
            ImGui.PopStyleColor();
            //ImGui.TextColored(new Vector4(0, 0, 0, 1), $"{_time}s");
            ImGui.EndMenuBar();

            float clipHeight = ImGui.GetContentRegionAvail().Y;
            RenderClipContent(menuBarHeight, clipHeight);

            if (isHoveringMenuBar && !resizeHovered && !_isLeftResizing && !_isRightResizing)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.None);
                ImGui.GetForegroundDrawList().AddText(ImGui.GetMousePos() - Vector2.One - ImGui.CalcTextSize(FontAwesome6.Hand) / 2,
                    ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), FontAwesome6.Hand);
                ImGui.GetForegroundDrawList().AddText(ImGui.GetMousePos() + Vector2.One - ImGui.CalcTextSize(FontAwesome6.Hand) / 2,
                    ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), FontAwesome6.Hand);
                ImGui.GetForegroundDrawList().AddText(ImGui.GetMousePos() - ImGui.CalcTextSize(FontAwesome6.Hand) / 2,
                    ImGui.GetColorU32(Vector4.One), FontAwesome6.Hand);
            }

            if (isClicked && Track.DraggedClip == null && !wasDoubleClicked && !resizeHovered && !_isLeftResizing && !_isRightResizing)
            {
                Track.SetDraggedClip(this);
                Track.SetDragStartOffset(mousePos.X - TimeLineV2.TimeToPosition(StartTick) - ArrangementView.WindowPos.X);
                //Track.SetDragStartOffset(mousePos.X - TimeLinePosition * ArrangementView.Zoom - ArrangementView.WindowPos.X);
            }

            // If dragging, update the clip's position
            if (Track.DraggedClip == this && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                long newTime = TimeLineV2.SnapToGrid(TimeLineV2.PositionToTime(mousePos.X - ArrangementView.WindowPos.X - Track.DragStartOffsetX));
                //long newTime = (TimeLineV2.PositionToTime((mousePos.X - ArrangementView.WindowPos.X - Track.DragStartOffsetX) / gridSpacing) * gridSpacing);
                StartTick = Math.Clamp(newTime, 0, long.MaxValue);

                //float newPos = (ImGui.GetMousePos().X - ArrangementView.WindowPos.X - Track.DragStartOffsetX) / ArrangementView.Zoom;
                //float snappedPosition = AdaptiveGrid.GetSnappedPosition(newPos);
                //TimeLinePosition = Math.Clamp(snappedPosition, 0, float.PositiveInfinity);
                /*
                float newPos = ImGui.GetMousePos().X - ArrangementView.WindowPos.X - Track.DragStartOffsetX;
                newPos /= ArrangementView.Zoom;
                float stepLength = 120 * ArrangementView.BeatsPerBar * 2;
                float snappedPosition = MathF.Round(newPos / stepLength) * stepLength;
                TimeLinePosition = Math.Clamp(snappedPosition, 0, float.PositiveInfinity);
                */
            }

            // Request clip copy
            if (Track.DraggedClip == this && ImGui.IsKeyDown(ImGuiKey.ModCtrl))
            {
                if (ImGui.IsKeyPressed(ImGuiKey.ModCtrl, false) || ImGui.IsMouseClicked(ImGuiMouseButton.Left, false))
                {
                    // Make a copy
                    DuplicateRequested = true;
                }
            }

            // Stop dragging when the mouse button is released
            if (Track.DraggedClip != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left)) // _audioTrack.DraggedClip == _clip
            {
                Track.SetDraggedClip(null);
            }
        }
        ImGui.EndChild(); // placed outside else gui breaks when scrolling
        ImGui.PopStyleColor(3);
    }

    protected virtual void RenderPopupMenu()
    {
        ImGui.PushStyleColor(ImGuiCol.Separator, Vector4.One);
        if (ImGui.MenuItem("Cut", "Ctrl+X"))
        {

        }
        if (ImGui.MenuItem("Copy", "Ctrl+C"))
        {

        }
        if (ImGui.MenuItem("Duplicate", "Ctrl+D"))
        {
            DuplicateRequested = true;
        }
        if (ImGui.MenuItem("Delete", "Del"))
        {
            DeleteRequested = true;
        }
        if (ImGui.MenuItem("Split", "Ctrl+E", false, !TimeLine.IsRunning))
        {

        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.MenuItem("Rename", "Ctrl+R"))
        {
            _renameRequested = true;
        }
        string state = Enabled ? "Deactivate Clip" : "Activate Clip";
        if (ImGui.MenuItem(state, "0"))
        {
            Enabled = !Enabled;
        }
        ImGui.ColorEdit4("Clip Color", ref _color, ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoInputs);
        ImGui.PopStyleColor();
    }

    private bool _renameRequested;
    public bool DeleteRequested { get; set; }
    public bool DuplicateRequested { get; set; }
}
