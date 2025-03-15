using Lumix.Views.Midi;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MidiFile = Melanchall.DryWetMidi.Core.MidiFile;

namespace Lumix.Clips.MidiClips;

public class MidiClipData
{
    public MidiFile MidiFile { get; set; }
    public List<PNote> Notes { get; set; } = new();
    public TempoMap TempoMap { get; set; }

    public MidiClipData(MidiFile midiFile)
    {
        MidiFile = midiFile;
        foreach (var note in midiFile.GetNotes())
        {
            Notes.Add(new PNote(note));
        }
        TempoMap = midiFile.GetTempoMap();
    }

    public MidiClipData()
    {
        var trackChunk = new TrackChunk();
        trackChunk.Events.Add(new SequenceTrackNameEvent() { DeltaTime = 0 });
        trackChunk.Events.Add(new SequenceTrackNameEvent()
        {
            DeltaTime = TimeConverter.ConvertFrom(new BarBeatTicksTimeSpan(2, 0, 0), TempoMap.Default)
        });
        MidiFile = new MidiFile(trackChunk);
        foreach (var note in MidiFile.GetNotes())
        {
            Notes.Add(new PNote(note));
        }
        TempoMap = MidiFile.GetTempoMap();
    }

    public MidiClipData(TimeSelection time)
    {
        var trackChunk = new TrackChunk();
        var length = time.Length;
        trackChunk.Events.Add(new SequenceTrackNameEvent() { DeltaTime = 0 });
        trackChunk.Events.Add(new SequenceTrackNameEvent()
        {
            DeltaTime = TimeConverter.ConvertFrom(new BarBeatTicksTimeSpan(length.Bars, length.Beats, length.Ticks), TempoMap.Default)
        });
        MidiFile = new MidiFile(trackChunk);
        foreach (var note in MidiFile.GetNotes())
        {
            Notes.Add(new PNote(note));
        }
        TempoMap = MidiFile.GetTempoMap();
    }
}
