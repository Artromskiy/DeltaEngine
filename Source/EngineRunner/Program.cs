using Delta;
using System.Diagnostics;

//try
{
    using var eng = new Engine(Directory.GetCurrentDirectory());
    eng.Run();
    Stopwatch sw = new();

    int c = 0;
    TimeSpan ms = TimeSpan.Zero;

    while (true)
    {
        Thread.Yield();
        sw.Restart();
        eng.Run();
        sw.Stop();
        ms += sw.Elapsed;
        c++;
        if (c == 100)
        {
            var el =  ms / c;
            //var sy = eng.GetSyncRendererMetric / c;
            //var up = eng.GetUpdateRendererMetric / c;
            //var cp = eng.GetCopyRendererMetric / c;
            //var cs = eng.GetCopySetupRendererMetric / c;
            //var acq = eng.GetAcquireFrameRendererMetric / c;
            //var rec = eng.GetRecordDrawRenderMetric / c;
            //var sud = eng.GetSubmitDrawRenderMetric / c;
            //var sup = eng.GetSubmitPresentRenderMetric / c;
            //var csn = eng.GetSceneMetric / c;
            //var skp = eng.GetRenderSkipPercent;
            Console.WriteLine();
            //Console.WriteLine($"updt: {up.TotalMilliseconds}"); // FPS of main thread
            //Console.WriteLine($"sync: {sy.TotalMilliseconds}"); // FPS of main thread
            //Console.WriteLine($"cpys: {cs.TotalMilliseconds}"); // FPS of main thread
            //Console.WriteLine();
            //Console.WriteLine($"copy: {cp.TotalMilliseconds}"); // FPS of main thread
            //Console.WriteLine($"acqu: {acq.TotalMilliseconds}"); // FPS of main thread
            //
            //Console.WriteLine($"recd: {rec.TotalMilliseconds}"); // FPS of main thread
            //Console.WriteLine($"subd: {sud.TotalMilliseconds}"); // FPS of main thread
            //Console.WriteLine($"subp: {sup.TotalMilliseconds}"); // FPS of main thread
            //
            //Console.WriteLine();
            //Console.WriteLine($"scen: {csn.TotalMilliseconds}"); // FPS of main thread
            //Console.WriteLine($"skip: {(int)(skp * 100)}%"); // FPS of main thread

            Console.WriteLine((int)(1.0 / el.TotalSeconds)); // FPS of main thread
            eng.ClearRendererMetrics();
            ms = TimeSpan.Zero;
            c = 0;
        }
    }
}
//catch (Exception e)
//{
//    Console.WriteLine(e);
//}
Console.ReadLine();
