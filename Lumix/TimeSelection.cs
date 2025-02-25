using Lumix.Views.Arrangement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        Start = TimeLineV2.TicksToMusicalTime(TimeLineV2.GetLastTickStart(), true);
        End = TimeLineV2.TicksToMusicalTime(TimeLineV2.GetLastTickStart(), true);
    }
}
