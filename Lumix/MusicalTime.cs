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

    public int Bars;
    public int Beats;
    public int Ticks;
}
