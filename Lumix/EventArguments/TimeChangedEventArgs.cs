using Lumix.Views.Arrangement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumix.EventArguments;

public sealed class TimeChangedEventArgs : EventArgs
{
    public MusicalTime OldTime { get; }
    public MusicalTime NewTime { get; }

    public TimeChangedEventArgs(MusicalTime oldTime, MusicalTime newTime)
    {
        OldTime = oldTime;
        NewTime = newTime;
    }
}
