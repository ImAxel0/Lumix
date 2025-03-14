using Lumix.Plugins;
using Lumix.Plugins.BuiltIn;
using Lumix.Plugins.VST;
using IconFonts;
using ImGuiNET;
using System.Numerics;
using Vanara.PInvoke;
using Lumix.Tracks;
using Lumix.Tracks.MidiTracks;
using Lumix.Views.Sidebar;
using Lumix.ImGuiExtensions;

namespace Lumix.Views;

public static class DevicesView
{
    public static Track SelectedTrack { get; set; }
    public static List<IAudioProcessor> SelectedPlugins { get; set; } = new();

    private static float _rectsSpacing = 10f;
    private static bool _windowHovered;
    private static float _windowScrollX;
    private static float _windowScrollMaxX;
    private static bool _hasDropped = false; // Flag to prevent multiple additions during one drop

    private static void ListenForShortcuts()
    {
        if (_windowHovered)
        {
            // Panning controls
            if (ImGui.IsMouseDown(ImGuiMouseButton.Middle))
            {
                var io = ImGui.GetIO();
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                ImGui.SetScrollX(-io.MouseDelta.X + _windowScrollX);
            }
        }

        // Delete all selected plugins from chain
        if (ImGui.IsKeyPressed(ImGuiKey.Delete, false))
        {
            SelectedPlugins.ForEach(plugin => plugin.DeleteRequested = true);
        }

        // Duplicated plugins
        if (ImGui.IsKeyDown(ImGuiKey.ModCtrl) && ImGui.IsKeyPressed(ImGuiKey.D, false))
        {
            SelectedPlugins.ForEach(plugin => plugin.DuplicateRequested = true);
        }
    }

