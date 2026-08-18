using Delta.Engine.Assets.Defaults;
using Delta.Engine.Assets;
using Delta.Engine.ECS;
using Delta.Engine.ECS.Components;
using Delta.Engine.Runtime;
using Delta.Engine.EditorLib.Loader;
using Delta.Maths;
using System.Diagnostics;

try
{
    string directoryPath = ProjectCreator.GetExecutableDirectory();
    var projectPath = new EditorPaths(directoryPath);
    ProjectCreator.CreateProject(projectPath);
    var ctx = RuntimeContextFactory.CreateWindowedContext(projectPath);
    using var eng = new Runtime(ctx);

    //VCShader.Init();
    DefaultsImporter<MeshData>.Import(Path.Combine(Directory.GetCurrentDirectory(), "Import", "Models"));
    //MaterialsImporter.Import(Path.Combine(Directory.GetCurrentDirectory(), "Import", "Shaders"));

    var camera = IRuntimeContext.Current.SceneManager.CurrentScene.AddEntity();
    camera.Entity.Add<Transform>();
    camera.Entity.Add<Camera>();
    camera.Entity.Get<Transform>() = new Transform()
    {
        rotation = quaternion.identity,
        scale = new float3(1),
        position = new float3(0, 0, -5),
    };
    var cam = camera.Entity.Get<Camera>();
    camera.Entity.Get<Camera>() = new Camera();
    cam = camera.Entity.Get<Camera>();

    var render = IRuntimeContext.Current.SceneManager.CurrentScene.AddEntity();
    render.Entity.Add<Transform>();
    render.Entity.Add<Render>();

    render.Entity.Get<Transform>() = new Transform()
    {
        rotation = quaternion.identity,
        scale = new float3(1),
        position = float3.zero
    };

    render.Entity.Get<Render>() = new Render()
    {
        material = IRuntimeContext.Current.AssetImporter.GetAllAssets<MaterialData>()[0],
        mesh = IRuntimeContext.Current.AssetImporter.GetAllAssets<MeshData>()[0],
    };


    eng.Context.Running = true;

    Stopwatch sw = new();
    TimeSpan ms = TimeSpan.Zero;
    TimeSpan timer = TimeSpan.Zero;

    while (true)
    {
        eng.Run();
        Thread.Yield();
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}
Console.ReadLine();
