using Lumix.Plugins.VST;
using Lumix.Plugins;
using ImGuiNET;
using System.Numerics;
using Vanara.PInvoke;
using IconFonts;
using Lumix.Views;
using Lumix.Clips;
using Lumix.Clips.AudioClips;
using Lumix.Clips.MidiClips;
using Lumix.Tracks.AudioTracks;
using Lumix.Tracks.MidiTracks;
using Lumix.Views.Sidebar;
using Lumix.Views.Arrangement;
using Lumix.ImGuiExtensions;

namespace Lumix.Tracks;

public enum TrackType
{
    Audio,
    Midi
}

public abstract class Track
{
    public abstract TrackType TrackType { get; }

    public TrackEngine Engine { get; protected set; }
    public string Id { get; } = Guid.NewGuid().ToString();

    private string _name = string.Empty;
    public string Name { get => _name; protected set { _name = value; } }

    private bool _enabled = true;
    public bool Enabled { get => _enabled; private set { _enabled = value; } }

    private bool _solo;
    public bool Solo { get => _solo; private set { _solo = value; } }

    private bool _recordOnStart;
    public bool RecordOnStart { get => _recordOnStart; private set { _recordOnStart = value; } }

    private float _volume;
    public float Volume { get => _volume; private set { _volume = value; } }

    private float _pan;
    public float Pan { get => _pan; private set { _pan = value; } }

    private Vector4 _color;
    public Vector4 Color { get => _color; protected set { _color = value; } }

    /// <summary>
    /// The clips of this track
    /// </summary>
    public List<Clip> Clips = new();

    protected float _leftChannelGain;
    protected float _rightChannelGain;
    protected float _smoothLeftChannelGain;
    protected float _smoothRightChannelGain;

    public float TrackTopPos { get; private set; }
    public float DragStartOffsetX { get; private set; } // Store the offset when dragging starts (for clips)
    public bool TrackHasCursor { get; private set; }

    private Clip? _tmpClip;

    private Clip? _draggedClip = null; // Track the currently dragged clip
    public Clip DraggedClip => _draggedClip;

    protected abstract void OnDoubleClickLeft();

    public void SetDraggedClip(Clip? draggedClip)
    {
        _draggedClip = draggedClip;
    }

    public void SetDragStartOffset(float dragStartOffsetX)
    {
        DragStartOffsetX = dragStartOffsetX;
    }

