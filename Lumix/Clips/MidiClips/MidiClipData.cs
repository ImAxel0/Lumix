using Lumix.Views.Arrangement;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace Lumix.Clips.MidiClips;

public class MidiClipData
{
    public MidiFile MidiFile { get; set; }
    public ICollection<Note> Notes { get; set; }
    public TempoMap TempoMap { get; set; }

    public MidiClipData(MidiFile midiFile)
    {
        MidiFile = midiFile;
        Notes = midiFile.GetNotes();
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
        Notes = MidiFile.GetNotes();
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
        Notes = MidiFile.GetNotes();
        TempoMap = MidiFile.GetTempoMap();
    }
}
