namespace Lumix.Views;

public enum BottomViewWindows
{
    DevicesView,
    MidiClipView,
    AudioClipView
}

public static class BottomView
{
    public static BottomViewWindows RenderedWindow { get; set; } = BottomViewWindows.DevicesView;
}