    public void RenderArrangement()
    {
        TrackTopPos = ImGui.GetCursorPosY();
        TrackHasCursor = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

        // REMEMBER TO CHANGE THIS IMPLEMENTATION
        var vstPlugin = Engine.PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
        vstPlugin?.PluginWindow.PumpEvents();
        Engine.PluginChainSampleProvider.FxPlugins.ForEach(plugin =>
        {
            var vstPlugin = plugin.GetPlugin<VstPlugin>();
            vstPlugin?.PluginWindow.PumpEvents();
        });

        float windowPosX = ImGui.GetWindowPos().X;

        Vector2 pos = ImGui.GetCursorPos();
        ImGui.Dummy(ImGui.GetContentRegionAvail());

        if (TrackHasCursor && ImGui.IsMouseClicked(ImGuiMouseButton.Left, false))
        {
            DevicesView.SelectedTrack = this;
        }

        if (!TrackHasCursor && _tmpClip != null)
            _tmpClip = null;

        if (TrackHasCursor && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            OnDoubleClickLeft();
        }

        if (ImGui.BeginDragDropTarget())
        {
            ImGui.AcceptDragDropPayload("CLIP");
            ImGui.AcceptDragDropPayload("PLUGIN_DRAG");
            ImGui.AcceptDragDropPayload("BUILTIN_PLUGIN_DRAG");

            if (!string.IsNullOrEmpty(SidebarView.DraggedFilePath) || SidebarView.DraggedBuiltInPlugin != null && !_hasDropped)
            {
                if (SidebarView.DraggedBuiltInPlugin != null) // Is built in plugin
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        var pluginInstance = SidebarView.DraggedBuiltInPlugin as IAudioProcessor;
                        Engine.PluginChainSampleProvider.AddPlugin(pluginInstance);
                        SidebarView.DraggedBuiltInPlugin = null;
                        _hasDropped = true;

                        // Switch to devices view and select drop targeted track
                        DevicesView.SelectedTrack = this;
                        BottomView.RenderedWindow = BottomViewWindows.DevicesView;
                    }
                }
                else if (Path.GetExtension(SidebarView.DraggedFilePath) == ".dll") // Is external vst plugin
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        var vst = new VstPlugin(SidebarView.DraggedFilePath);
                        var vstProcessor = new VstAudioProcessor(vst);
                        if (TrackType == TrackType.Audio)
                        {
                            if (vst.PluginType != VstType.VSTi)
                            {
                                Engine.PluginChainSampleProvider.AddPlugin(vstProcessor);
                            }
                            else
                            {
                                vst.Dispose();
                                User32.MessageBox(IntPtr.Zero,
                                    "Can't add vst instrument to audio track",
                                    "Error",
                                    User32.MB_FLAGS.MB_ICONERROR | User32.MB_FLAGS.MB_TOPMOST);
                            }
                        }
                        else if (TrackType == TrackType.Midi)
                        {
                            Engine.PluginChainSampleProvider.AddPlugin(vstProcessor);
                        }

                        // Switch to devices view and select drop targeted track
                        DevicesView.SelectedTrack = this;
                        BottomView.RenderedWindow = BottomViewWindows.DevicesView;
                        SidebarView.DraggedFilePath = string.Empty;
                        _hasDropped = true;
                    }
                }
                else if (Path.HasExtension(SidebarView.DraggedFilePath)) // change to proper check later
                {
                    bool allowed = false;
                    if (TrackType == TrackType.Audio)
                    {
                        if (Path.GetExtension(SidebarView.DraggedFilePath) != ".mid")
                        {
                            _tmpClip ??= new AudioClip(this as AudioTrack, new AudioClipData(SidebarView.DraggedFilePath), TimeLineV2.PositionToTime(ImGui.GetMousePos().X - windowPosX));
                            allowed = true;
                        }
                        else
                        {
                            ImGui.SetMouseCursor(ImGuiMouseCursor.NotAllowed);
                            allowed = false;
                        }
                    }
                    else if (TrackType == TrackType.Midi)
                    {
                        if (Path.GetExtension(SidebarView.DraggedFilePath) == ".mid")
                        {
                            _tmpClip ??= new MidiClip(this as MidiTrack, SidebarView.DraggedFilePath, TimeLineV2.PositionToTime(ImGui.GetMousePos().X - windowPosX));
                            allowed = true;
                        }
                        else
                        {
                            ImGui.SetMouseCursor(ImGuiMouseCursor.NotAllowed);
                            allowed = false;
                        }
                    }

                    long newTime = TimeLineV2.SnapToGrid(TimeLineV2.PositionToTime(ImGui.GetMousePos().X - windowPosX));
                    _tmpClip?.SetStartTick(newTime);
                    _tmpClip?.Render();

                    /*
                    float newClipPos = (ImGui.GetMousePos().X - windowPosX) / ArrangementView.Zoom;
                    float snappedPosition = AdaptiveGrid.GetSnappedPosition(newClipPos);
                    _tmpClip?.ChangePos(Math.Clamp(snappedPosition, 0, float.PositiveInfinity));
                    _tmpClip?.Render();
                    */
                    /*
                    float newClipPos = ImGui.GetMousePos().X - windowPosX;
                    newClipPos /= ArrangementView.Zoom;
                    float stepLength = 120 * ArrangementView.BeatsPerBar * 2;
                    float snappedPosition = MathF.Round(newClipPos / stepLength) * stepLength;
                    _tmpClip?.ChangePos(Math.Clamp(snappedPosition, 0, float.PositiveInfinity));
                    _tmpClip?.Render();
                    */

                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && allowed)
                    {
                        // Add the dragged clip to this track
                        Clip clip = _tmpClip;
                        /*
                        if (TrackType == TrackType.Audio)
                        {
                            clip = new AudioClip(this as AudioTrack, new AudioClipData(Sidebar.DraggedFilePath), snappedPosition);
                        }
                        else if (TrackType == TrackType.Midi)
                        {
                            clip = new MidiClip(this as MidiTrack, Sidebar.DraggedFilePath, snappedPosition);
                        }
                        */
                        Clips.Add(clip);
                        SidebarView.DraggedFilePath = string.Empty; // Reset dragging state

                        _tmpClip = null;
                        _hasDropped = true; // Set flag to prevent adding multiple times

                        // Switch to devices view and select drop targeted track
                        DevicesView.SelectedTrack = this;
                        BottomView.RenderedWindow = BottomViewWindows.DevicesView;
                    }
                }
                else
                    ImGui.SetMouseCursor(ImGuiMouseCursor.NotAllowed);
            }
            ImGui.EndDragDropTarget();

            // Reset the drop flag if the mouse is no longer dragging anything
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _hasDropped = false; // Reset drop flag after mouse drag ends
            }
        }
        ImGui.SetCursorPos(pos);
        /*
        uint gridColorSecondary = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.3f)); // Minor grid lines

        Vector2 position = ImGui.GetWindowPos();
        float startX = position.X;
        float endX = startX + ArrangementView.MaxClipLength;

        float spacing = AdaptiveGrid.CalculateGridSpacing(ArrangementView.Zoom, AdaptiveGrid.MinSpacing, AdaptiveGrid.MaxSpacing);

        float currentTime = 0f; // Starting time in beats (or seconds, as desired)
        float currentPosition = startX;
        
        while (currentPosition <= endX)
        {
            // Draw the grid line
            ImGui.GetWindowDrawList().AddLine(new Vector2(currentPosition, position.Y), new Vector2(currentPosition, position.Y + 125), gridColorSecondary, 1);

            // Increment position and time
            currentPosition += spacing;
            currentTime += spacing / AdaptiveGrid.BaseSpacing; // Convert spacing to beat time
        }
        */
        RenderGridLines(ArrangementView.ArrangementWidth, 125);

        /*
        uint gridColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.5f));
        float gridThickness = 1.0f;
        Vector2 position = ImGui.GetWindowPos();
        float startX = position.X;
        float endX = startX + Length;
        float seconds = 0f;
        for (float x = startX; x <= endX; x += 120 * ArrangementView.Zoom * ArrangementView.BeatsPerBar)
        {
            if (seconds == 9)
                seconds = -1;

            if (seconds % 2 != 0)
            {
                seconds++;
                continue;
            }
            var textSize = ImGui.CalcTextSize(seconds.ToString()).X;
            //ImGui.GetWindowDrawList().AddText(new Vector2(x - (textSize / 3), position.Y), ImGui.GetColorU32(Vector4.One), $"{seconds}");
            ImGui.GetWindowDrawList().AddLine(new Vector2(x, position.Y), new Vector2(x, position.Y + 125), gridColor, gridThickness);
            seconds += 1;
        }
        */
        // Track clips rendering
        foreach (var clip in Clips.ToList()) // try not to make a copy list here
        {
            clip.Render();
        }

        // actually delete deleted clip
        var deleted = Clips.FirstOrDefault(clip => clip.DeleteRequested);
        if (deleted != null)
        {
            Clips.Remove(deleted);
        }


        var duplicated = Clips.FirstOrDefault(clip => clip.DuplicateRequested);
        if (duplicated != null)
        {
            if (duplicated is AudioClip audioClip)
            {
                var copy = new AudioClip(this as AudioTrack, new AudioClipData(audioClip.Clip.AudioFileReader.FileName),
                    audioClip.StartTick + audioClip.DurationTicks);
                Clips.Add(copy);
                ArrangementView.SelectedClips.Clear();
                ArrangementView.SelectedClips.Add(copy);
                SetDraggedClip(copy);
                duplicated.DuplicateRequested = false;
            }
            else if (duplicated is MidiClip midiClip)
            {
                var copy = new MidiClip(this as MidiTrack, midiClip.MidiClipData, midiClip.StartTick + midiClip.DurationTicks);
                Clips.Add(copy);
                ArrangementView.SelectedClips.Clear();
                ArrangementView.SelectedClips.Add(copy);
                SetDraggedClip(copy);
                duplicated.DuplicateRequested = false;
            }
        }

    }

    private void RenderGridLines(float viewportWidth, float trackHeight)
    {
        long startTick = TimeLineV2.PositionToTime(ArrangementView.ArrangementScroolX);
        long endTick = TimeLineV2.PositionToTime(ArrangementView.ArrangementScroolX + viewportWidth);

        float pixelsPerTick = TimeLineV2.PixelsPerTick;
        long beatSpacing = TimeLineV2.PPQ;
        long barSpacing = (long)(beatSpacing * TimeLineV2.BeatsPerBar);

        long gridSpacing = barSpacing;
        //if (pixelsPerTick > 0.5f) gridSpacing = beatSpacing; // Zoomed in: Draw every beat
        //else if (pixelsPerTick < 0.01f) gridSpacing = barSpacing * 4; // Zoomed out: Draw every 4 bars

        for (long tick = (startTick / gridSpacing) * gridSpacing; tick <= endTick; tick += gridSpacing)
        {
            float xPosition = TimeLineV2.TimeToPosition(tick) - ArrangementView.ArrangementScroolX;

            if (tick % barSpacing == 0)
                DrawGridLine(ArrangementView.WindowPos.X + xPosition, new Vector4(0f, 0f, 0f, 0.3f), thickness: 1); // Bar line
            else if (gridSpacing == barSpacing) 
                DrawGridLine(ArrangementView.WindowPos.X + xPosition, new Vector4(1, 0, 0, 0.5f), thickness: 1); // Beat line
        }
    }

    void DrawGridLine(float xPosition, Vector4 color, float thickness = 1f)
    {
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(xPosition, ImGui.GetWindowPos().Y),         // Start point
            new Vector2(xPosition, ImGui.GetWindowPos().Y + 125),       // End point
            ImGui.ColorConvertFloat4ToU32(color), // Convert color
            thickness
        );
    }

    public void RenderControls()
    {
        if (ImGui.BeginPopupContextWindow("track_popup", ImGuiPopupFlags.MouseButtonRight))
        {
            RenderPopupMenu();
            ImGui.EndPopup();
        }

        // change later
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            DevicesView.SelectedTrack = this;
            BottomView.RenderedWindow = BottomViewWindows.DevicesView;
        }

        ImGui.Columns(3, "controls_column", false);
        ImGui.SetColumnWidth(0, 125);

        // Track Name
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Vector4.Zero);
        if (_renameRequested)
        {
            if (ImGui.InputText($"##name", ref _name, 30, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue)
                || ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = "Blank";
                }
                _renameRequested = false;
            }
            if (_renameRequested)
            {
                ImGui.SetKeyboardFocusHere(-1);
            }
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 1));
            var textPos = ImGui.GetCursorScreenPos() - Vector2.One;
            ImGui.TextWrapped(_name);
            ImGui.PopStyleColor(1);
            ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One);
            ImGui.SetCursorScreenPos(textPos);
            ImGui.TextWrapped(_name);
            ImGui.PopStyleColor(1);
        }
        ImGui.PopStyleColor(1);
        InfoBox.SetInfoData("Track name", "Name of the track.");

        ImGui.SetCursorPosY(ImGui.GetWindowSize().Y - ImGui.GetFrameHeightWithSpacing() - 5);
        var trackIcon = TrackType == TrackType.Audio ? Fontaudio.LogoAudiobus : FontAwesome6.BarsStaggered;
        var iconFont = TrackType == TrackType.Audio ? Fontaudio.IconFontPtr : ImGui.GetIO().Fonts.Fonts[0];
        ImGui.PushFont(iconFont);
        ImGui.Text(trackIcon);
        ImGui.PopFont();
        ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - 55);
        ImGui.ColorEdit4("##track_color", ref _color, ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoInputs);
        InfoBox.SetInfoData("Track color picker", "Allows to change the track color.");

        ImGui.GetWindowDrawList().AddLine(ImGui.GetWindowPos() + new Vector2(123, 0),
            ImGui.GetWindowPos() + new Vector2(123, 125),
            ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), 4f);

        float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }

        ImGui.NextColumn();
        ImGui.SetColumnWidth(1, 45);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg]);
        if (ImGui.BeginChild("volume_meter", new Vector2(20, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.FrameStyle))
        {
            var dl = ImGui.GetWindowDrawList();
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();
            float halfWidth = windowSize.X / 2;
            Vector4 vCol = Enabled ? new Vector4(0.58f, 0.98f, 0.29f, 1f) : new Vector4(0.49f, 0.49f, 0.49f, 1f);
            uint col = ImGui.GetColorU32(vCol);
            float alphaCol = 0.5f;

            // Define the smoothing factor
            float smoothingFactor = 0.1f; // Adjust this value to control the smoothing speed

            // Smooth gain variables (adjusted for smoother transitions)
            _smoothLeftChannelGain = Lerp(_smoothLeftChannelGain, _leftChannelGain, smoothingFactor);
            _smoothRightChannelGain = Lerp(_smoothRightChannelGain, _rightChannelGain, smoothingFactor);

            Vector2 lstart = new Vector2(windowPos.X, windowPos.Y + windowSize.Y * (1 - _smoothLeftChannelGain));
            Vector2 lend = new Vector2(windowPos.X + halfWidth, windowPos.Y + windowSize.Y);
            dl.AddRectFilledMultiColor(lstart, lend, col, col, ImGui.GetColorU32(col, alphaCol), ImGui.GetColorU32(col, alphaCol));

            Vector2 rstart = new Vector2(windowPos.X + halfWidth, windowPos.Y + windowSize.Y * (1 - _smoothRightChannelGain));
            Vector2 rend = new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y);
            dl.AddRectFilledMultiColor(rstart, rend, col, col, ImGui.GetColorU32(col, alphaCol), ImGui.GetColorU32(col, alphaCol));

            Vector2 lineStart = new Vector2(windowPos.X + halfWidth, windowPos.Y);
            Vector2 lineEnd = new Vector2(windowPos.X + halfWidth, windowPos.Y + windowSize.Y);
            dl.AddLine(lineStart, lineEnd, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), 1f);
        }
        ImGui.EndChild();
        InfoBox.SetInfoData("Gain meter", "Shows realtime audio gain");
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        ImGui.GetWindowDrawList().AddLine(ImGui.GetWindowPos() + new Vector2(165, 0),
            ImGui.GetWindowPos() + new Vector2(165, 125),
            ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), 2f);

        ImGui.NextColumn();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg]);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 2f);
        if (ImGui.BeginChild("button_controls", Vector2.Zero, ImGuiChildFlags.Border))
        {
            Fontaudio.Push();
            if (UiElement.Toggle($"{Fontaudio.Powerswitch}", _enabled, new Vector4(0.95f, 0.58f, 0.13f, 1f), new(50, 25)))
            {
                _enabled = !_enabled;
            }
            Fontaudio.Pop();
            InfoBox.SetInfoData("Switch toggle", "Turns on or off the track");
            ImGui.SameLine();
            Fontaudio.Push();
            if (UiElement.Toggle($"{Fontaudio.Solo}##track_solo", _solo, new Vector4(0.17f, 0.49f, 0.85f, 1f), new(35, 25)))
            {
                _solo = !_solo;
                ArrangementView.Tracks.ToList().ForEach(track =>
                {
                    if (track == this)
                    {
                        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                            _enabled = !_enabled;
                        else
                            _enabled = true;
                    }

                    if (track != this)
                    {
                        if (!ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                        {
                            track._enabled = !_solo;
                            track._solo = false;
                        }
                    }
                });
            }
            Fontaudio.Pop();
            InfoBox.SetInfoData("Solo toggle", "Mute all tracks except this one");
            ImGui.SameLine();
            Fontaudio.Push();
            if (UiElement.Toggle($"{Fontaudio.Armrecording}##track_record", _recordOnStart, new Vector4(0.88f, 0.20f, 0.16f, 1f), new(35, 25)))
            {
                _recordOnStart = !_recordOnStart;
            }
            Fontaudio.Pop();
            InfoBox.SetInfoData("Track recording", "Start audio recording when record button is pressed");

            ImGui.Spacing();

            // volume control
            if (UiElement.DragSlider($"{FontAwesome6.VolumeHigh}##track_volume", 105, ref _volume, 0.1f, -90f, 6f, "%.1f", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput))
            {
                float linearVolume = (float)Math.Pow(10, _volume / 20);
                Engine.StereoSampleProvider.SetGain(linearVolume);
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _volume = 0f;
                float linearVolume = (float)Math.Pow(10, _volume / 20);
                Engine.StereoSampleProvider.SetGain(linearVolume);
            }
            InfoBox.SetInfoData("Volume slider", "Controls track volume");

            ImGui.Spacing();

            // pan control
            if (UiElement.DragSlider($"{FontAwesome6.RightLeft}##track_pan", 105, ref _pan, 0.1f, -50f, 50f, "%.0f", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput))
            {
                float mappedPan = _pan / 50f;
                Engine.StereoSampleProvider.Pan = mappedPan;
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _pan = 0f;
                float mappedPan = _pan / 50f;
                Engine.StereoSampleProvider.Pan = mappedPan;
            }
            InfoBox.SetInfoData("Panning slider", "Controls track panning");
        }
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);

        ImGui.EndChild();
    }

    protected void RenderPopupMenu()
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

        }
        if (ImGui.MenuItem("Delete", "Del"))
        {
            ArrangementView.Tracks.Remove(this);
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.MenuItem("Rename", "Ctrl+R"))
        {
            _renameRequested = true;
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.MenuItem("Insert Audio Track", "Ctrl+T"))
        {
            ArrangementView.NewAudioTrack($"Audio Track {ArrangementView.Tracks.Count}", ArrangementView.Tracks.IndexOf(this) + 1);
        }
        if (ImGui.MenuItem("Insert Midi Track", "Ctrl+Shift+T"))
        {
            ArrangementView.NewMidiTrack($"Midi Track {ArrangementView.Tracks.Count}", ArrangementView.Tracks.IndexOf(this) + 1);
        }
        ImGui.PopStyleColor();
    }


    // flag of popup menu
    private bool _renameRequested;

    private bool _hasDropped = false; // Flag to prevent multiple additions during one drop
}
