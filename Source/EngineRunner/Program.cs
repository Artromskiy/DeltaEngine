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
    eng.Draw();
    sw.Stop();
    ms += sw.ElapsedTicks;
    c++;
    if (c == 100)
    {
        ms /= 100;
        Console.WriteLine((int)(10000000f / ms)); // FPS of main thread
        ms = 0;
        c = 0;
    }
}