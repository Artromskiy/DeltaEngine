using Delta;
using System.Diagnostics;

//try
{
    using var eng = new Engine(Directory.GetCurrentDirectory());
    eng.CreateTestScene();
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
    }
}
//catch (Exception e)
//{
//    Console.WriteLine(e);
//}
Console.ReadLine();
