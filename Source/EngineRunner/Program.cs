using Delta.ECS.Components;
using Delta.Runtime;
using System.Diagnostics;
using System.Numerics;

//try
{
    using var eng = new Runtime(new EditorPaths(Directory.GetCurrentDirectory()));
    eng.CreateTestScene();
    eng.RunOnce();
    Stopwatch sw = new();

    int c = 0;
    TimeSpan ms = TimeSpan.Zero;
    TimeSpan timer = TimeSpan.Zero;

    while (true)
    {
        Thread.Yield();
        sw.Restart();
        eng.RunOnce();
        sw.Stop();
        ms += sw.Elapsed;
        timer += sw.Elapsed;
        c++;
        if (c == 100)
        {
            var el = ms / c;
            Console.WriteLine();
            Console.WriteLine((int)(1.0 / el.TotalSeconds)); // FPS of main thread
            Console.WriteLine();
            foreach (var item in eng.GetCurrentScene().GetMetrics())
                Console.WriteLine($"{item.Key}: {(item.Value / 100).TotalMilliseconds:0.00}ms");
            eng.GetCurrentScene().ClearMetrics();
            ms = TimeSpan.Zero;
            c = 0;
        }
        if (timer.TotalSeconds >= 20)
        {
            timer = TimeSpan.Zero;
            eng.CreateTestScene();
        }
    }
}
//catch (Exception e)
//{
//    Console.WriteLine(e);
//}
Console.ReadLine();
