using Delta.Runtime;
using DeltaEditorLib.Compile;
using DeltaEditorLib.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DeltaEditorLib.Loader;

public class RuntimeLoader
{
    private readonly IProjectPath _projectPath;
    private IRuntime _runtime;

    private readonly ICompilerModule _compilerModule;
    private IRuntimeScheduler _executionModule;

    private readonly IUIThreadGetter? _uiThreadGetter;

    public IAccessorsContainer Accessors => _compilerModule.Accessors!;
    public List<Type> Components => _compilerModule.Components;

    public event Action<IRuntime> OnUIThreadLoop
    {
        add => _executionModule.OnUIThreadLoop += value;
        remove => _executionModule.OnUIThreadLoop -= value;
    }

    public event Action<IRuntime> OnUIThread
    {
        add => _executionModule.OnUIThread += value;
        remove => _executionModule.OnUIThread -= value;
    }

    public event Action<IRuntime> OnRuntimeThread
    {
        add => _executionModule.OnRuntimeThread += value;
        remove => _executionModule.OnRuntimeThread -= value;
    }

    public RuntimeLoader(IProjectPath projectPath, IUIThreadGetter? uiThreadGetter)
    {
        _projectPath = projectPath;
        _uiThreadGetter = uiThreadGetter;

        _compilerModule = new CompilerModule(_projectPath);

        _compilerModule.Recompile();
        _runtime = new Runtime(_projectPath);
        _executionModule = new RuntimeScheduler(_runtime, _uiThreadGetter);
    }

    public void ReloadRuntime()
    {
        _runtime.Dispose();
        _runtime = null!;

        _compilerModule.Recompile();

        _runtime = new Runtime(_projectPath);
        _executionModule = new RuntimeScheduler(_runtime, _uiThreadGetter);
    }

    public void OpenProjectFolder()
    {
        try
        {
            if (Directory.Exists(_projectPath.RootDirectory))
                Process.Start("explorer.exe", _projectPath.RootDirectory);
        }
        catch { }
    }
}