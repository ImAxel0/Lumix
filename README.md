<p align="center">
 <img src="https://i.imgur.com/PhXPmxf.png" width="150" height="150" />
</p>

# Lumix ![GitHub License](https://img.shields.io/github/license/ImAxel0/Lumix) [![Scc Count Badge](https://sloc.xyz/github/ImAxel0/Lumix/?category=code)](https://github.com/ImAxel0/Lumix/)

### **... is a digital audio workstation (DAW) currently under development, with an Ableton Live heavily inspired ui and workflow.**

> **This repository is intended to showcase progress, provide updates, and occasionally share work in progress versions of the project as it evolves.
Pull requests or any sort of contribution isn't accepted for the time being since the daw is still barebone.**

> **Source is provided as is. If compiling it, expect crashes, unfinished and partially working features. For latest source commits see the [Development](https://github.com/ImAxel0/Lumix/tree/Development) branch**

> **Project must be compiled either as x86/x64. If compiling in debug mode, remove LOCAL_DEV constant define inside .csproj file**

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
- **Piano roll**: placement, deletion, time and length change (partially)  
- **Plugins index swap**: works with keyboard arrows only
- **Tracks index swap**: works with gui buttons
- **Track area selection**: working one track at time; needs refinements

### Not Yet Implemented Features :x:
- Audio clip editing view
- Audio clips warping
- Plugins grouping
- Tracks grouping
- Parameters automations
- Project saving, loading and export
---

<h2 align="center">
 Gallery :camera:
</h2>

<p align="center">
 <b>Arrangement</b>
</p>

![Lumix arrangement](https://github.com/user-attachments/assets/e37157f1-aaa0-45de-b877-165215a48598)

<p align="center">
 <b>Piano Roll</b>
</p>

![Lumix piano roll](https://github.com/user-attachments/assets/5ba67ccd-325a-409a-999e-87ce9fa64f19)

<p align="center">
 <b>VST Plugins</b>
</p>

![Lumix plugins](https://github.com/user-attachments/assets/70aa6132-4ce6-459b-9679-7ae36ae93044)

### Special Thanks To
- [dear-imgui](https://github.com/ocornut/imgui) and [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
- [NAudio](https://github.com/naudio/NAudio)
- [DryWetMidi](https://github.com/melanchall/drywetmidi)
- [VST.NET](https://github.com/obiwanjacobi/vst.net)
