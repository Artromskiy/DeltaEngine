using Delta.Assets.Defaults;
using Delta.Assets;
using Delta.ECS;
using Delta.ECS.Components;
using Delta.Runtime;
using DeltaEditorLib.Loader;
using System.Diagnostics;
using System.Numerics;

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
        rotation = Quaternion.Identity,
        scale = Vector3.One,
        position = new Vector3(0,0,-5),
    };
    var cam = camera.Entity.Get<Camera>();
    camera.Entity.Get<Camera>() = new Camera();
    cam = camera.Entity.Get<Camera>();

    var render = IRuntimeContext.Current.SceneManager.CurrentScene.AddEntity();
    render.Entity.Add<Transform>();
    render.Entity.Add<Render>();

    render.Entity.Get<Transform>() = new Transform()
    {
        rotation = Quaternion.Identity,
        scale = Vector3.One,
        position = Vector3.Zero
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


