using IconFonts;
using ImGuiNET;
using Lumix.FileDialogs;
using Lumix.ImGuiExtensions;
using Lumix.Views.Preferences.Audio;
using Lumix.Views.Preferences.Plugins;
using NAudio.Wave;
using System.Numerics;

namespace Lumix.Views.Preferences;

public static class PreferencesView
{
    private static bool _showView;
    public static bool ShowView {  get => _showView; set => _showView = value; }

    private static PreferencesTabs _selectedTab;
    private enum PreferencesTabs
    {
        LookAndFeel,
        Audio,
        Plugins
    }

    public static void Render()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 3);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4);
        ImGui.SetNextWindowSize(new(720, 720));
        ImGui.Begin("Preferences", ref _showView, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings);
        ImGui.PopStyleVar(2);

        ImGui.Columns(sizeof(PreferencesTabs), "preferences_columns", false);
        if (UiElement.SelectableColored($"Look and Feel {FontAwesome6.Paintbrush}", _selectedTab == PreferencesTabs.LookAndFeel))
        {
            _selectedTab = PreferencesTabs.LookAndFeel;
        }
        ImGui.NextColumn();
        if (UiElement.SelectableColored($"Audio {FontAwesome6.Computer}", _selectedTab == PreferencesTabs.Audio))
        {
            _selectedTab = PreferencesTabs.Audio;
        }
        ImGui.NextColumn();
        if (UiElement.SelectableColored($"Plugins {FontAwesome6.Plug}", _selectedTab == PreferencesTabs.Plugins))
        {
            _selectedTab = PreferencesTabs.Plugins;
        }
        ImGui.NextColumn();
        if (UiElement.SelectableColored("Other", false))
        {
            _selectedTab = PreferencesTabs.Plugins;
        }
        ImGui.Columns(1);
        ImGui.Dummy(new(0, 10));
        ImGui.BeginChild("preferences_container", ImGui.GetContentRegionAvail());

        switch (_selectedTab)
        {
            case PreferencesTabs.LookAndFeel:
                break;
            case PreferencesTabs.Audio:
                RenderAudioTab();
                break;
            case PreferencesTabs.Plugins:
                RenderPluginsTab();
                break;
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private static void RenderAudioTab()
    {
        Vector2 spacing = new(0, 5);

        ImGui.SeparatorText("Audio Device");
        ImGui.Dummy(spacing);

        ImGui.Columns(2, "Audio Device", false);

        ImGui.Text("Driver Type");
        ImGui.Spacing();
        ImGui.Text("Audio Device");
        
        switch (AudioSettings.AudioDriver)
        {
            case AudioDriver.Wasapi:
                {
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Text("Wasapi Latency");
                }
                break;  
            case AudioDriver.Asio:
                {
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Text("Channel Configuration");
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Text("Hardware Setup");
                }
                break;
        }

        ImGui.NextColumn();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.BeginCombo("##driver_type_combo", AudioSettings.AudioDriver.ToString(), ImGuiComboFlags.HeightLargest))
        {
            foreach (var driver in Enum.GetValues<AudioDriver>())
            {
                bool isAsioInstalled = AsioOut.GetDriverNames().Length > 0;
                var flags = (driver == AudioDriver.Asio && !isAsioInstalled) ? ImGuiSelectableFlags.Disabled : ImGuiSelectableFlags.None;

                if (ImGui.Selectable(driver.ToString(), false, flags))
                {
                    AudioSettings.SetDriver(driver);
                    AudioSettings.Init(true);
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.BeginCombo("##audio_device_combo", AudioSettings.DeviceName, ImGuiComboFlags.HeightLarge))
        {
            switch (AudioSettings.AudioDriver)
            {
                case AudioDriver.Wasapi:
                    {
                        foreach (var device in AudioSettings.WasapiDevices)
                        {
                            if (ImGui.Selectable(device.Key))
                            {
                                var wasapi = new WasapiOut(device.Value, NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, true, (int)AudioSettings.WasapiLatency);
                                AudioSettings.UpdateAudioOutput(wasapi, device.Key);
                            }
                        }
                    }
                    break;
                case AudioDriver.Asio:
                    {
                        foreach (var device in AsioOut.GetDriverNames())
                        {
                            if (ImGui.Selectable(device))
                            {
                                var asio = new AsioOut(device);
                                AudioSettings.UpdateAudioOutput(asio, asio.DriverName);
                            }
                        }
                    }
                    break;
            }
            ImGui.EndCombo();
        }

        ImGui.Spacing();
        UiElement.Button("Input Config", new(150, 25));
        ImGui.SameLine();
        UiElement.Button("Output Config", new(150, 25));
        ImGui.Spacing();

        if (AudioSettings.AudioDriver == AudioDriver.Wasapi)
        {
            ImGui.BeginDisabled();
            if (UiElement.DragSlider("ms##wasapi_latency", 100, ref AudioSettings.WasapiLatency, 1, 15, 500, "%.0f", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoInput))
            {
                if (AudioSettings.OutputDevice is WasapiOut)
                {
                    AudioSettings.UpdateAudioOutput(AudioSettings.OutputDevice, AudioSettings.DeviceName);
                }
            }
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                AudioSettings.WasapiLatency = 50;
                if (AudioSettings.OutputDevice is WasapiOut)
                {
                    AudioSettings.UpdateAudioOutput(AudioSettings.OutputDevice, AudioSettings.DeviceName);
                }
            }
            ImGui.EndDisabled();
        }
        else if (AudioSettings.AudioDriver == AudioDriver.Asio)
        {
            if (UiElement.Button("Hardware Setup", new(150, 25)))
            {
                if (AudioSettings.OutputDevice is AsioOut asio)
                    asio.ShowControlPanel();
            }
        }

        ImGui.Columns(1);

        ImGui.Spacing();
        ImGui.SeparatorText("Sample Rate");
        ImGui.Dummy(spacing);

        ImGui.Columns(2, "Sample Rate", false);

        ImGui.Text("In/Out Sample Rate");

        ImGui.NextColumn();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.BeginDisabled();
        if (ImGui.BeginCombo("##sample_rate_combo", AudioSettings.SampleRate.ToString(), ImGuiComboFlags.HeightLargest))
        {
            int[] sampleRates = new[] { 22050, 44100, 48000, 96000, 192000 };
            foreach (var rate in sampleRates)
            {
                if (ImGui.Selectable(rate.ToString(), AudioSettings.SampleRate == rate))
                {
                    AudioSettings.SetSampleRate(rate);
                    AudioSettings.UpdateAudioOutput(AudioSettings.OutputDevice, AudioSettings.DeviceName);
                }
            }
            ImGui.EndCombo();
        }
        ImGui.EndDisabled();

        ImGui.Columns(1);

        ImGui.Spacing();
        ImGui.SeparatorText("Latency");
        ImGui.Dummy(spacing);

        ImGui.Columns(2, "Latency", false);

        ImGui.Text("Buffer Size");
        ImGui.Spacing();
        ImGui.Text("Input Latency");
        ImGui.Spacing();
        ImGui.Text("Output Latency");

        ImGui.NextColumn();

        ImGui.Text("Not Implemented");
        ImGui.Spacing();
        ImGui.Text("Not Implemented");
        ImGui.Spacing();
        ImGui.Text("Not Implemented");
    }

    private static void RenderPluginsTab()
    {
        Vector2 spacing = new(0, 5);

        ImGui.SeparatorText("Plug-In Sources");
        ImGui.Dummy(spacing);

        ImGui.Columns(2, "Plug-In Sources", false);

        ImGui.Text("VST2 Plug-In Custom Folder");
        ImGui.Spacing();
        ImGui.TextDisabled(string.IsNullOrEmpty(PluginsPreferences.VST2PluginsPath) ? "No VST2 folder set" : PluginsPreferences.VST2PluginsPath);

        ImGui.NextColumn();

        if (UiElement.Button("Browse", new(100, 25)))
        {
            var dlg = new FolderPicker();
            dlg.InputPath = "C:\\";
            if (dlg.ShowDialog(Program._window.Handle) == true)
            {
                PluginsPreferences.VST2PluginsPath = dlg.ResultPath;
            }
        }

        ImGui.Columns(1);
        ImGui.Spacing();
        ImGui.SeparatorText("Plug-In Windows");
        ImGui.Dummy(spacing);

        ImGui.Columns(2, "Plug-In Windows", false);
        ImGui.Text("Multiple Plug-in Windows");
        ImGui.Spacing();
        ImGui.Text("Auto-Hide Plug-In Windows");
        ImGui.Spacing();
        ImGui.Text("Auto-Open Plug-In Windows");

        ImGui.NextColumn();

        ImGui.PushID("MultiplePluginWindows");
        if (UiElement.Toggle(PluginsPreferences.MultiplePluginWindows ? "On" : "Off", PluginsPreferences.MultiplePluginWindows, new(0.95f, 0.58f, 0.13f, 1f), new(50, 25)))
        {
            PluginsPreferences.MultiplePluginWindows = !PluginsPreferences.MultiplePluginWindows;
        }
        ImGui.PopID();
        ImGui.PushID("AutoHidePluginWindows");
        if (UiElement.Toggle(PluginsPreferences.AutoHidePluginWindows ? "On" : "Off", PluginsPreferences.AutoHidePluginWindows, new(0.95f, 0.58f, 0.13f, 1f), new(50, 25)))
        {
            PluginsPreferences.AutoHidePluginWindows = !PluginsPreferences.AutoHidePluginWindows;
        }
        ImGui.PopID();
        ImGui.PushID("AutoOpenPluginWindows");
        if (UiElement.Toggle(PluginsPreferences.AutoOpenPluginWindows ? "On" : "Off", PluginsPreferences.AutoOpenPluginWindows, new(0.95f, 0.58f, 0.13f, 1f), new(50, 25)))
        {
            PluginsPreferences.AutoOpenPluginWindows = !PluginsPreferences.AutoOpenPluginWindows;
        }
        ImGui.PopID();
    }
}
