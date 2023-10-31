using DeltaEngine;
using System.Diagnostics;

using var eng = new Engine();
eng.Run();
Stopwatch sw = new();

while (true)
{
    Thread.Yield();
    sw.Restart();
    eng.Run();
    eng.Draw();
    sw.Stop();
    //Console.WriteLine((int)(10000000f / sw.ElapsedTicks)); // FPS of main thread
}