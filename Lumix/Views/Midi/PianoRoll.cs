using Lumix.Plugins.VST;
using IconFonts;
using ImGuiNET;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using System.Numerics;
using Lumix.Clips.MidiClips;
using Lumix.Tracks.MidiTracks;
using Lumix.Views.Arrangement;
using Lumix.ImGuiExtensions;
using Melanchall.DryWetMidi.Common;
using Note = Melanchall.DryWetMidi.Interaction.Note;

namespace Lumix.Views.Midi;

public class PianoRoll
{
    private float _pixelsPerTick => 0.05f * _zoom;

    private MidiTrack _midiTrack;
    private MidiClip _midiClip;
    private const int TotalKeys = 88; // Full piano keys (A0 to C8)
    private const float KeyWidth = 60f;
    private float _noteHeight;

    public List<PNote> _notes = new();
    private PNote? _currentNote = null;
    private List<PNote> _selectedNotes = new();
    private List<bool> _notesHovered = new(); // list of notes hovered state
    private int _lastSentNoteNum;

    private float _beatsPerBar = 1;
    private bool _keysSound;
    private float _zoom = 2f;
    private float _vZoom = 0.3f;
    private float _scrollX = 0f;
    private float _scrollY = 0f;
    private float _panBoostForce = 4f;

    private Vector2 _windowPos;
    private Vector2 _windowSize;
    private bool _hoveringPiano;
    private bool _initialized;
    private float _timer;

    public PianoRoll(MidiClip parent, MidiTrack midiTrack)
    {
        _midiClip = parent;
        _midiTrack = midiTrack;
    }

