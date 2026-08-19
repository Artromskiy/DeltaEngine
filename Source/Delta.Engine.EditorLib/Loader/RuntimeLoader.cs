using Delta.Engine.Assets;
using Delta.Engine.Assets.Defaults;
using Delta.Engine.Runtime;
using Delta.Engine.EditorLib.Compile;
using Delta.Engine.EditorLib.Scripting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Delta.Engine.EditorLib.Loader;

public class RuntimeLoader
{
    private readonly IProjectPath _projectPath;
    private IRuntime _runtime;

    private readonly ICompilerModule _compilerModule;
    private readonly ShaderCompilerModule _shaderCompilerModule;
    private IRuntimeScheduler _executionModule;

    private readonly IThreadGetter? _threadGetter;

    public IAccessorsContainer Accessors => _compilerModule.Accessors!;
    public List<Type> Components => _compilerModule.Components;

    public event Action OnLoop
    {
        add => _executionModule.OnLoop += value;
        remove => _executionModule.OnLoop -= value;
    }


    public RuntimeLoader(IProjectPath projectPath, IThreadGetter? uiThreadGetter)
    {
        _projectPath = projectPath;
        _threadGetter = uiThreadGetter;

        _compilerModule = new CompilerModule(_projectPath);
        _shaderCompilerModule = new ShaderCompilerModule();

        _compilerModule.Recompile();

        var ctx = RuntimeContextFactory.CreateHeadlessContext(_projectPath);
        _runtime = new Delta.Engine.Runtime.Runtime(ctx);

        _executionModule = new RuntimeScheduler(_runtime, _threadGetter);
        var directory = Directory.GetCurrentDirectory();

        DefaultsImporter<MeshData>.Import(Path.Combine(directory, "Import", "Models"));
        _shaderCompilerModule.CompileAndImportShaders(Path.Combine(directory, "Import", "Shaders"));
    }

    public void ReloadRuntime()
    {
        _runtime.Dispose();
        _runtime = null!;

        _compilerModule.Recompile();

        var ctx = RuntimeContextFactory.CreateHeadlessContext(_projectPath);
        _runtime = new Delta.Engine.Runtime.Runtime(ctx);
        _executionModule = new RuntimeScheduler(_runtime, _threadGetter);
    }

    public void Init() => _executionModule.Init();
}
