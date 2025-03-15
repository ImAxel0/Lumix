using Lumix.Views.Sidebar.Preview;
using System.Diagnostics;

namespace Lumix.Views.Arrangement;

public static class TimeLine
{
    public const int PPQ = 960;
    public static float BeatsPerBar { get; set; } = 4; // Default 4/4 time signature
    public static float PixelsPerTick => 0.05f * ArrangementView.Zoom;

    private static Stopwatch _stopwatch = new();
    private static long _currentTick;
    private static long _lastTickStart;

    public static void StartPlayback()
    {
        _stopwatch.Start();
        _lastTickStart = _currentTick;
    }

    public static void StopPlayback(bool moveToStart = false)
    {
        _stopwatch.Stop();
        _currentTick = _lastTickStart;
        if (moveToStart)
        {
            _lastTickStart = 0;
            _stopwatch.Reset();
        }

        AudioPreviewEngine.Instance.StopSound();
        MidiPreviewEngine.StopPreview();
        foreach (var track in ArrangementView.Tracks)
        {
            track.Engine.StopSounds();
            foreach (var clip in track.Clips)
            {
                clip.HasPlayed = false;
            }

            if (track.Engine.IsRecording)
            {
                track.Engine.StopRecording(track);
            }
        }
    }

    public static void StartRecording()
    {
        throw new NotImplementedException();
    }

    public static bool IsPlaying()
    {
        return _stopwatch.IsRunning;
    }

    public static void UpdatePlayback()
    {
        if (_stopwatch.IsRunning)
        {
            double elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
            long elapsedTicks = SecondsToTicks(elapsedSeconds);
            _currentTick += elapsedTicks;

            // Reset stopwatch to avoid accumulating elapsed time
            _stopwatch.Restart();
        }
    }

    public static long GetCurrentTick()
    {
        return _currentTick;
    }

    public static long GetLastTickStart()
    { 
        return _lastTickStart; 
    }

    public static void SetCurrentTick(long ticks)
    {
        _currentTick = ticks;
    }

    public static void SetLastTickSart(long ticks)
    {
        _lastTickStart = ticks;
    }

    public static double TicksToSeconds(long ticks, bool useTempo = true)
    {
        if (!useTempo)
        {
            return ticks * (60.0 / (120.0 * PPQ));
        }
        return ticks * (60.0 / (TopBarControls.Bpm * PPQ));
    }

    public static long SecondsToTicks(double seconds, bool useTempo = true)
    {
        if (!useTempo)
        {
            return (long)Math.Round(seconds * (120.0 * PPQ) / 60.0);
        }
        return (long)Math.Round(seconds * (TopBarControls.Bpm * PPQ) / 60.0);
    }

    public static MusicalTime TicksToMusicalTime(long ticks, bool applyOffset = false)
    {
        // Ticks per bar and beat
        int ticksPerBar = 4 * PPQ; // not sure 4 is right
        int ticksPerBeat = PPQ;

        // Calculate bars
        int bars = (int)(ticks / ticksPerBar);
        long remainingTicksAfterBars = ticks % ticksPerBar;

        // Calculate beats
        int beats = (int)(remainingTicksAfterBars / ticksPerBeat);
        long remainingTicksAfterBeats = remainingTicksAfterBars % ticksPerBeat;

        // Remaining ticks
        int ticksRemainder = (int)remainingTicksAfterBeats;

        if (applyOffset)
        {
            // Offset bars, beats, and ticks to start at 1:1:1
            return new MusicalTime(bars + 1, beats + 1, ticksRemainder + 1);
        }
        return new MusicalTime(bars, beats, ticksRemainder);
    }

    public static long MusicalTimeToTicks(MusicalTime musicalTime, bool applyOffset = false)
    {
        int ticksPerBar = 4 * PPQ; // not sure 4 is right
        int ticksPerBeat = PPQ;

        if (applyOffset)
        {
            // Subtract 1 to match the offset logic
            return ((musicalTime.Bars - 1) * ticksPerBar) + ((musicalTime.Beats - 1) * ticksPerBeat) + (musicalTime.Ticks - 1);
        }
        return ((musicalTime.Bars) * ticksPerBar) + ((musicalTime.Beats) * ticksPerBeat) + (musicalTime.Ticks);
    }

    public static float TimeToPosition(long ticks)
    {
        return ticks * PixelsPerTick;
    }

    public static long PositionToTime(float x)
    {
        return (long)(x / PixelsPerTick);
    }

    public static float TicksToPixels(long ticks)
    {
        return ticks * PixelsPerTick;
    }

    public static float MusicalTimeToPixels(MusicalTime musicalTime)
    {
        return MusicalTimeToTicks(musicalTime) * PixelsPerTick;
    }

    public static long SnapToGrid(long tick)
    {
        long gridSpacing = (long)(TimeLine.PPQ * TimeLine.BeatsPerBar);
        return (long)Math.Round((double)tick / gridSpacing) * gridSpacing;
    }
}
