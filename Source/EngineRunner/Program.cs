using DeltaEngine;
using System.Diagnostics;




//var h = System.Runtime.InteropServices.NativeLibrary.GetMainProgramHandle();


Console.WriteLine("Hello, World!");

var eng = new Engine();
eng.Run();
Stopwatch sw = new();
while (true)
{
    sw.Restart();
    Thread.Yield();
    eng.Run();
    sw.Stop();
    Console.WriteLine(sw.ElapsedTicks);
}