# Lumix
**... is a digital audio workstation (DAW) currently under development, with an Ableton Live heavily inspired ui and workflow.**

**This repository is intended to showcase progress, provide updates, and occasionally share work in progress versions of the project as it evolves.
Source code isn't available yet during development stage.**

---

Several open source libraries are being used:
- Interface is entirely developed using [dear-imgui](https://github.com/ocornut/imgui) with the [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) wrapper.
- Audio is handled by [NAudio](https://github.com/naudio/NAudio) library while midi by [DryWetMidi](https://github.com/melanchall/drywetmidi).
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
- Adaptive grid snapping
- Files sound/waveform preview
- Help info box

### Partially Working Features :hammer_and_wrench:
- **Volume meters**: working but need further changes and improvements
- **Metronome**: ui only
- **Piano roll**: notes placement and deletion (no move), horizontal and vertical zoom
- **Moving clips between tracks**: works for audio files, not for midi yet
- **Plugins index swap**: works with keyboard arrows only

### Not Yet Implemented Features :x:
- Track area selection
- Audio clip editing view
- Audio clips warping
- Plugins grouping
- Tracks grouping
- Parameters automations
- Custom audio/midi files folder paths
- Custom vst's folder paths
- Project saving, loading and export
---

### 15-01-2025
![Lumix 15-01-2025](https://github.com/user-attachments/assets/93d2e266-48d8-4aac-8461-66a9d5bb8939)

[Development playlist](https://www.youtube.com/playlist?list=PLskQuYoe4Bn8Aub8okcEeravu602E4kNM)

### Special Thanks To
- [dear-imgui](https://github.com/ocornut/imgui) and [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
- [NAudio](https://github.com/naudio/NAudio)
- [DryWetMidi](https://github.com/melanchall/drywetmidi)
- [VST.NET](https://github.com/obiwanjacobi/vst.net)