    public void ZoomChange(float value)
    {
        const float epsilon = 0.0001f;

        // Calculate the mouse position within the arrangement view
        float mousePosInWindowX = ImGui.GetMousePos().X - _windowPos.X - KeyWidth;
        float mousePosInContentX = mousePosInWindowX + _scrollX;
        //float t = (_zoom <= 0.1f + epsilon && value < 0) || (_zoom < 0.1f - epsilon && value > 0) ? 0.01f : 0.1f;

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
            float zoomFactor = _zoom / _previousValue;
            _scrollX = Math.Clamp(mousePosInContentX * zoomFactor - mousePosInWindowX, 0, float.PositiveInfinity);
        }
    }

    public void VZoomChange(float value)
    {
        // Calculate the mouse position within the arrangement view
        float mousePosInWindowY = ImGui.GetMousePos().Y - _windowPos.Y;
        float mousePosInContentY = mousePosInWindowY + _scrollY;

        // Store the current zoom value
        float _previousValue = _vZoom;

        // Update the zoom level
        if (value > 0)
        {
            _vZoom = Math.Clamp(_vZoom + 0.1f, 0.1f, 2f);
        }
        else
        {
            _vZoom = Math.Clamp(_vZoom - 0.1f, 0.1f, 2f);
        }

        if (_previousValue != _vZoom)
        {
            float zoomFactor = _vZoom / _previousValue;
            _scrollY = Math.Clamp(mousePosInContentY * zoomFactor - mousePosInWindowY, 0, float.PositiveInfinity);
        }
    }

    private void ListenForShortcuts()
    {
        // Shift note up or down
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
        {
            if (ImGui.IsKeyDown(ImGuiKey.ModShift))
            {
                _selectedNotes.ForEach(n => n.Data.NoteNumber = (SevenBitNumber)Math.Clamp(n.Data.NoteNumber + new SevenBitNumber(12), 21, 108));
                _scrollY = Math.Clamp(_scrollY - (_noteHeight * 12), 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
            }
            else
            {
                _selectedNotes.ForEach(n => n.Data.NoteNumber = (SevenBitNumber)Math.Clamp(n.Data.NoteNumber + new SevenBitNumber(1), 21, 108));
                _scrollY = Math.Clamp(_scrollY - _noteHeight, 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
            }

            HandleOverlappingNotes();
            _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
        {
            if (ImGui.IsKeyDown(ImGuiKey.ModShift))
            {
                _selectedNotes.ForEach(n => n.Data.NoteNumber = (SevenBitNumber)Math.Clamp(n.Data.NoteNumber - new SevenBitNumber(12), 21, 108));
                _scrollY = Math.Clamp(_scrollY + (_noteHeight * 12), 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
            }
            else
            {
                _selectedNotes.ForEach(n => n.Data.NoteNumber = (SevenBitNumber)Math.Clamp(n.Data.NoteNumber - new SevenBitNumber(1), 21, 108));
                _scrollY = Math.Clamp(_scrollY + _noteHeight, 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
            }

            HandleOverlappingNotes();
            _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
        }

        // Move note right or left or make longer/shorter if Shift is pressed
        if (ImGui.IsKeyPressed(ImGuiKey.RightArrow))
        {
            if (ImGui.IsKeyDown(ImGuiKey.ModShift))
            {
                _selectedNotes.ForEach(n => n.Data.Length = n.Data.Length + GetTicksInBar());
            }
            else
                _selectedNotes.ForEach(n => n.Data.Time = n.Data.Time + GetTicksInBar());

            HandleOverlappingNotes();
            _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow))
        {
            if (ImGui.IsKeyDown(ImGuiKey.ModShift))
            {
                _selectedNotes.ForEach(n => n.Data.Length = Math.Clamp(n.Data.Length - GetTicksInBar(), GetTicksInBar(), long.MaxValue));
            }
            else
                _selectedNotes.ForEach(n => n.Data.Time = Math.Clamp(n.Data.Time - GetTicksInBar(), 0, long.MaxValue));

            HandleOverlappingNotes();
            _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
        }

        // Enable/Disable selected notes
        if (ImGui.IsKeyPressed(ImGuiKey._0, false))
        {
            _selectedNotes.ForEach(n => n.Enabled = !n.Enabled);
        }

        // Notes duplication on Ctrl+LMB
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsMouseClicked(ImGuiMouseButton.Left, false))
        {
            DuplicateNotes(false, false);
        }

        // Notes duplication on Ctrl+D
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyPressed(ImGuiKey.D, false) && !ImGui.IsKeyDown(ImGuiKey.ModAlt))
        {
            DuplicateNotes(true);
        }
    }

    private void DuplicateNotes(bool shiftTime = false, bool handleOverlaps = true)
    {
        foreach (var note in _selectedNotes.ToList())
        {
            _selectedNotes.Remove(note);
            var clone = note.Data.Clone();
            var newNote = new PNote((Note)clone);

            if (shiftTime)
            {
                newNote.Data.Time = note.Data.EndTime; // shift time
            }

            newNote.Data.LengthChanged += (sender, e) =>
            {
                HandleOverlappingNotes();
                _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
            };
            newNote.Data.TimeChanged += (sender, e) =>
            {
                HandleOverlappingNotes();
                _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
            };

            _notes.Add(newNote);
            _selectedNotes.Add(newNote);
        }

        if (handleOverlaps)
        {
            HandleOverlappingNotes();
        }       
        _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
    }

    public void Render()
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.22f, 0.22f, 0.22f, 1f));
        ImGui.BeginChild("piano_roll_menubar", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.MenuBar);

        ListenForShortcuts();

        if (ImGui.BeginMenuBar())
        {
            ImGui.SetCursorScreenPos(ImGui.GetWindowPos());
            if (UiElement.Toggle($"{FontAwesome6.HeadphonesSimple}", _keysSound, new Vector4(0.95f, 0.58f, 0.13f, 1f), new Vector2(40, ImGui.GetFrameHeightWithSpacing())))
            {
                _keysSound = !_keysSound;
            }

            if (ImGui.BeginCombo("H-Zoom", $"{_zoom * 10:n0}x", ImGuiComboFlags.WidthFitPreview | ImGuiComboFlags.HeightLargest))
            {
                for (int i = 1; i <= 20; i++)
                {
                    if (ImGui.Selectable($"{i}x"))
                    {
                        _zoom = i / 10f;
                        float maxScrollX = TimeToPosition(_midiClip.DurationTicks);
                        _scrollX = Math.Clamp(_scrollX, 0f, maxScrollX);
                        _scrollY = Math.Clamp(_scrollY, 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
                    }
                }
                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo("V-Zoom", $"{_vZoom * 10:n0}x", ImGuiComboFlags.WidthFitPreview | ImGuiComboFlags.HeightLargest))
            {
                for (int i = 1; i <= 20; i++)
                {
                    if (ImGui.Selectable($"{i}x"))
                    {
                        _vZoom = i / 10f;
                        float maxScrollX = TimeToPosition(_midiClip.DurationTicks);
                        _scrollX = Math.Clamp(_scrollX, 0f, maxScrollX);
                        _scrollY = Math.Clamp(_scrollY, 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
                    }
                }
                ImGui.EndCombo();
            }

            //ImGui.Text($"{FontAwesome6.ArrowsLeftRightToLine} {(int)(_zoom * 10)}x/20");
            //ImGui.Text($"{FontAwesome6.ArrowDownUpAcrossLine} {(int)(_vZoom * 10)}x/20");
            ImGui.Separator();
            int scroll = (int)(_scrollY / (TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y) * 100);
            scroll = Math.Clamp(scroll, 0, int.MaxValue);
            ImGui.TextUnformatted($"V: {scroll} {FontAwesome6.Percent}");
            ImGui.TextUnformatted($"H: 0 {FontAwesome6.Percent}");
            ImGui.EndMenuBar();
        }

        // Menu bar bottom line
        ImGui.GetWindowDrawList().AddLine(new Vector2(_windowPos.X, _windowPos.Y - 8), new Vector2(_windowPos.X + _windowSize.X, _windowPos.Y - 8),
            ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), 1);

        ImGui.BeginChild("piano_roll", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.None);
        _windowPos = ImGui.GetCursorScreenPos();
        _windowSize = ImGui.GetWindowSize();
        _noteHeight = ImGui.GetWindowSize().Y / TotalKeys; // spread keys across all window height

        _hoveringPiano = ImGui.IsMouseHoveringRect(_windowPos, _windowPos + new Vector2(KeyWidth, _windowSize.Y));
        
        RenderGrid();
        RenderNotes();
        RenderKeys();
        RenderTimeLine();
        HandleMouseInteraction();

        if (!ImGui.IsPopupOpen("note_popup") && ImGui.BeginPopupContextWindow("piano_roll_popup", ImGuiPopupFlags.MouseButtonRight))
        {
            RenderPopupMenu();
            ImGui.EndPopup();
        }

        if (!_initialized && _timer > 0.01f)
        {
            _scrollY = (TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y) / 2f;
            _initialized = true;
        }

        if (!_initialized)
            _timer += ImGui.GetIO().DeltaTime;

        ImGui.EndChild();


        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private void RenderKeys()
    {
        var drawList = ImGui.GetWindowDrawList();
        for (int i = 0; i < TotalKeys; i++)
        {
            // Map the row to a MIDI note from A0 (MIDI 21) to C8 (MIDI 108)
            int noteNumber = RowToNoteNumberDraw(i);

            // Determine whether the key is black or white
            bool isBlackKey = IsBlackKey(noteNumber);

            // Calculate position of the key
            Vector2 keyStart = _windowPos + new Vector2(0, (TotalKeys - 1 - i) * _noteHeight * (_vZoom * 10) - _scrollY); // Reverse the order here
            Vector2 keyEnd = keyStart + new Vector2(KeyWidth, _noteHeight * (_vZoom * 10));

            // Set the color of the key (black or white)
            uint keyColor = isBlackKey
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)) // Black key
                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.8f, 0.8f, 1.0f)); // White key

            // Draw the key
            drawList.AddRectFilled(keyStart, keyEnd, keyColor);
            /*
            if (ImGui.IsMouseHoveringRect(keyStart, keyEnd) && _hoveringPiano && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _keysSound)
            {
                if (_lastSentNoteNum != noteNumber)
                {
                    _midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOff(0, _lastSentNoteNum, 0);
                    _midiTrack.MidiEngine.Synthesizer.NoteOn(0, noteNumber, 100);
                    _midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOn(0, noteNumber, 100);
                }
                _lastSentNoteNum = noteNumber;
            }
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _hoveringPiano && _keysSound)
            {
                _midiTrack.MidiEngine.Synthesizer.NoteOffAll(false);
                _midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOff(0, noteNumber, 0);
                _lastSentNoteNum = noteNumber;
            }
            else if (!_hoveringPiano && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _keysSound)
            {
                _midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOff(0, _lastSentNoteNum, 0);
                _lastSentNoteNum = 0;
            }
            */
            // Add a border around the key
            drawList.AddRect(keyStart, keyEnd, ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1f)), 0, ImDrawFlags.None, 1);

            // if notes are too small skip text rendering
            if (_vZoom < 0.3f)
                continue;

            // Determine the note name (e.g., C, C#, D, etc.)
            string noteName = NoteNumberToName(noteNumber);

            // Set the text color
            uint textColor = isBlackKey
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1.0f)) // White text for black keys
                : ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 1.0f)); // Black text for white keys

            // Highlight C notes
            bool isC = noteName[0] == 'C' && int.TryParse(noteName[1].ToString(), out var res);

            // Calculate the position for the note name text (centered on the key)
            Vector2 textSize = ImGui.CalcTextSize(noteName);
            Vector2 textPos = keyStart + new Vector2((KeyWidth - textSize.X) * 0.5f, (_noteHeight * (_vZoom * 10) - textSize.Y) * 0.5f);

            // Draw the note name
            if (isC)
            {
                drawList.AddText(textPos, textColor, noteName);
            }
            else if (NoteNumberToRow(noteNumber) == GetRowNumberAtCursor())
            {
                drawList.AddText(textPos, textColor, noteName);
            }
        }
    }

    private void RenderGrid()
    {
        var drawList = ImGui.GetWindowDrawList();
        uint gridColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.2f));

        long startTick = PositionToTime(_scrollX);
        long endTick = PositionToTime(_scrollX + _windowSize.X - KeyWidth);

        long beatSpacing = TimeLineV2.PPQ;
        long barSpacing = (long)(beatSpacing * _beatsPerBar);

        float pixelsPerTick = _pixelsPerTick;
        long gridSpacing = barSpacing;

        // Vertical grid
        for (long tick = (startTick / gridSpacing) * gridSpacing; tick <= endTick; tick += gridSpacing)
        {
            float xPosition = TimeToPosition(tick) - _scrollX;

            if (tick % barSpacing == 0)
                DrawGridLine(_windowPos.X + KeyWidth + xPosition, new Vector4(0f, 0f, 0f, 0.2f), thickness: 1); // Bar line
            else if (gridSpacing == barSpacing)
                DrawGridLine(_windowPos.X + KeyWidth + xPosition, new Vector4(1, 0, 0, 0.5f), thickness: 1); // Beat line
        }

        // Horizontal grid
        for (int row = 0; row < TotalKeys; row++)
        {
            Vector2 lineStart = _windowPos + new Vector2(KeyWidth, row * _noteHeight * (_vZoom * 10) - _scrollY);
            Vector2 lineEnd = _windowPos + new Vector2(KeyWidth + _windowSize.X - KeyWidth, row * _noteHeight * (_vZoom * 10) - _scrollY);
            drawList.AddLine(lineStart, lineEnd, gridColor);
        }

        // Make Bars:Beats:Ticks text not clash itself
        float minTextSpacing = 60f;
        if (pixelsPerTick * gridSpacing < minTextSpacing)
        {
            while (pixelsPerTick * gridSpacing < minTextSpacing)
            {
                gridSpacing += barSpacing;
            }
        }

        // Bars:Beats:Ticks timeline
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.12f, 0.12f, 0.12f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 0);
        ImGui.BeginChild("bars_timeline", new Vector2(ImGui.GetWindowSize().X, 20), ImGuiChildFlags.None, ImGuiWindowFlags.None);
        for (long tick = (startTick / gridSpacing) * gridSpacing; tick <= endTick; tick += gridSpacing)
        {
            float xPosition = TimeToPosition(tick) - _scrollX + _windowPos.X + KeyWidth;
            var musicalTime = TimeLineV2.TicksToMusicalTime(tick, true);
            xPosition -= ImGui.CalcTextSize($"{musicalTime.Bars}.{musicalTime.Beats}").X / 2;

            if (tick % barSpacing == 0 && xPosition > _windowPos.X + KeyWidth)
            {
                ImGui.GetWindowDrawList().AddText(new(xPosition, ImGui.GetWindowPos().Y),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, 0.5f)),
                    $"{musicalTime.Bars}.{musicalTime.Beats}");
            }
        }
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
        ImGui.EndChild();
    }

    private float TimeToPosition(long ticks)
    {
        return ticks * _pixelsPerTick;
    }

    private long PositionToTime(float x)
    {
        return (long)(x / _pixelsPerTick);
    }

    private  float TicksToPixels(long ticks)
    {
        return ticks * _pixelsPerTick;
    }

    public long SnapToGrid(long tick)
    {
        long gridSpacing = (long)(TimeLineV2.PPQ * _beatsPerBar);
        return (long)Math.Round((double)tick / gridSpacing) * gridSpacing;
    }

    /// <returns>How many ticks are in one bar</returns>
    private long GetTicksInBar()
    {
        long barTicks = (long)(TimeLineV2.PPQ * _beatsPerBar);
        double barSeconds = TimeLineV2.TicksToSeconds(barTicks, false);
        long realBarTicks = TimeConverter.ConvertFrom(new MetricTimeSpan(TimeSpan.FromSeconds(barSeconds)), _midiClip.MidiClipData.TempoMap);
        return realBarTicks;
    }

    void DrawGridLine(float xPosition, Vector4 color, float thickness = 1f)
    {
        ImGui.GetWindowDrawList().AddLine(
            new Vector2(xPosition, _windowPos.Y),
            new Vector2(xPosition, _windowPos.Y + _windowSize.Y),
            ImGui.ColorConvertFloat4ToU32(color),
            thickness
        );
    }

    private void RenderNotes()
    {
        bool requestUpdate = false;
        var drawList = ImGui.GetWindowDrawList();
        List<PNote> deleted = new();
        foreach (var note in _notes.ToList())
        {
            int row = NoteNumberToRow(note.Data.NoteNumber);
            float noteStartTime = (float)note.Data.TimeAs<MetricTimeSpan>(_midiClip.MidiClipData.TempoMap).TotalSeconds;
            float noteDuration = (float)note.Data.LengthAs<MetricTimeSpan>(_midiClip.MidiClipData.TempoMap).TotalSeconds;
            noteStartTime = TimeToPosition(TimeLineV2.SecondsToTicks(noteStartTime, false));
            noteDuration = TimeToPosition(TimeLineV2.SecondsToTicks(noteDuration, false));

            Vector2 rectStart = _windowPos + new Vector2(KeyWidth + noteStartTime - _scrollX, row * _noteHeight * (_vZoom * 10) - _scrollY);
            Vector2 rectEnd = _windowPos + new Vector2(KeyWidth + noteStartTime + noteDuration - _scrollX, (row + 1) * _noteHeight * (_vZoom * 10) - _scrollY);

            if (rectEnd.X < _windowPos.X + KeyWidth)
                continue;

            var noteColor = note.Enabled ? _midiClip.Color : Vector4.Zero;

            float velocityScale = note.Data.Velocity / 127f;
            float minBrightness = 0.2f;

            // Calculate the maximum color channel value to preserve the color's ratios
            float maxChannel = Math.Max(noteColor.X, Math.Max(noteColor.Y, noteColor.Z));
            if (maxChannel > 0)
            {
                noteColor.X = Math.Max(minBrightness * (noteColor.X / maxChannel), noteColor.X * velocityScale);
                noteColor.Y = Math.Max(minBrightness * (noteColor.Y / maxChannel), noteColor.Y * velocityScale);
                noteColor.Z = Math.Max(minBrightness * (noteColor.Z / maxChannel), noteColor.Z * velocityScale);
            }

            drawList.AddRectFilled(rectStart + new Vector2(0, 1), rectEnd - new Vector2(0, 1), ImGui.ColorConvertFloat4ToU32(noteColor), 2);
            drawList.AddRect(rectStart, rectEnd, note.Enabled ? ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.3f)) 
                : ImGui.ColorConvertFloat4ToU32(_midiClip.Color), 2); // border

            ImGui.SetCursorScreenPos(rectStart);
            ImGui.InvisibleButton("##invisible_button", rectEnd - rectStart);

            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                if (!_selectedNotes.Contains(note))
                {
                    _selectedNotes.Add(note);
                }
                ImGui.OpenPopup("note_popup");
            }

            // Highlight selected notes with border
            if (_selectedNotes.Contains(note))
                drawList.AddRect(rectStart, rectEnd, ImGui.ColorConvertFloat4ToU32(new Vector4(0.55f, 0.79f, 0.85f, 1f)), 2, ImDrawFlags.None, 2); // border

            string noteName = $"{note.Data.NoteName.ToString().Replace("Sharp", "#")}{note.Data.Octave}";
            if (_vZoom >= 0.3f && ImGui.CalcTextSize(noteName).X < rectEnd.X - rectStart.X) // draw notes text
                drawList.AddText(new Vector2(rectStart.X + 2, 
                    rectStart.Y + (rectEnd.Y - rectStart.Y - ImGui.CalcTextSize(noteName).Y) / 2),
                    note.Enabled && note.Data.Velocity > 50 ? ImGui.GetColorU32(new Vector4(0, 0, 0, 1)) : ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]),
                    noteName);

            bool noteHovered = ImGui.IsMouseHoveringRect(rectStart, rectEnd);
            _notesHovered.Add(noteHovered);

            // Border hover
            float rectWidth = rectEnd.X - rectStart.X;
            float resizeGripSize = rectWidth * 10 / 100;
            resizeGripSize = Math.Clamp(resizeGripSize, 5f, 15f);
            bool rightBorderHover = ImGui.IsMouseHoveringRect(new Vector2(rectEnd.X - resizeGripSize, rectStart.Y), rectEnd);
            bool leftBorderHover = ImGui.IsMouseHoveringRect(rectStart, new Vector2(rectStart.X + resizeGripSize, rectEnd.Y));
            //drawList.AddRectFilled(new Vector2(rectEnd.X - resizeGripSize, rectStart.Y), rectEnd, ImGui.GetColorU32(Vector4.One));
            //drawList.AddRectFilled(rectStart, new Vector2(rectStart.X + resizeGripSize, rectEnd.Y), ImGui.GetColorU32(Vector4.One));

            if (noteHovered)
            {           
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    deleted.Add(note);
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (!rightBorderHover && !leftBorderHover)
                        _movingNotes = true;

                    if (ImGui.IsKeyDown(ImGuiKey.ModShift))
                    {
                        if (!_selectedNotes.Contains(note))
                            _selectedNotes.Add(note);
                    }
                    else
                    {
                        _selectedNotes.Clear();
                        _selectedNotes.Add(note);
                    }

                    if (_keysSound)
                    {
                        var vstPlugin = _midiTrack.Engine.PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
                        vstPlugin?.SendNoteOn(0, note.Data.NoteNumber, note.Data.Velocity);
                        _lastSentNoteNum = note.Data.NoteNumber;
                        //_midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOn(0, note.NoteNumber, note.Velocity);
                    }
                }
            }

            // Note resizing
            if ((rightBorderHover || _rightResizing) && !_leftResizing && !_movingNotes)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    _rightResizing = true;

                    long tick = GetTicksAtCursor();
                    _resizeSnapTick ??= SnapToGrid(tick);

                    if (SnapToGrid(tick) > _resizeSnapTick)
                    {
                        _selectedNotes.ForEach(n => {
                            n.Data.Length = Math.Clamp(n.Data.Length + GetTicksInBar(), GetTicksInBar(), long.MaxValue);
                        });
                        _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                        _resizeSnapTick = SnapToGrid(tick);
                    }
                    else if (SnapToGrid(tick) < _resizeSnapTick)
                    {
                        _selectedNotes.ForEach(n => {
                            n.Data.Length = Math.Clamp(n.Data.Length - GetTicksInBar(), GetTicksInBar(), long.MaxValue);
                        });
                        _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                        _resizeSnapTick = SnapToGrid(tick);
                    }
                }
            }
            else if ((leftBorderHover || _leftResizing) && !_rightResizing && !_movingNotes)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    _leftResizing = true;

                    long tick = GetTicksAtCursor();
                    _resizeSnapTick ??= SnapToGrid(tick);

                    if (SnapToGrid(tick) < _resizeSnapTick)
                    {
                        _selectedNotes.ForEach(n => {
                            if (n.Data.Time > 0)
                            {
                                n.Data.Length = Math.Clamp(n.Data.Length + GetTicksInBar(), GetTicksInBar(), long.MaxValue);
                            }
                            n.Data.Time = Math.Clamp(n.Data.Time - GetTicksInBar(), 0, long.MaxValue);
                        });
                        _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                        _resizeSnapTick = SnapToGrid(tick);
                    }
                    else if (SnapToGrid(tick) > _resizeSnapTick)
                    {
                        _selectedNotes.ForEach(n => {
                            n.Data.Time = Math.Clamp(n.Data.Time + GetTicksInBar(), 0, n.Data.EndTime - GetTicksInBar());
                            n.Data.Length = Math.Clamp(n.Data.Length - GetTicksInBar(), GetTicksInBar(), long.MaxValue);
                        });
                        _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                        _resizeSnapTick = SnapToGrid(tick);
                    }
                }
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && (_rightResizing || _leftResizing)) 
            {
                _resizeSnapTick = null;
                _rightResizing = false;
                _leftResizing = false;
            }
        }

        if (ImGui.BeginPopup("note_popup"))
        {
            RenderNotePopupMenu();
            ImGui.EndPopup();
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Delete))
        {
            _selectedNotes.ForEach(note =>
            {
                _notes.Remove(note);
            });
            _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
        }

        // Actually remove all deleted notes
        deleted.ForEach(note =>
        {
            _notes.Remove(note);
        });

        // Update midi data if atleast one note was deleted
        if (deleted.Count > 0 || requestUpdate)
            _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
    }
    private long? _resizeSnapTick = null;
    private bool _rightResizing;
    private bool _leftResizing;

    private void RenderTimeLine()
    {
        float xOffset = _windowPos.X + TimeToPosition(TimeLineV2.GetCurrentTick()) - _scrollX + KeyWidth - TicksToPixels(_midiClip.StartTick);
        //float xOffset = _windowPos.X + TimeLine.CurrentTime * _zoom - _scrollX + KeyWidth;
        if (TimeLineV2.GetCurrentTick() > 0 && xOffset > _windowPos.X + KeyWidth && xOffset < _windowPos.X + _windowSize.X && TimeLineV2.IsPlaying())
            ImGui.GetWindowDrawList().AddLine(new(xOffset, _windowPos.Y), new(xOffset, _windowPos.Y + _windowSize.Y), ImGui.GetColorU32(new Vector4(1, 1, 1, 0.8f)));

        if (xOffset < _windowPos.X + _windowSize.X && xOffset > _windowPos.X + KeyWidth)
            ImGui.GetForegroundDrawList().AddTriangleFilled(new Vector2(xOffset, _windowPos.Y) - new Vector2(-6f, 6),
                new Vector2(xOffset, _windowPos.Y) - new Vector2(6f, 6),
                new Vector2(xOffset, _windowPos.Y) - new Vector2(0, 0f),
                ImGui.GetColorU32(new Vector4(0.95f, 0.58f, 0.13f, 1f)));
    }

    private void RenderPopupMenu()
    {
        ImGui.PushStyleColor(ImGuiCol.Separator, Vector4.One);
        ImGui.SeparatorText("Fixed Grid");
        if (ImGui.BeginTable("grid_settings", 5, ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            if (ImGui.MenuItem("8 Bars", string.Empty, _beatsPerBar == 32))
                _beatsPerBar = 32;
            ImGui.TableSetColumnIndex(1);
            if (ImGui.MenuItem("4 Bars", "", _beatsPerBar == 16))
                _beatsPerBar = 16;
            ImGui.TableSetColumnIndex(2);
            if (ImGui.MenuItem("2 Bars", "", _beatsPerBar == 8))
                _beatsPerBar = 8;
            ImGui.TableSetColumnIndex(3);
            if (ImGui.MenuItem("1 Bar", "", _beatsPerBar == 4))
                _beatsPerBar = 4;
            ImGui.TableSetColumnIndex(4);
            if (ImGui.MenuItem("1/2", "", _beatsPerBar == 2))
                _beatsPerBar = 2;
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            if (ImGui.MenuItem("1/4", "", _beatsPerBar == 1))
                _beatsPerBar = 1;
            ImGui.TableSetColumnIndex(1);
            if (ImGui.MenuItem("1/8", "", _beatsPerBar == 1 / 2f))
                _beatsPerBar = 1 / 2f;
            ImGui.TableSetColumnIndex(2);
            if (ImGui.MenuItem("1/16", "", _beatsPerBar == 1 / 4f))
                _beatsPerBar = 1 / 4f;
            ImGui.TableSetColumnIndex(3);
            if (ImGui.MenuItem("1/32", "", _beatsPerBar == 1 / 8f))
                _beatsPerBar = 1 / 8f;
            ImGui.TableSetColumnIndex(4);
            if (ImGui.MenuItem("Off"))
                _beatsPerBar = 4;
            ImGui.EndTable();
        }
        ImGui.PopStyleColor();
    }

    private void RenderNotePopupMenu()
    {
        ImGui.PushStyleColor(ImGuiCol.Separator, Vector4.One);
        if (ImGui.MenuItem("Duplicate", "Ctrl+D"))
        {
            DuplicateNotes(true);
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        string state = _selectedNotes.All(n => n.Enabled) ? "Deactivate Notes" : "Activate Notes";
        if (ImGui.MenuItem(state, "0"))
        {
            _selectedNotes.ForEach(n => n.Enabled = !n.Enabled);
        }
        ImGui.PopStyleColor();
    }

    private void HandleMouseInteraction()
    {
        Vector2 mousePos = ImGui.GetMousePos();
        Vector2 localPos = mousePos - _windowPos;

        if (localPos.X > KeyWidth && localPos.Y > 0 && localPos.Y < TotalKeys * _noteHeight)
        {
            // Apply scrolling offsets
            localPos.X += _scrollX;
            localPos.Y += _scrollY;

            float maxScrollX = TimeToPosition(_midiClip.DurationTicks);

            // Handle horizontal zoom
            float scrollDelta = ImGui.GetIO().MouseWheel;
            if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && scrollDelta != 0)
            {
                ZoomChange(scrollDelta);
                _scrollX = Math.Clamp(_scrollX, 0f, maxScrollX);
                _scrollY = Math.Clamp(_scrollY, 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
            }

            // Handle vertical zoom
            if (ImGui.IsKeyDown(ImGuiKey.ModAlt) && scrollDelta != 0)
            {
                VZoomChange(scrollDelta);
                _scrollX = Math.Clamp(_scrollX, 0f, maxScrollX);
                _scrollY = Math.Clamp(_scrollY, 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
            }

            // Handle scrolling with mouse wheel
            if (!ImGui.IsKeyDown(ImGuiKey.ModAlt) && !ImGui.IsKeyDown(ImGuiKey.ModCtrl) && scrollDelta != 0)
            {
                _scrollY -= scrollDelta * 100;
                _scrollY = Math.Clamp(_scrollY, 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
            }

            // Handle panning with middle button
            if (ImGui.IsMouseDown(ImGuiMouseButton.Middle))
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                float force = ImGui.IsKeyDown(ImGuiKey.ModShift) ? _panBoostForce : 1f;
                _scrollX -= ImGui.GetIO().MouseDelta.X * force;
                _scrollY -= ImGui.GetIO().MouseDelta.Y * force;
                _scrollX = Math.Clamp(_scrollX, 0f, maxScrollX);
                _scrollY = Math.Clamp(_scrollY, 0f, TotalKeys * _noteHeight * (_vZoom * 10) - _windowSize.Y);
            }

            // If no note is hovered
            if (!_notesHovered.Any(n => n == true))
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsPopupOpen("note_popup"))
                {
                    _selectedNotes.Clear(); // Deselect all notes
                }
            }
            /*
            // Selection area
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                _selectionRectStart ??= mousePos;
                ImGui.GetWindowDrawList().AddRect(_selectionRectStart.Value, mousePos, ImGui.GetColorU32(Vector4.One));

                foreach (var note in _notes)
                {
                    float endPos = TimeToPosition(TimeLineV2.SecondsToTicks(note.EndTimeAs<MetricTimeSpan>(_midiClip.MidiClipData.TempoMap).TotalSeconds, false));
                    UiElement.Tooltip($"{localPos.X - KeyWidth}, {endPos}");
                    if (localPos.X - KeyWidth < endPos)
                    {
                        if (!_selectedNotes.Contains(note))
                            _selectedNotes.Add(note);
                    }
                    else if (_selectedNotes.Contains(note))
                        _selectedNotes.Remove(note);
                }
            }
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                _selectionRectStart = null;
            */
            // We get the note row number dividing mouse local pos to the height of each note
            int row = (int)(localPos.Y / (_vZoom * 10) / _noteHeight);            
            float adjustedMousePosX = (int)localPos.X - KeyWidth;
            long tick = SnapToGrid(PositionToTime(adjustedMousePosX));
            float seconds = (float)TimeLineV2.TicksToSeconds(tick, false);
            long realTicks = TimeConverter.ConvertFrom(new MetricTimeSpan(TimeSpan.FromSeconds(seconds)), _midiClip.MidiClipData.TempoMap);
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && !_notesHovered.Any(n => n == true))
            {
                if (_keysSound)
                {
                    var vstPlugin = _midiTrack.Engine.PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
                    vstPlugin?.SendNoteOn(0, RowToNoteNumber(row), 100);
                    //_midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOn(0, RowToNoteNumber(row), 100);
                    _lastSentNoteNum = RowToNoteNumber(row);
                }

                // We start placing the note
                NoteName noteName = RowToNoteName(row);
                int octave = RowToOctave(row);
                Note data = new(noteName, octave)
                {
                    Velocity = new SevenBitNumber(100),
                    Time = realTicks,
                    Length = GetTicksInBar() // this is the default note length when placing start
                };
                _currentNote = new PNote(data);
                _currentNote.Data.LengthChanged += (sender, e) =>
                {
                    HandleOverlappingNotes();
                    _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                };
                _currentNote.Data.TimeChanged += (sender, e) =>
                {
                    HandleOverlappingNotes();
                    _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                };
                _notes.Add(_currentNote);
            }
            else if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && _currentNote != null)
            {
                // We update the note length while dragging
                _currentNote.Data.Length = Math.Clamp(realTicks - _currentNote.Data.Time, GetTicksInBar(), long.MaxValue);

                // We update the note velocity while dragging
                float delta = Math.Clamp(ImGui.GetIO().MouseDelta.Y, -1f, 1f);
                var vel = _currentNote.Data.Velocity - delta;
                _currentNote.Data.Velocity = (SevenBitNumber)Math.Clamp(vel, SevenBitNumber.MinValue, SevenBitNumber.MaxValue);
                UiElement.Tooltip($"Velocity [{_currentNote.Data.NoteName.ToString().Replace("Sharp", "#")}{_currentNote.Data.Octave}]: {_currentNote.Data.Velocity}");
            }
            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _currentNote != null)
            {
                var vstPlugin = _midiTrack.Engine.PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
                vstPlugin?.SendNoteOff(0, _lastSentNoteNum, 0);
                //_midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOff(0, _lastSentNoteNum, 0);

                _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));

                // We select and release the drawed note
                _selectedNotes.Clear();
                _selectedNotes.Add(_currentNote);
                _currentNote = null;
            }

            // Notes moving
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left) && _currentNote == null && !_rightResizing && !_leftResizing && _movingNotes)
            {
                _lastSelectedRow ??= row;
                _lastSnapTick ??= SnapToGrid(tick);

                bool velocityChange = ImGui.IsKeyDown(ImGuiKey.ModAlt);
                if (velocityChange)
                {
                    float delta = Math.Clamp(ImGui.GetIO().MouseDelta.Y, -1f, 1f);
                    _selectedNotes.ForEach(note => {
                        var vel = note.Data.Velocity - delta;
                        note.Data.Velocity = (SevenBitNumber)Math.Clamp(vel, SevenBitNumber.MinValue, SevenBitNumber.MaxValue);                
                        UiElement.Tooltip($"Velocity [{note.Data.NoteName.ToString().Replace("Sharp", "#")}{note.Data.Octave}]: {note.Data.Velocity}");
                    });

                    if (delta != 0)
                    {
                        _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                    }
                }

                // Up and Down movement
                if (row < _lastSelectedRow && !velocityChange)
                {
                    _selectedNotes.ForEach(n => n.Data.NoteNumber = (SevenBitNumber)Math.Clamp(n.Data.NoteNumber + new SevenBitNumber(1), 21, 108));
                    HandleOverlappingNotes();
                    _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                    _lastSelectedRow = row;

                    if (_keysSound)
                    {
                        var vstPlugin = _midiTrack.Engine.PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
                        vstPlugin?.SendNoteOff(0, _lastSentNoteNum, 0);

                        vstPlugin?.SendNoteOn(0, RowToNoteNumber(row), 100);
                        //_midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOn(0, RowToNoteNumber(row), 100);
                        _lastSentNoteNum = RowToNoteNumber(row);
                    }
                }
                else if (row > _lastSelectedRow && !velocityChange)
                {
                    _selectedNotes.ForEach(n => n.Data.NoteNumber = (SevenBitNumber)Math.Clamp(n.Data.NoteNumber - new SevenBitNumber(1), 21, 108));
                    HandleOverlappingNotes();
                    _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                    _lastSelectedRow = row;

                    if (_keysSound)
                    {
                        var vstPlugin = _midiTrack.Engine.PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
                        vstPlugin?.SendNoteOff(0, _lastSentNoteNum, 0);

                        vstPlugin?.SendNoteOn(0, RowToNoteNumber(row), 100);
                        //_midiTrack.MidiEngine.VstChainSampleProvider.VstInstrument?.VstPlugin.SendNoteOn(0, RowToNoteNumber(row), 100);
                        _lastSentNoteNum = RowToNoteNumber(row);
                    }
                }

                // Right and Left movement
                if (SnapToGrid(tick) < _lastSnapTick && !velocityChange)
                {
                    _selectedNotes.ForEach(n => n.Data.Time = Math.Clamp(n.Data.Time - GetTicksInBar(), 0, long.MaxValue));
                    _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                    _lastSnapTick = SnapToGrid(tick);
                }
                else if (SnapToGrid(tick) > _lastSnapTick && !velocityChange)
                {
                    _selectedNotes.ForEach(n => n.Data.Time = Math.Clamp(n.Data.Time + GetTicksInBar(), 0, long.MaxValue));
                    _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                    _lastSnapTick = SnapToGrid(tick);
                }
            }
            else
            {
                _lastSelectedRow = null;
                _lastSnapTick = null;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _movingNotes)
            {
                _movingNotes = false;
            }

            _notesHovered.Clear();
        }

        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !TimeLineV2.IsPlaying())
        {
            var vstPlugin = _midiTrack.Engine.PluginChainSampleProvider.PluginInstrument?.GetPlugin<VstPlugin>();
            vstPlugin?.SendNoteOff(0, _lastSentNoteNum, 0);
        }
    }
    private Vector2? _selectionRectStart = null;
    private int? _lastSelectedRow = null;
    private long? _lastSnapTick = null;
    private bool _movingNotes;

    public void HandleOverlappingNotes()
    {
        foreach (var note in _selectedNotes.ToList())
        {
            foreach (var other in _notes.ToList())
            {
                if (note.Data != other.Data && note.Data.NoteNumber == other.Data.NoteNumber)
                {
                    long noteStart = note.Data.Time;
                    long noteEnd = note.Data.Time + note.Data.Length;
                    long otherStart = other.Data.Time;
                    long otherEnd = other.Data.Time + other.Data.Length;

                    // Check for overlap
                    if (noteStart < otherEnd && noteEnd > otherStart)
                    {
                        if (noteStart <= otherStart && noteEnd >= otherEnd)
                        {
                            // Selected note completely covers the other note, remove the other note
                            _notes.Remove(other);
                        }
                        else if (noteStart > otherStart && noteEnd < otherEnd)
                        {
                            // Selected note is inside the other note, split the other note
                            var data = new Note(other.Data.NoteNumber, otherEnd - noteEnd, noteEnd);
                            var newNote = new PNote(data);

                            newNote.Data.LengthChanged += (sender, e) =>
                            {
                                HandleOverlappingNotes();
                                _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                            };
                            newNote.Data.TimeChanged += (sender, e) =>
                            {
                                HandleOverlappingNotes();
                                _midiClip.UpdateClipData(new MidiClipData(ToMidiFile()));
                            };

                            _notes.Add(newNote);
                            other.Data.Length = noteStart - otherStart;
                        }
                        else if (noteStart <= otherStart)
                        {
                            // Selected note overlaps the start of the other note, shorten the other note
                            other.Data.Time = noteEnd;
                            other.Data.Length = otherEnd - noteEnd;
                        }
                        else
                        {
                            // Selected note overlaps the end of the other note, shorten the other note
                            other.Data.Length = noteStart - otherStart;
                        }
                    }
                }
            }
        }
    }

    private long GetTicksAtCursor()
    {
        Vector2 mousePos = ImGui.GetMousePos();
        Vector2 localPos = mousePos - _windowPos;

        // Apply scrolling offsets
        localPos.X += _scrollX;
        localPos.Y += _scrollY;

        float adjustedMousePosX = (int)localPos.X - KeyWidth;
        return PositionToTime(adjustedMousePosX);
    }

    private int GetRowNumberAtCursor()
    {
        Vector2 mousePos = ImGui.GetMousePos();
        Vector2 localPos = mousePos - _windowPos;

        // Apply scrolling offsets
        localPos.X += _scrollX;
        localPos.Y += _scrollY;

        return (int)(localPos.Y / (_vZoom * 10) / _noteHeight);
    }

    private bool IsBlackKey(int row)
    {
        int noteInOctave = row % 12;
        return noteInOctave == 1 || noteInOctave == 3 || noteInOctave == 6 || noteInOctave == 8 || noteInOctave == 10;
    }

    private int RowToNoteNumberDraw(int row) => 21 + row;
    private int RowToNoteNumber(int row) => 108 - row; // 24 = C0 and 21 = A0
    private int NoteNumberToRow(int noteNumber) => 108 - noteNumber;

    private string NoteNumberToName(int noteNumber)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = noteNumber / 12 - 1;
        string noteName = noteNames[noteNumber % 12];
        return $"{noteName}{octave}";
    }

    private NoteName RowToNoteName(int row)
    {
        int noteNumber = RowToNoteNumber(row);
        int noteInOctave = noteNumber % 12;

        return noteInOctave switch
        {
            0 => NoteName.C,
            1 => NoteName.CSharp,
            2 => NoteName.D,
            3 => NoteName.DSharp,
            4 => NoteName.E,
            5 => NoteName.F,
            6 => NoteName.FSharp,
            7 => NoteName.G,
            8 => NoteName.GSharp,
            9 => NoteName.A,
            10 => NoteName.ASharp,
            11 => NoteName.B,
            _ => throw new InvalidOperationException("Invalid note number.")
        };
    }

    private int RowToOctave(int row)
    {
        int noteNumber = RowToNoteNumber(row);
        return noteNumber / 12 - 1; // MIDI octave calculation
    }

    public MidiFile ToMidiFile()
    {
        // Create a new track chunk
        var trackChunk = new TrackChunk();

        // Create a list to hold all MIDI events with absolute time information
        var timedEvents = new List<(long AbsoluteTime, MidiEvent MidiEvent)>();

        // Preserve original not note events
        foreach (var timedEvent in _midiClip.MidiClipData.MidiFile.GetTimedEvents())
        {
            if (timedEvent.Event is NoteOnEvent || timedEvent.Event is NoteOffEvent)
                continue;

            timedEvents.Add((
                AbsoluteTime: timedEvent.Time,
                MidiEvent: timedEvent.Event
            ));
        }

        // Iterate through the notes and create NoteOn/NoteOff events
        foreach (var note in _notes)
        {
            // Add NoteOn event
            timedEvents.Add((
                AbsoluteTime: note.Data.Time,
                MidiEvent: new NoteOnEvent(note.Data.NoteNumber, note.Data.Velocity)
            ));

            // Add NoteOff event
            timedEvents.Add((
                AbsoluteTime: note.Data.Time + note.Data.Length,
                MidiEvent: new NoteOffEvent(note.Data.NoteNumber, (SevenBitNumber)0)
            ));
        }

        // Sort events by AbsoluteTime
        timedEvents = timedEvents.OrderBy(e => e.AbsoluteTime).ToList();

        // Calculate DeltaTime for each event
        long lastEventTime = 0;
        foreach (var (absoluteTime, midiEvent) in timedEvents)
        {
            midiEvent.DeltaTime = (int)(absoluteTime - lastEventTime);
            lastEventTime = absoluteTime;

            // Add the MIDI event to the track chunk
            trackChunk.Events.Add(midiEvent);
        }

        // Create the MIDI file and add the track chunk
        var midiFile = new MidiFile(trackChunk);

        // Preserve the original tempo map
        midiFile.ReplaceTempoMap(_midiClip.MidiClipData.MidiFile.GetTempoMap());

        return midiFile;
    }
}
