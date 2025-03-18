using IconFonts;
using ImGuiNET;
using Lumix.Clips.AudioClips;
using Lumix.Clips.MidiClips;
using Lumix.EventArguments;
using Lumix.ImGuiExtensions;
using Lumix.Tracks;
using Lumix.Views;
using Lumix.Views.Arrangement;
using Lumix.Views.Sidebar;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
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

    private Vector4 _color;
    /// <summary>
    /// Clip color
    /// </summary>
    public Vector4 Color { get => _color; set { _color = value; } }

    #region Clip time properties

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

    // Backing fields for markers
    private MusicalTime _startMarker;
    private MusicalTime _endMarker;

    /// <summary>
    /// Marker at which clip will start playing
    /// </summary>
    public MusicalTime StartMarker
    {
        get => _startMarker;
        protected set
        {
            if (_startMarker != value)
            {
                StartMarkerChanged?.Invoke(this, new EventArguments.TimeChangedEventArgs(_startMarker, value));
                _startMarker = value;
            }
        }
    }

    /// <summary>
    /// Marker at which clip will stop playing
    /// </summary>
    public MusicalTime EndMarker
    {
        get => _endMarker;
        protected set
        {
            if (_endMarker != value)
            {
                EndMarkerChanged?.Invoke(this, new EventArguments.TimeChangedEventArgs(_endMarker, value));
                _endMarker = value;
            }
        }
    }

    public event EventHandler<EventArguments.TimeChangedEventArgs> StartMarkerChanged;
    public event EventHandler<EventArguments.TimeChangedEventArgs> EndMarkerChanged;

    #endregion

    #region Flag properties

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

    /// <summary>
    /// Flag used to determine if this clip has been fired during current playback
    /// </summary>
    public bool HasPlayed { get; set; }

    // Resizing flags
    private bool _isLeftResizing;
    private bool _isRightResizing;
    private float _resizeMouseStartX;

    #endregion

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
        return  TimeLine.TicksToMusicalTime(StartTick + TimeLine.MusicalTimeToTicks(StartMarker), true);
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
        return TimeLine.TicksToMusicalTime(DurationTicks - TimeLine.MusicalTimeToTicks(StartMarker - EndMarker));
    }

    /// <summary>
    /// Get duration of clip in seconds
    /// </summary>
    /// <returns></returns>
    public double GetDurationInSeconds()
    {
        return TimeLine.TicksToSeconds(DurationTicks - TimeLine.MusicalTimeToTicks(StartMarker - EndMarker));
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
    /*
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
    */
    public void Render()
    {
        UpdateTimesData();
        ClipWidth = GetClipWidth();

        Vector2 mousePos = ImGui.GetMousePos();

        ImGui.SetCursorPosX(TimeLine.TimeToPosition(StartTick));
        ImGui.SetCursorPosY(Track.TrackTopPos);

        bool selected = ArrangementView.SelectedClips.Contains(this);
        Vector4 backgroundCol = selected ? ImGuiTheme.SelectionCol : Color;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, selected ? backgroundCol * 0.6f : Enabled ? backgroundCol * 0.6f : Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.MenuBarBg, Enabled ? Color : Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.Border, Color);
        if (ImGui.BeginChild(Id, new(ClipWidth, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border, ImGuiWindowFlags.MenuBar))
        {           
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

            // Show clip info in Info Box
            InfoBox.SetInfoData(_name, $"Start: {StartMusicalTime.AsString()}\n" +
                $"End: {StartMusicalTime.Bars + DurationMusicalTime.Bars}.{StartMusicalTime.Beats + DurationMusicalTime.Beats}.{StartMusicalTime.Ticks + DurationMusicalTime.Ticks}\n" +
                $"Length: {DurationMusicalTime.AsString()}\n" +
                $"Duration: {GetDurationInSeconds():n3}s\n\n" +
                $"Marker (Start): {StartMarker.AsString()}\n" +
                $"Marker (End): {EndMarker.AsString()}",
                isHoveringMenuBar || this.Track.DraggedClip == this);

            // Is hovering top-left corner (not implemented yet)
            bool resizeHovered = false;

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
                Track.SetDragStartOffset(mousePos.X - TimeLine.TimeToPosition(StartTick) - ArrangementView.WindowPos.X);
                //Track.SetDragStartOffset(mousePos.X - TimeLinePosition * ArrangementView.Zoom - ArrangementView.WindowPos.X);
            }

            // If dragging, update the clip's position
            if (Track.DraggedClip == this && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                long newTime = TimeLine.SnapToGrid(TimeLine.PositionToTime(mousePos.X - ArrangementView.WindowPos.X - Track.DragStartOffsetX));
                StartTick = Math.Clamp(newTime, 0, long.MaxValue);
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
        if (ImGui.MenuItem("Split", "Ctrl+E", false, !TimeLine.IsPlaying()))
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
