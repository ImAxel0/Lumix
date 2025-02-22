using Lumix.Plugins.BuiltIn;
using IconFonts;
using ImGuiNET;
using Melanchall.DryWetMidi.Core;
using System.Numerics;
using System.Reflection;
using Lumix.Clips.MidiClips;
using Lumix.Views.Sidebar.Preview;
using Lumix.Clips.Renderers;
using Lumix.ImGuiExtensions;
using Lumix.Views.Preferences.Plugins;
using Lumix.Views.Preferences.AudioFolders;
using Lumix.FileDialogs;

namespace Lumix.Views.Sidebar;

public enum BrowserTabs
{
    Sounds,
    Drums,
    Instruments,
    AudioEffects,
    Plugins,
    AudioFolder
}

public class SidebarView
{
    public static BrowserTabs ActiveTab { get; set; } = BrowserTabs.Sounds;
    private static string _searchBuffer = string.Empty;
    private static bool _soundPreview = true;
    private static bool _sortAscending = true;
    private static bool _hidden;
    private static float _waveformWidth;
    private static (List<float> samples, float[] peaks, float[] valleys) _waveformData;
    private static MidiClipData _midiClipDataPreview;
    private static bool _lastPreviewIsAudio;

    public static void Render()
    {
        if (_hidden)
        {
            if (ImGui.Button(FontAwesome6.CaretDown, new Vector2(25)))
            {
                _hidden = false;
            }
            InfoBox.SetInfoData("Hide Browser toggle", "Hide or show browser tab.");
            return;
        }
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.18f, 0.18f, 0.18f, 1f));
        if (ImGui.BeginChild("sidebar", new(ImGui.GetContentRegionAvail().X * 25 / 100, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border,
             ImGuiWindowFlags.NoSavedSettings))
        {
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            Vector3 border = new Vector3(0.13f, 0.14f, 0.17f) * 0.7f;
            ImGui.GetForegroundDrawList().AddRect(windowPos, windowPos + windowSize,
                ImGui.GetColorU32(new Vector4(border.X, border.Y, border.Z, 1.00f)), 4f, ImDrawFlags.None, 4f);

            if (ImGui.BeginChild("sidebar_columns", new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 70))) // 70 was: / 1.07f
            {
                if (ImGui.Button(FontAwesome6.CaretRight, new Vector2(25)))
                {
                    _hidden = true;
                }
                InfoBox.SetInfoData("Hide Browser toggle", "Hide or show browser tab.");
                ImGui.SameLine();
                if (ImGui.BeginChild("Search bar", new(ImGui.GetContentRegionAvail().X, 30))) // 30 was ImGui.GetContentRegionAvail().Y / 30
                {
                    Vector2 seachbarPos = ImGui.GetCursorScreenPos();
                    Vector2 searchbarSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
                    Vector4 borderColor = new Vector4(0, 0, 0, 1);

                    ImGui.SetNextItemWidth(searchbarSize.X);
                    ImGui.InputTextWithHint($"##search_bar", $"Search (Ctrl + F)", ref _searchBuffer, 1000, ImGuiInputTextFlags.AutoSelectAll);
                    ImGui.GetWindowDrawList().AddRect(seachbarPos, seachbarPos + searchbarSize, ImGui.GetColorU32(borderColor));
                    InfoBox.SetInfoData("Search Bar", "Allows to find files in browser.");
                }
                ImGui.EndChild();

                ImGui.SeparatorText($"Browser {FontAwesome6.FolderOpen}");
                ImGui.Columns(2, "sidebar_column", true);
                ImGui.SetColumnWidth(0, 160);

                int spacing = 5;
                ImGui.TextColored(new(.8f, .8f, .8f, 1), "Collections");
                ImGui.TextColored(new Vector4(1.00f, 0.02f, 0.02f, 1), FontAwesome6.Square);
                ImGui.SameLine(0, spacing);
                ImGui.Selectable($"Favorites");
                ImGui.TextColored(new Vector4(1.00f, 0.65f, 0.16f, 1), FontAwesome6.Square);
                ImGui.SameLine(0, spacing);
                ImGui.Selectable($"Orange");
                ImGui.TextColored(new Vector4(1.00f, 0.94f, 0.20f, 1), FontAwesome6.Square);
                ImGui.SameLine(0, spacing);
                ImGui.Selectable($"Yellow");
                ImGui.TextColored(new Vector4(0.15f, 1.00f, 0.66f, 1), FontAwesome6.Square);
                ImGui.SameLine(0, spacing);
                ImGui.Selectable($"Green");
                ImGui.TextColored(new Vector4(0.06f, 0.64f, 0.93f, 1), FontAwesome6.Square);
                ImGui.SameLine(0, spacing);
                ImGui.Selectable($"Blue");
                ImGui.TextColored(new Vector4(0.72f, 0.55f, 1.00f, 1), FontAwesome6.Square);
                ImGui.SameLine(0, spacing);
                ImGui.Selectable($"Purple");
                ImGui.TextColored(new Vector4(0.66f, 0.66f, 0.66f, 1), FontAwesome6.Square);
                ImGui.SameLine(0, spacing);
                ImGui.Selectable($"Gray");

                ImGui.NewLine();

                ImGui.TextColored(new(.8f, .8f, .8f, 1), "Categories");
                if (UiElement.SelectableColored($"{FontAwesome6.Music} Sounds", ActiveTab == BrowserTabs.Sounds))
                    ActiveTab = BrowserTabs.Sounds;
                if (UiElement.SelectableColored($"{FontAwesome6.Drum} Drums", ActiveTab == BrowserTabs.Drums))
                    ActiveTab = BrowserTabs.Drums;
                if (UiElement.SelectableColored($"{FontAwesome6.Guitar} Instruments", ActiveTab == BrowserTabs.Instruments))
                    ActiveTab = BrowserTabs.Instruments;
                if (UiElement.SelectableColored($"{FontAwesome6.Burst} Audio Effects", ActiveTab == BrowserTabs.AudioEffects))
                    ActiveTab = BrowserTabs.AudioEffects;
                if (UiElement.SelectableColored($"{FontAwesome6.Plug} Plug-Ins", ActiveTab == BrowserTabs.Plugins))
                    ActiveTab = BrowserTabs.Plugins;

                ImGui.NewLine();

                ImGui.TextColored(new(.8f, .8f, .8f, 1), "Places");

                foreach (var folder in AudioFolders.FoldersPath)
                {
                    if (UiElement.SelectableColored($"{FontAwesome6.Folder} {new DirectoryInfo(folder).Name}", AudioFolders.SelectedFolder == folder 
                        && ActiveTab == BrowserTabs.AudioFolder))
                    {
                        AudioFolders.SelectFolder(folder);
                        ActiveTab = BrowserTabs.AudioFolder;
                    }
                }

                ImGui.Text($"{FontAwesome6.FolderPlus} Add Folder...");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        var dlg = new FolderPicker();
                        dlg.InputPath = "C:\\";
                        if (dlg.ShowDialog(Program._window.Handle) == true)
                        {
                            if (!AudioFolders.FoldersPath.Contains(dlg.ResultPath))
                                AudioFolders.FoldersPath.Add(dlg.ResultPath);
                        }
                    }
                }

                ImGui.NextColumn();

                ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new Vector4(0.12f, 0.12f, 0.12f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.18f, 0.18f, 0.18f, 1f));
                if (ImGui.BeginChild("sidebar_right", ImGui.GetContentRegionAvail(), ImGuiChildFlags.None, ImGuiWindowFlags.MenuBar))
                {
                    //Vector2 separatorStart = new Vector2(ImGui.GetWindowPos().X + 1, ImGui.GetWindowPos().Y);
                    //Vector2 separatorEnd = new Vector2(ImGui.GetWindowPos().X + 1, ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y);
                    //ImGui.GetWindowDrawList().AddLine(separatorStart, separatorEnd, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), 2f);

                    string _filterIcon = !_sortAscending ? FontAwesome6.ArrowUpZA : FontAwesome6.ArrowDownAZ;

                    ImGui.BeginMenuBar();
                    var iconPos = ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(_filterIcon).X;
                    if (ImGui.Selectable($"Name##name filter"))
                    {
                        _sortAscending = !_sortAscending;
                    }
                    InfoBox.SetInfoData("Filter", "Toggles alphabetic order.");
                    ImGui.SetCursorPosX(iconPos);
                    ImGui.Text(_filterIcon);
                    ImGui.EndMenuBar();
                    ImGui.PopStyleColor(2);

                    switch (ActiveTab)
                    {
                        case BrowserTabs.Sounds:
                            ImGui.TextDisabled("Not implemented");
                            break;
                        case BrowserTabs.Drums:
                            ImGui.TextDisabled("Not implemented");
                            break;
                        case BrowserTabs.Instruments:
                            ImGui.TextDisabled("Not implemented");
                            break;
                        case BrowserTabs.AudioEffects:
                            foreach (var category in Enum.GetValues<BuiltInCategory>())
                                ShowAudioEffectsTreeNodes(category);
                            break;
                        case BrowserTabs.Plugins:
#if LOCAL_DEV
                            string pluginsPath = "C:\\Program Files\\Steinberg\\vstplugins";
#else
                            string pluginsPath = PluginsPreferences.VST2PluginsPath;
#endif
                            if (!string.IsNullOrWhiteSpace(pluginsPath))
                            {
                                ShowPluginsDirectory(pluginsPath);
                            }
                            else
                                ImGui.TextWrapped("Add a VST2 plugins custom folder from View->Preferences->Plugins");
                            break;
                        case BrowserTabs.AudioFolder:
                            ShowDirectory(AudioFolders.SelectedFolder);
                            break;
                    }

                    ImGui.EndChild();
                }

                ImGui.EndChild();
            }

            ImGui.SeparatorText("Preview Tab");
            string icon = _soundPreview ? FontAwesome6.VolumeHigh : FontAwesome6.VolumeXmark;
            if (UiElement.Toggle(icon, _soundPreview, new Vector4(0.95f, 0.58f, 0.13f, 1f), new(30)))
            {
                _soundPreview = !_soundPreview;
            }
            ImGui.SameLine();
            if (ImGui.BeginChild("sound_waveform_preview", new Vector2(0, 30), ImGuiChildFlags.FrameStyle))
            {
                _waveformWidth = ImGui.GetContentRegionAvail().X;

                if (_lastPreviewIsAudio)
                {
                    if (_waveformData.peaks != null)
                    {
                        WaveformRenderer.RenderWaveform((_waveformData.peaks, _waveformData.valleys), ImGui.GetCursorScreenPos(), _waveformWidth, ImGui.GetContentRegionAvail().Y, _soundPreview);
                    }
                }
                else
                {
                    if (_midiClipDataPreview != null)
                    {
                        MidiRenderer.RenderMidiDataPreview(_midiClipDataPreview, ImGui.GetCursorScreenPos(), _waveformWidth, ImGui.GetContentRegionAvail().Y);
                    }
                }
            }
            ImGui.EndChild(); // waveform
            ImGui.EndChild(); // outer
        }
        ImGui.PopStyleColor();
    }

    private static void ShowDirectory(string path)
    {
        var drawList = ImGui.GetWindowDrawList();
        var folderTextPos = ImGui.GetCursorScreenPos() + new Vector2(28, 0);
        // Create a TreeNode for the current directory
        Fontaudio.Push();
        bool treeOpen = ImGui.TreeNodeEx($"{Fontaudio.Close} {Path.GetFileName(path)}", ImGuiTreeNodeFlags.SpanFullWidth);
        Fontaudio.Pop();
        if (treeOpen)
        {
            if (ImGui.IsItemHovered())
            {
                Fontaudio.Push();
                drawList.AddText(folderTextPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Fontaudio.Close} {Path.GetFileName(path)}");
                Fontaudio.Pop();
            }

            var directories = _sortAscending
                ? Directory.GetDirectories(path).OrderBy(d => Path.GetFileName(d))
                : Directory.GetDirectories(path).OrderByDescending(d => Path.GetFileName(d));

            foreach (var directory in directories)
            {
                ShowDirectory(directory);
            }

            // Get all files in the current directory
            string[] extensions = { "*.wav", "*.mp3", "*.mid" };
            List<string> files = new();
            foreach (var extension in extensions)
            {
                files.AddRange(_sortAscending
                    ? Directory.GetFiles(path, extension).OrderBy(f => Path.GetFileName(f)).ToList()
                    : Directory.GetFiles(path, extension).OrderByDescending(f => Path.GetFileName(f)).ToList());
            }

            // Iterate through the sorted files
            foreach (var file in files)
            {
                // searchbar filter
                if (!Path.GetFileNameWithoutExtension(file).ToLower().Contains(_searchBuffer.ToLower()) && !string.IsNullOrWhiteSpace(_searchBuffer))
                    continue;

                if (Path.GetExtension(file) == ".mid")
                {
                    var textPos = ImGui.GetCursorScreenPos();
                    if (ImGui.Selectable($"{FontAwesome6.BarsStaggered} {Path.GetFileName(file)}", false, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        _lastPreviewIsAudio = false;
                        if (_soundPreview)
                        {
                            MidiPreviewEngine.PlayMidiPreview(file);
                        }

                        _midiClipDataPreview = new MidiClipData(MidiFile.Read(file));
                    }
                    if (ImGui.IsItemHovered())
                    {
                        drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{FontAwesome6.BarsStaggered} {Path.GetFileName(file)}");
                    }

                    // Start the drag operation if the item is clicked and dragged
                    if (ImGui.BeginDragDropSource())
                    {
                        DraggedFilePath = file; // Track the file being dragged
                        ImGui.SetDragDropPayload("CLIP", IntPtr.Zero, 0); // Set a payload for the drag event
                        ImGui.Text($"{FontAwesome6.BarsStaggered} {Path.GetFileName(file)}"); // Display the file being dragged
                        ImGui.EndDragDropSource();
                    }
                }
                else
                {
                    var textPos = ImGui.GetCursorScreenPos();
                    Fontaudio.Push();
                    if (ImGui.Selectable($"{Fontaudio.LogoAudiobus} {Path.GetFileName(file)}", false, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        _lastPreviewIsAudio = true;
                        if (_soundPreview)
                        {
                            AudioPreviewEngine.Instance.PlaySound(file, true);
                        }

                        _waveformData = WaveformRenderer.GetWaveformPeaks(file, (int)_waveformWidth);
                    }
                    if (ImGui.IsItemHovered())
                    {
                        drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Fontaudio.LogoAudiobus} {Path.GetFileName(file)}");
                    }

                    // Start the drag operation if the item is clicked and dragged
                    if (ImGui.BeginDragDropSource())
                    {
                        DraggedFilePath = file; // Track the file being dragged
                        ImGui.SetDragDropPayload("CLIP", IntPtr.Zero, 0); // Set a payload for the drag event
                        ImGui.Text($"{Fontaudio.LogoAudiobus} {Path.GetFileName(file)}"); // Display the file being dragged
                        ImGui.EndDragDropSource();
                    }
                    Fontaudio.Pop();
                }
            }

            ImGui.TreePop(); // Close the TreeNode
        }
        if (ImGui.IsItemHovered() && !treeOpen)
        {
            Fontaudio.Push();
            drawList.AddText(folderTextPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Fontaudio.Close} {Path.GetFileName(path)}");
            Fontaudio.Pop();
        }
    }

    private static void ShowAudioEffectsTreeNodes(BuiltInCategory category)
    {
        var drawList = ImGui.GetWindowDrawList();
        var folderTextPos = ImGui.GetCursorScreenPos() + new Vector2(28, 0);
        // Create a TreeNode for the category
        Fontaudio.Push();
        bool treeOpen = ImGui.TreeNodeEx($"{Fontaudio.Close} {category}", ImGuiTreeNodeFlags.SpanFullWidth);
        Fontaudio.Pop();
        if (treeOpen)
        {
            if (ImGui.IsItemHovered())
            {
                Fontaudio.Push();
                drawList.AddText(folderTextPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Fontaudio.Close} {category}");
                Fontaudio.Pop();
            }

            // Iterate through the built in plugins of the category
            var plugins = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BuiltInPlugin)));

            Fontaudio.Push();
            foreach (var plug in plugins)
            {
                if (Activator.CreateInstance(plug) is BuiltInPlugin pluginInstance)
                {
                    // Skip selectable rendering if plugin isn't of current category
                    if (pluginInstance.Category != category)
                        continue;

                    // searchbar filter
                    if (!Path.GetFileNameWithoutExtension(pluginInstance.PluginName).ToLower().Contains(_searchBuffer.ToLower()) && !string.IsNullOrWhiteSpace(_searchBuffer))
                        continue;

                    var textPos = ImGui.GetCursorScreenPos();
                    ImGui.Selectable($"{Fontaudio.SquareswitchOff} {pluginInstance.PluginName}");
                    if (ImGui.IsItemHovered())
                    {
                        drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Fontaudio.SquareswitchOff} {pluginInstance.PluginName}");
                    }

                    // Start the drag operation if the item is clicked and dragged
                    if (ImGui.BeginDragDropSource())
                    {
                        DraggedBuiltInPlugin = pluginInstance;
                        ImGui.SetDragDropPayload("BUILTIN_PLUGIN_DRAG", IntPtr.Zero, 0); // Set a payload for the drag event
                        ImGui.Text($"{Fontaudio.SquareswitchOff} {pluginInstance.PluginName}"); // Display the file being dragged
                        ImGui.EndDragDropSource();
                    }
                }
            }
            Fontaudio.Pop();

            ImGui.TreePop(); // Close the TreeNode
        }
        if (ImGui.IsItemHovered() && !treeOpen)
        {
            Fontaudio.Push();
            drawList.AddText(folderTextPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Fontaudio.Close} {category}");
            Fontaudio.Pop();
        }
    }

    private static void ShowPluginsDirectory(string path)
    {
        var drawList = ImGui.GetWindowDrawList();
        var folderTextPos = ImGui.GetCursorScreenPos() + new Vector2(28, 0);
        // Create a TreeNode for the current directory
        bool treeOpen = ImGui.TreeNodeEx($"{Path.GetFileName(path)}", ImGuiTreeNodeFlags.SpanFullWidth);
        if (treeOpen)
        {
            if (ImGui.IsItemHovered())
            {
                drawList.AddText(folderTextPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Path.GetFileName(path)}");
            }

            var directories = _sortAscending
                ? Directory.GetDirectories(path).OrderBy(d => Path.GetFileName(d))
                : Directory.GetDirectories(path).OrderByDescending(d => Path.GetFileName(d));

            foreach (var directory in directories)
            {
                ShowPluginsDirectory(directory);
            }

            // Get all files in the current directory
            string[] extensions = { "*.dll" };
            List<string> files = new();
            foreach (var extension in extensions)
            {
                files.AddRange(_sortAscending
                    ? Directory.GetFiles(path, extension).OrderBy(f => Path.GetFileName(f)).ToList()
                    : Directory.GetFiles(path, extension).OrderByDescending(f => Path.GetFileName(f)).ToList());
            }

            // Iterate through the sorted files
            foreach (var file in files)
            {
                // searchbar filter
                if (!Path.GetFileNameWithoutExtension(file).ToLower().Contains(_searchBuffer.ToLower()) && !string.IsNullOrWhiteSpace(_searchBuffer))
                    continue;

                if (Path.GetExtension(file) == ".dll")
                {
                    var textPos = ImGui.GetCursorScreenPos();
                    Fontaudio.Push();
                    if (ImGui.Selectable($"{Fontaudio.LogoVst} {Path.GetFileName(file)}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        // add plugin to selected track
                    }

                    if (ImGui.IsItemHovered())
                    {
                        drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Fontaudio.LogoVst} {Path.GetFileName(file)}");
                    }

                    // Start the drag operation if the item is clicked and dragged
                    if (ImGui.BeginDragDropSource())
                    {
                        DraggedFilePath = file; // Track the file being dragged
                        ImGui.SetDragDropPayload("PLUGIN_DRAG", IntPtr.Zero, 0); // Set a payload for the drag event
                        ImGui.Text($"{Fontaudio.LogoVst} {Path.GetFileName(file)}"); // Display the file being dragged
                        ImGui.EndDragDropSource();
                    }
                    Fontaudio.Pop();
                }
            }

            ImGui.TreePop(); // Close the TreeNode
        }
        if (ImGui.IsItemHovered() && !treeOpen)
        {
            drawList.AddText(folderTextPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1f)), $"{Path.GetFileName(path)}");
        }
    }

    public static string DraggedFilePath { get; set; }
    public static BuiltInPlugin DraggedBuiltInPlugin { get; set; }
}
