namespace DeltaEditor.Inspector;

internal static class Helpers
{
    public static int SmoothInt(int value1, int value2, int smoothing)
    {
        return ((value1 * smoothing) + value2) / (smoothing + 1);
    }

}
