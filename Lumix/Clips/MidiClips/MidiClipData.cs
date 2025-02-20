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
        // new NoteOnEvent(new SevenBitNumber(68), new SevenBitNumber(100)
        // new NoteOffEvent(new SevenBitNumber(68), new SevenBitNumber(0)
        var trackChunk = new TrackChunk();
        trackChunk.Events.Add(new SequenceTrackNameEvent() { DeltaTime = 0 });
        trackChunk.Events.Add(new SequenceTrackNameEvent()
        {
            DeltaTime = TimeConverter.ConvertFrom(new MetricTimeSpan(0, 0, 4), TempoMap.Default)
        });
        MidiFile = new MidiFile(trackChunk);
        Notes = MidiFile.GetNotes();
        TempoMap = MidiFile.GetTempoMap();
    }

    private MidiFile CreateNewMidiFile2(ICollection<Note> notes)
    {
        // Define the time division (e.g., 480 ticks per quarter note)
        var timeDivision = new TicksPerQuarterNoteTimeDivision(96); // Adjust if needed

        // Create a track chunk for the MIDI file
        var trackChunk = new TrackChunk();

        // Add a track name event (optional)
        trackChunk.Events.Add(new SequenceTrackNameEvent("Custom Track"));

        // Add a time signature event (default to 4/4)
        trackChunk.Events.Add(new TimeSignatureEvent(4, 4, 36, 8)); // 4/4 time signature
        trackChunk.Events.Add(new TimeSignatureEvent(4, 4, 36, 8));
        // Add a tempo event (default to 120 BPM)
        //trackChunk.Events.Add(new SetTempoEvent(500_000)); // 120 BPM in microseconds per quarter note

        // Initialize the previous tick position
        long previousTick = 0;

        foreach (var note in notes.OrderBy(n => n.Time))
        {
            // Start tick and duration in ticks
            long startTick = note.Time;
            long durationTicks = note.Length;

            // DeltaTime is the difference from the previous tick position
            long deltaTime = startTick - previousTick;

            // Add NoteOn event
            trackChunk.Events.Add(new NoteOnEvent(note.NoteNumber, note.Velocity)
            {
                DeltaTime = (int)deltaTime
            });

            // Update the previous tick position
            previousTick = startTick;

            // Add NoteOff event (with duration as delta time)
            trackChunk.Events.Add(new NoteOffEvent(note.NoteNumber, SevenBitNumber.MinValue)
            {
                DeltaTime = (int)durationTicks
            });

            // Update the previous tick position to include the note's duration
            //previousTick += durationTicks;
        }

        // Add an EndOfTrack event
        //trackChunk.Events.Add(new EndOfTrackEvent());

        // Create the MIDI file with the defined time division and track
        var midiFile = new MidiFile(trackChunk)
        {
            TimeDivision = timeDivision
        };

        return midiFile;
    }



    private MidiFile CreateNewMidiFile(ICollection<Note> notes)
    {
        // Create a track chunk for the MIDI file
        var trackChunk = new TrackChunk();

        // Keep track of the previous time in microseconds for delta time calculations
        long previousTime = 0;

        foreach (var note in notes)
        {
            // Calculate the note's start and duration in microseconds
            var startTime = note.Time; //.TimeAs<MetricTimeSpan>(TempoMap.Default).TotalMicroseconds;
            var duration = note.Length; //.LengthAs<MetricTimeSpan>(TempoMap.Default).TotalMicroseconds;

            trackChunk.Events.Add(new NoteOnEvent(note.NoteNumber, note.Velocity)
            {
                DeltaTime = (int)(startTime - previousTime)
            });

            // Update previous time to the start time of the note
            previousTime = startTime;

            // Calculate delta time for NoteOffEvent (note duration)
            trackChunk.Events.Add(new NoteOffEvent(note.NoteNumber, SevenBitNumber.MinValue)
            {
                DeltaTime = (int)duration
            });

            // Update previous time to the end time of the note
            //previousTime += duration;
        }

        // Create the MIDI file with the single track
        var midiFile = new MidiFile(trackChunk);

        return midiFile;
    }

    /*
    private MidiFile CreateNewMidiFile(ICollection<Note> notes)
    {
        var trackChunk = new TrackChunk();
        foreach (var note in notes)
        {
            trackChunk.Events.Add(new NoteOnEvent(note.NoteNumber, note.Velocity) { DeltaTime = 0 });
            trackChunk.Events.Add(new NoteOffEvent(note.NoteNumber, SevenBitNumber.MinValue) { DeltaTime = note.LengthAs<MetricTimeSpan>(TempoMap.Default).TotalMicroseconds });
        }

        var midiFile = new MidiFile(trackChunk);
        return midiFile;
    }
    */
}
