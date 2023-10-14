using DeltaEngine;





Console.WriteLine("Hello, World!");

var eng = new Engine();
eng.Run();

while(true)
{
    await Task.Yield();
}