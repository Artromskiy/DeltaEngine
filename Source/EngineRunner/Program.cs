using DeltaEngine;
using System.Diagnostics;

using var eng = new Engine();
eng.Run();
Stopwatch sw = new();

int c = 0;
long ms = 0;

while (true)
{
    Thread.Yield();
    sw.Restart();
    eng.Run();
    sw.Stop();
    ms += sw.ElapsedTicks;
    c++;
    if (c == 10)
    {
        ms /= c;
        var sy = eng.GetSyncRendererMetric / c;
        var up = eng.GetUpdateRendererMetric / c;
        var cp = eng.GetCopyRendererMetric / c;
        var cs = eng.GetCopySetupRendererMetric / c;
        var csn = eng.GetSceneMetric / c;
        Console.WriteLine($"sync: {sy.TotalMilliseconds}"); // FPS of main thread
        Console.WriteLine($"updt: {up.TotalMilliseconds}"); // FPS of main thread
        Console.WriteLine($"copy: {cp.TotalMilliseconds}"); // FPS of main thread
        Console.WriteLine($"cpys: {cs.TotalMilliseconds}"); // FPS of main thread
        Console.WriteLine($"scen: {csn.TotalMilliseconds}"); // FPS of main thread
        Console.WriteLine((int)(10000000f / ms)); // FPS of main thread
        eng.ClearRendererMetrics();
        ms = 0;
        c = 0;
    }
}