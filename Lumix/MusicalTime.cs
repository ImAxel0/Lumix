namespace Lumix;

public struct MusicalTime
{
    public MusicalTime(int bars, int beats, int ticks)
    {
        this.Bars = bars;
        this.Beats = beats;
        this.Ticks = ticks;
    }

    public static MusicalTime operator +(MusicalTime a, MusicalTime b)
    {
        return new MusicalTime(a.Bars + b.Bars, a.Beats + b.Beats, a.Ticks + b.Ticks);
    }

    public static MusicalTime operator -(MusicalTime a, MusicalTime b)
    {
        return new MusicalTime(a.Bars - b.Bars, a.Beats - b.Beats, a.Ticks - b.Ticks);
    }

    public static MusicalTime operator *(MusicalTime a, MusicalTime b)
    {
        return new MusicalTime(a.Bars * b.Bars, a.Beats * b.Beats, a.Ticks * b.Ticks);
    }

    public static MusicalTime operator /(MusicalTime a, MusicalTime b)
    {
        return new MusicalTime(a.Bars / b.Bars, a.Beats / b.Beats, a.Ticks / b.Ticks);
    }

    public static bool operator >(MusicalTime a, MusicalTime b)
    {
        if (a.Bars > b.Bars) return true;
        if (a.Bars == b.Bars && a.Beats > b.Beats) return true;
        if (a.Bars == b.Bars && a.Beats == b.Beats && a.Ticks > b.Ticks) return true;
        return false;
    }

    public static bool operator <(MusicalTime a, MusicalTime b)
    {
        if (a.Bars < b.Bars) return true;
        if (a.Bars == b.Bars && a.Beats < b.Beats) return true;
        if (a.Bars == b.Bars && a.Beats == b.Beats && a.Ticks < b.Ticks) return true;
        return false;
    }

    public static bool operator >=(MusicalTime a, MusicalTime b)
    {
        return a > b || a == b;
    }

    public static bool operator <=(MusicalTime a, MusicalTime b)
    {
        return a < b || a == b;
    }

    public static bool operator ==(MusicalTime a, MusicalTime b)
    {
        return a.Bars == b.Bars && a.Beats == b.Beats && a.Ticks == b.Ticks;
    }

    public static bool operator !=(MusicalTime a, MusicalTime b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj is MusicalTime)
        {
            return this == (MusicalTime)obj;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Bars.GetHashCode() ^ Beats.GetHashCode() ^ Ticks.GetHashCode();
    }

    public int Bars;
    public int Beats;
    public int Ticks;
}
