using DeltaEngine;
using System.Diagnostics;




//var h = System.Runtime.InteropServices.NativeLibrary.GetMainProgramHandle();


Console.WriteLine("Hello, World!");

var eng = new Engine();
eng.Run();
Stopwatch sw = new();
while (true)
{
    Thread.Yield();
    sw.Restart();
    eng.Run();
    eng.Draw();
    sw.Stop();
    //Console.WriteLine((int)((1f/sw.ElapsedMilliseconds) * 1000));
}