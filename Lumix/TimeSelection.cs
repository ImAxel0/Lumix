using Lumix.Views.Arrangement;

namespace Lumix;

public class TimeSelection
{
    public MusicalTime Start { get; private set; }
    public MusicalTime End { get; private set; }
    public MusicalTime Length => End - Start;

    public void SetStart(MusicalTime start) => Start = start;
    public void SetEnd(MusicalTime end) => End = end;
    public void AddToStart(MusicalTime time) => Start += time;
    public void AddToEnd(MusicalTime time) => End += time;
    public void SubFromStart(MusicalTime time) => Start -= time;
    public void SubFromEnd(MusicalTime time) => End -= time;
    public bool HasArea() => Start != End;
    public void Reset()
    {
        Start = TimeLine.TicksToMusicalTime(TimeLine.GetLastTickStart(), true);
        End = TimeLine.TicksToMusicalTime(TimeLine.GetLastTickStart(), true);
    }
}
