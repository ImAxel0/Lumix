namespace Lumix;

public struct MusicalTime
{
    public MusicalTime(int bars, int beats, int ticks)
    {
        this.Bars = bars;
        this.Beats = beats;
        this.Ticks = ticks;
    }

    public int Bars;
    public int Beats;
    public int Ticks;
}
