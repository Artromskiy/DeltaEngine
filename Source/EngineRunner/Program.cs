using DeltaEngine;
using DeltaEngine.Files;
using DeltaEngine.Rendering;
using System.Diagnostics;


using var eng = new Engine();
//eng.Run();
Stopwatch sw = new();


//int entitties = 500;
//
//int shaders = 5;
//int materials = 15;
//int meshes = 25;

int entitties = 1000000;

int shaders = 300;
int materials = 1000;
int meshes = 1000;

RenderData[] datas = new RenderData[entitties];

GuidAsset<MeshData>[] meshDatas = new GuidAsset<MeshData>[meshes];
GuidAsset<ShaderData>[] shaderDatas = new GuidAsset<ShaderData>[shaders];
GuidAsset<MaterialData>[] materialDatas = new GuidAsset<MaterialData>[materials];

for (int i = 0; i < meshes; i++)
    meshDatas[i] = AssetImporter.Instance.CreateRuntimeAsset(new MeshData(Array.Empty<uint>(), Array.Empty<byte[]>(), 0));
for (int i = 0; i < shaders; i++)
    shaderDatas[i] = AssetImporter.Instance.CreateRuntimeAsset(ShaderData.DummyShaderData());
for (int i = 0; i < materials; i++)
    materialDatas[i] = AssetImporter.Instance.CreateRuntimeAsset(new MaterialData(i < shaders ? shaderDatas[i] : shaderDatas[Random.Shared.Next(0, shaders)]));
for (int i = 0; i < entitties; i++)
    datas[i] = new RenderData()
    {
        isStatic = i % 2 == 0,
        material = i < materials ? materialDatas[i] : materialDatas[Random.Shared.Next(0, materials)],
        mesh = i < meshes ? meshDatas[i] : meshDatas[Random.Shared.Next(0, meshes)]
    };

sw.Restart();
Batcher.IterateWithCopy(datas);
sw.Stop();
Console.WriteLine(sw.ElapsedTicks);
//Console.WriteLine((int)(10000000f / sw.ElapsedTicks)); // FPS of main thread
Console.ReadLine();

while (true)
{
    Thread.Yield();
    sw.Restart();
    Batcher.IterateWithCopy(datas);
    sw.Stop();
    Console.WriteLine(sw.ElapsedTicks);
    Console.WriteLine($"fps: {(int)(10000000f / sw.ElapsedTicks)}"); // FPS of main thread
}

while (true)
{
    Thread.Yield();
    sw.Restart();
    eng.Run();
    eng.Draw();
    sw.Stop();
    //Console.WriteLine((int)(10000000f / sw.ElapsedTicks)); // FPS of main thread
}