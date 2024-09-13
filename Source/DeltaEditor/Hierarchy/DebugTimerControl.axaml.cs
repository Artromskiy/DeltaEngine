using Avalonia;
using Avalonia.Controls;
using System.Diagnostics;

namespace DeltaEditor;

public partial class DebugTimerControl : UserControl
{
    private Stopwatch sw;
    private int prevTime = 0;
    public DebugTimerControl()=> InitializeComponent();

    public void StartDebug()
    {
        (sw ??= new()).Restart();
    }

    public void StopDebug()
    {
        sw.Stop();
        int us = (int)(sw?.Elapsed.TotalMicroseconds ?? 0);
        prevTime = SmoothInt(prevTime, us, 50);
        DebugTimer.Content = $"{prevTime}us";
    }

    private static int SmoothInt(int value1, int value2, int smoothing)
    {
        return (value1 * smoothing + value2) / (smoothing + 1);
    }
}