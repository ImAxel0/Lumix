namespace Lumix.Views.Arrangement;

[Obsolete]
public enum AdaptiveGridOptions
{
    Widest,
    Wide,
    Medium,
    Narrow,
    Narrowest
}

[Obsolete]
public static class AdaptiveGrid
{
    public static AdaptiveGridOptions GridOption { get; private set; } = AdaptiveGridOptions.Narrow;
    public const float BaseSpacing = 120f; // Base spacing in pixels for beats at neutral zoom
    public static float MinSpacing { get; private set; } = 15f; // Minimum grid spacing in pixels
    public static float MaxSpacing { get; private set; } = 40f; // Maximum grid spacing in pixels

    public static void SetGridOption(AdaptiveGridOptions adaptiveGridOption)
    {
        switch (adaptiveGridOption)
        {
            case AdaptiveGridOptions.Widest:
                MinSpacing = 240f;
                MaxSpacing = 640f;
                break;
            case AdaptiveGridOptions.Wide:
                MinSpacing = 120f;
                MaxSpacing = 320f;
                break;
            case AdaptiveGridOptions.Medium:
                MinSpacing = 60f;
                MaxSpacing = 160f;
                break;
            case AdaptiveGridOptions.Narrow:
                MinSpacing = 15f;
                MaxSpacing = 40f;
                break;
            case AdaptiveGridOptions.Narrowest:
                MinSpacing = 7.5f;
                MaxSpacing = 20f;
                break;
        }
        GridOption = adaptiveGridOption;
    }

    internal static float CalculateGridSpacing(float zoom, float minSpacing, float maxSpacing)
    {
        float spacing = BaseSpacing * zoom;

        // Ensure spacing stays within the desired range
        while (spacing < minSpacing) spacing *= 2; // Increase granularity (smaller intervals)
        while (spacing > maxSpacing) spacing /= 2; // Reduce granularity (larger intervals)

        return spacing;
    }

    public static float GetSnappedPosition(float targetPosition)
    {
        float gridSpacingPixels = CalculateGridSpacing(ArrangementView.Zoom, MinSpacing, MaxSpacing);

        // Convert grid spacing in pixels to time units
        float gridSpacingTime = gridSpacingPixels / ArrangementView.Zoom;

        // Snap the position to the nearest grid line
        float snappedPosition = MathF.Round(targetPosition / gridSpacingTime) * gridSpacingTime;
        return snappedPosition;
    }
}