    private static void RenderPluginRect(IAudioProcessor plugin, Vector2 rectSize)
    {
        var vstPlugin = plugin.GetPlugin<VstPlugin>();
        if (vstPlugin == null) // If plugin is built in
        {
            var p = plugin as BuiltInPlugin;
            p.RenderRect(plugin);
        }
        else if (vstPlugin != null) // If plugin is external
        {
            bool selected = SelectedPlugins.Contains(plugin);
            Vector4 menuBarCol = selected ? ImGuiTheme.SelectionCol : new Vector4(0.28f, 0.28f, 0.28f, 1);
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, menuBarCol);
            ImGui.BeginChild($"plugin_rect{vstPlugin.PluginId}", rectSize, ImGuiChildFlags.Border, ImGuiWindowFlags.MenuBar);

            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) && !ImGui.IsAnyItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (ImGui.IsKeyDown(ImGuiKey.ModCtrl))
                {
                    SelectedPlugins.Add(plugin);
                }
                else
                {
                    SelectedPlugins.Clear();
                    SelectedPlugins.Add(plugin);
                }
            }

            if (ImGui.BeginMenuBar())
            {
                var textCol = selected ? new Vector4(0, 0, 0, 1) : ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

                if (UiElement.RoundToggle(plugin.Enabled, new Vector4(0.95f, 0.58f, 0.13f, 1f)))
                {
                    plugin.Toggle();
                }

                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 15f);
                ImGui.TextColored(textCol, $"{FontAwesome6.Wrench}");
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    vstPlugin.OpenPluginWindow();
                }
                ImGui.PopStyleVar();

                ImGui.TextColored(textCol, vstPlugin.PluginName);
                ImGui.EndMenuBar();
            }

            string vstType = vstPlugin.PluginType == VstType.VST ? "Fx" : "Instrument";
            ImGui.Text($"VST Type: {vstType}");
            ImGui.Text($"Parameters: {vstPlugin.PluginContext.PluginInfo.ParameterCount}");
            ImGui.Text($"In: {vstPlugin.PluginContext.PluginInfo.AudioInputCount}");
            ImGui.SameLine();
            ImGui.Text($"Out: {vstPlugin.PluginContext.PluginInfo.AudioOutputCount}");

            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
    }

    private static void ListenForPluginDrop()
    {
        Vector2 pos = ImGui.GetCursorPos();
        ImGui.Dummy(ImGui.GetContentRegionAvail());
        if (ImGui.BeginDragDropTarget())
        {
            ImGui.AcceptDragDropPayload("PLUGIN_DRAG");
            ImGui.AcceptDragDropPayload("BUILTIN_PLUGIN_DRAG");
            if (!string.IsNullOrEmpty(SidebarView.DraggedFilePath) || SidebarView.DraggedBuiltInPlugin != null && !_hasDropped)
            {
                if (SidebarView.DraggedBuiltInPlugin != null) // Is built in plugin
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        var pluginInstance = SidebarView.DraggedBuiltInPlugin as IAudioProcessor;
                        if (SelectedTrack != null)
                        {
                            SelectedTrack.Engine.PluginChainSampleProvider.AddPlugin(pluginInstance);
                        }
                        else
                        {
                            User32.MessageBox(IntPtr.Zero, "Can't add plugin here", "Error", User32.MB_FLAGS.MB_ICONERROR | User32.MB_FLAGS.MB_TOPMOST);
                        }
                        SidebarView.DraggedBuiltInPlugin = null;
                        _hasDropped = true;
                    }
                }
                else if (Path.GetExtension(SidebarView.DraggedFilePath) == ".dll")
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        var vst = new VstPlugin(SidebarView.DraggedFilePath);
                        var vstProcessor = new VstAudioProcessor(vst);
                        if (SelectedTrack != null && vst.PluginType != VstType.VSTi)
                        {
                            SelectedTrack.Engine.PluginChainSampleProvider.AddPlugin(vstProcessor);
                        }
                        else if (SelectedTrack != null && SelectedTrack is MidiTrack)
                        {
                            SelectedTrack.Engine.PluginChainSampleProvider.AddPlugin(vstProcessor);
                        }
                        else
                        {
                            vst.Dispose();
                            User32.MessageBox(IntPtr.Zero, "Can't add vst here", "Error", User32.MB_FLAGS.MB_ICONERROR | User32.MB_FLAGS.MB_TOPMOST);
                        }
                        _hasDropped = true;
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
    }

    private static void HandlePluginSwap(IAudioProcessor plugin)
    {
        // Enable swapping only if one plugin is selected in chain and check if said plugin is param plugin
        if (SelectedPlugins.Count == 1 && SelectedPlugins[0] == plugin)
        {
            if (SelectedTrack != null)
            {
                var _vstFxPlugins = SelectedTrack.Engine.PluginChainSampleProvider.FxPlugins;
                int pluginIndex = _vstFxPlugins.IndexOf(plugin);
                if (pluginIndex >= 0)
                {
                    if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow, false) && pluginIndex > 0)
                    {
                        var temp = _vstFxPlugins[pluginIndex];
                        _vstFxPlugins[pluginIndex] = _vstFxPlugins[pluginIndex - 1];
                        _vstFxPlugins[pluginIndex - 1] = temp;
                    }
                    else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow, false) && pluginIndex < _vstFxPlugins.Count - 1)
                    {
                        var temp = _vstFxPlugins[pluginIndex];
                        _vstFxPlugins[pluginIndex] = _vstFxPlugins[pluginIndex + 1];
                        _vstFxPlugins[pluginIndex + 1] = temp;
                    }
                }
            }
        }
    }

    public static void Render()
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.18f, 0.18f, 0.18f, 1f));
        if (ImGui.BeginChild("track_devices", ImGui.GetContentRegionAvail(), ImGuiChildFlags.None, ImGuiWindowFlags.None))
        {
            ListenForPluginDrop();

            _windowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);
            _windowScrollX = ImGui.GetScrollX();
            _windowScrollMaxX = ImGui.GetScrollMaxX();
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            ListenForShortcuts();

            Vector3 border = new Vector3(0.13f, 0.14f, 0.17f) * 0.7f;
            ImGui.GetForegroundDrawList().AddRect(windowPos, windowPos + windowSize,
                ImGui.GetColorU32(new Vector4(border.X, border.Y, border.Z, 1.00f)), 4f, ImDrawFlags.None, 4f);

            // Deselect all plugin windows
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsKeyDown(ImGuiKey.ModCtrl))
            {
                SelectedPlugins.Clear();
            }

            if (SelectedTrack != null)
            {
                // Remove deleted plugins from chain
                var deletedPlugins = SelectedTrack.Engine.PluginChainSampleProvider.FxPlugins.FindAll(plug => plug.DeleteRequested);
                deletedPlugins.ForEach(del => SelectedTrack.Engine.PluginChainSampleProvider.RemovePlugin(del));

                Vector2 space = ImGui.GetContentRegionAvail();
                Vector4 bgCol = new(0.22f, 0.22f, 0.22f, 1);

                // Render plugins rects
                ImGui.PushStyleColor(ImGuiCol.ChildBg, bgCol * 0.6f);
                ImGui.BeginChild("plugin_rects_background", Vector2.Zero, ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.Border);
                Vector2 rectSize = new(ImGui.GetContentRegionAvail().Y);

                var instrumentPlugin = SelectedTrack.Engine.PluginChainSampleProvider.PluginInstrument;
                float instrumentWidth = instrumentPlugin == null ? 0 : rectSize.X;
                int pluginCount = SelectedTrack.Engine.PluginChainSampleProvider.FxPlugins.Count;

                ImGui.PushStyleColor(ImGuiCol.ChildBg, bgCol);
                if (instrumentPlugin != null)
                {
                    RenderPluginRect(instrumentPlugin, rectSize);
                    ImGui.SameLine(0, _rectsSpacing);

                    // Remove deleted instrument 
                    if (instrumentPlugin.DeleteRequested)
                    {
                        SelectedTrack.Engine.PluginChainSampleProvider.RemovePlugin(instrumentPlugin);
                    }
                }

                foreach (var plugin in SelectedTrack.Engine.PluginChainSampleProvider.FxPlugins.ToList())
                {
                    HandlePluginSwap(plugin);
                    RenderPluginRect(plugin, rectSize);
                    ImGui.SameLine(0, _rectsSpacing);
                }
                ImGui.EndChild();
                ImGui.PopStyleColor(2);
                int maxCount = SelectedTrack.TrackType == TrackType.Audio ? 6 : 5;
                if (pluginCount <= maxCount)
                {
                    string label = SelectedTrack.TrackType == TrackType.Audio ? "Drop Audio Effects Here" : "Drop Instruments or Audio Effects Here";
                    ImGui.SetCursorPos((space + new Vector2(pluginCount * rectSize.X, 0) - ImGui.CalcTextSize(label)) / 2);
                    ImGui.Text(label);
                }
            }

            // Custom scrollbar line
            float scrollbarStartX = windowPos.X + _windowScrollX / _windowScrollMaxX * windowSize.X;
            //float offsetLength = _windowScrollX >= _windowScrollMaxX - _windowScrollMaxX / 2 ? -_windowScrollMaxX * 0.5f : _windowScrollMaxX * 0.5f;
            float offsetLength = _windowScrollX >= _windowScrollMaxX - _windowScrollMaxX / 2 ? -100 : 100;
            float scrollbarEndX = scrollbarStartX + offsetLength;
            ImGui.GetWindowDrawList().AddLine(new Vector2(scrollbarStartX, windowPos.Y + windowSize.Y),
                new Vector2(scrollbarEndX, windowPos.Y + windowSize.Y),
                ImGui.GetColorU32(Vector4.One), 10);

            ImGui.EndChild();
        }
        ImGui.PopStyleColor();
    }
}
