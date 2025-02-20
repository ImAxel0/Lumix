<p align="center">
 <img src="https://i.imgur.com/PhXPmxf.png" width="100" height="100" />
</p>

# Lumix
**... is a digital audio workstation (DAW) currently under development, with an Ableton Live heavily inspired ui and workflow.**

> **This repository is intended to showcase progress, provide updates, and occasionally share work in progress versions of the project as it evolves.
Pull requests or any sort of help isn't accepted for the time being since the daw is still barebone.**

> **Source is provided as is. If compiling it, expect crashes, unfinished and partially working features.**

---

Several open source libraries are being used:
- Interface is entirely developed using [dear-imgui](https://github.com/ocornut/imgui) with the [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) wrapper.
- Audio is handled by [NAudio](https://github.com/naudio/NAudio) library and midi by [DryWetMidi](https://github.com/melanchall/drywetmidi).
- VST's support is possible by [VST.NET](https://github.com/obiwanjacobi/vst.net).

### Currently Working Features :ok_hand:
- Audio files playback
- Midi files playback
- Audio/Midi tracks rendering and common controls
- Audio/Midi clips rendering and basic controls
- Built-in plugins support
- VST2 plugins support
- Plugins chain
- Computer keyboard midi events
- Arrangement zoom
- Grid snapping
- Files sound/waveform preview
- Help info box

### Partially Working Features :hammer_and_wrench:
- **Volume meters**: working but need further changes and improvements
- **Metronome**: ui only
- **Piano roll**: notes placement and deletion, movement with arrow keys, horizontal and vertical zoom
- **Plugins index swap**: works with keyboard arrows only

### Not Yet Implemented Features :x:
- Track area selection
- Audio clip editing view
- Audio clips warping
- Plugins grouping
- Tracks grouping
- Parameters automations
- Project saving, loading and export
---

### 20-02-2025
Source code is now available to compile from all users who would like to try it.

### 15-01-2025
![Lumix 15-01-2025](https://github.com/user-attachments/assets/93d2e266-48d8-4aac-8461-66a9d5bb8939)

[Development playlist](https://www.youtube.com/playlist?list=PLskQuYoe4Bn8Aub8okcEeravu602E4kNM)

### Special Thanks To
- [dear-imgui](https://github.com/ocornut/imgui) and [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
- [NAudio](https://github.com/naudio/NAudio)
- [DryWetMidi](https://github.com/melanchall/drywetmidi)
- [VST.NET](https://github.com/obiwanjacobi/vst.net)
