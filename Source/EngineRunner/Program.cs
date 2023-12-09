using Arch.Core;
using DeltaEngine;
using DeltaEngine.ECS;
using DeltaEngine.Files;
using DeltaEngine.Rendering;
using System.Diagnostics;

using var eng = new Engine();
//eng.Run();
Stopwatch sw = new();


int entitties = 1_000_000;

int shaders = 300;
int materials = 1000;
int meshes = 1000;


RenderData[] datas = new RenderData[entitties];

GuidAsset<MeshData>[] meshDatas = new GuidAsset<MeshData>[meshes];
GuidAsset<ShaderData>[] shaderDatas = new GuidAsset<ShaderData>[shaders];
GuidAsset<MaterialData>[] materialDatas = new GuidAsset<MaterialData>[materials];

for (int i = 0; i < meshes; i++)
    meshDatas[i] = AssetImporter.Instance.CreateRuntimeAsset(new MeshData([], [], 0));
for (int i = 0; i < shaders; i++)
    shaderDatas[i] = AssetImporter.Instance.CreateRuntimeAsset(ShaderData.DummyShaderData());
for (int i = 0; i < materials; i++)
    materialDatas[i] = AssetImporter.Instance.CreateRuntimeAsset(new MaterialData(i < shaders ? shaderDatas[i] : shaderDatas[Random.Shared.Next(0, shaders)]));


var world = World.Create();
for (int i = 0; i < entitties; i++)
{
    var entity = world.Create
        (
            new Transform() { isStatic = i % 2 == 0 },
            i < materials ? materialDatas[i] : materialDatas[Random.Shared.Next(0, materials)],
            i < meshes ? meshDatas[i] : meshDatas[Random.Shared.Next(0, meshes)]
        );
}

Console.ReadLine();

while (true)
{
    Thread.Yield();

}


while (true)
{
    Thread.Yield();
    sw.Restart();
    //  eng.Run();
    //  eng.Draw();
    sw.Stop();
    //Console.WriteLine((int)(10000000f / sw.ElapsedTicks)); // FPS of main thread
}

static void Shuffle<T>(T[] array)
{
    int n = array.Length;
    while (n > 1)
    {
        int k = Random.Shared.Next(n--);
        (array[k], array[n]) = (array[n], array[k]);
    }
}