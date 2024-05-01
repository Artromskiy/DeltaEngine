using Delta.Runtime;
using Delta.Scripting;
using DeltaEditorLib.Compile;
using System.Reflection;
using System.Runtime.Loader;

namespace DeltaEditorLib.Scripting;

internal class CompilerModule : ICompilerModule
{
    private readonly CompileHelper _compileHelper;

    private AssemblyLoadContext? _context;
    private AssemblyLoadContext.ContextualReflectionScope _scope;

    private readonly HashSet<WeakReference<AssemblyLoadContext>> _oldAlcs = [];
    private readonly HashSet<Type> _components = [];

    public IAccessorsContainer? Accessors { get; private set; }
    public List<Type> Components => new(_components);

    public CompilerModule(IProjectPath projectPath)
    {
        _compileHelper = new CompileHelper(projectPath);
    }

    public void Recompile()
    {
        _context = NewLoadContext();
        Compile();

        _components.UnionWith(GetComponents());
        Accessors = (Activator.CreateInstance(AccessorsContainerType()) as IAccessorsContainer)!;
    }

    private void Compile()
    {
        var scriptsPath = _compileHelper.CompileScripts();
        _context!.LoadFromAssemblyPath(scriptsPath);
        HashSet<Type> components = new(GetComponents());
        var accessorsPath = _compileHelper.CompileAccessors(components);
        _context.LoadFromAssemblyPath(accessorsPath);
    }

    private AssemblyLoadContext NewLoadContext()
    {
        UnloadContext();

        var loadContext = new AssemblyLoadContext("Scripting", true);
        _scope = loadContext.EnterContextualReflection();
        return loadContext;
    }

    private void UnloadContext()
    {
        _components.Clear();
        Accessors = null;
        _scope.Dispose();
        _scope = default;
        _context?.Unload();
        if (_context != null)
            _oldAlcs.Add(new WeakReference<AssemblyLoadContext>(_context, false));
        _context = null;
        _oldAlcs.RemoveWhere(r => !r.TryGetTarget(out _));
    }


    private static Type AccessorsContainerType()
    {
        var contextAssemblies = AssemblyLoadContext.CurrentContextualReflectionContext!.Assemblies;
        var contextTypes = contextAssemblies.SelectMany(x => x.GetTypes());
        return contextTypes.Where(t => typeof(IAccessorsContainer).IsAssignableFrom(t)).FirstOrDefault();
    }

    private static IEnumerable<Type> GetComponents()
    {
        var contextAssemblies = AssemblyLoadContext.CurrentContextualReflectionContext!.Assemblies;
        var mainAssemblies = AssemblyLoadContext.Default.Assemblies;
        return contextAssemblies.Select(GetComponents).
            Concat(AssemblyLoadContext.Default.Assemblies.Select(GetComponents)).
            SelectMany(type => type);
    }

    private static IEnumerable<Type> GetComponents(Assembly assembly)
    {
        return assembly.GetTypes().
            Where(type => type.HasAttribute<ComponentAttribute>());
    }
}
