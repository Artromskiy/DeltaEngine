using Delta.Assets;
using Delta.Assets.Defaults;
using Delta.Runtime;
using DeltaEditorLib.Compile;
using DeltaEditorLib.Scripting;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeltaEditorLib.Loader;

public class RuntimeLoader
{
    private readonly IProjectPath _projectPath;
    private IRuntime _runtime;

    private readonly ICompilerModule _compilerModule;
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

        _compilerModule.Recompile();
        _runtime = new Runtime(_projectPath);
        _executionModule = new RuntimeScheduler(_runtime, _threadGetter);
        VCShader.Init();
        DefaultsImporter<MeshData>.Import(_runtime.Context, Path.Combine(Directory.GetCurrentDirectory(), "Import", "Models"));
    }

    public void ReloadRuntime()
    {
        _runtime.Dispose();
        _runtime = null!;

        _compilerModule.Recompile();

        _runtime = new Runtime(_projectPath);
        _executionModule = new RuntimeScheduler(_runtime, _threadGetter);
    }

    public void Init() => _executionModule.Init();
}