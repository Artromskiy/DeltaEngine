using Arch.Core;
using Arch.Core.Extensions;
using Delta.Runtime;
using Delta.Scripting;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace DeltaEditorLib.Scripting
{
    public class RuntimeLoader
    {
        private readonly IProjectPath _projectPath;

        private readonly List<object> _instantiatedObjects = [];

        private AssemblyLoadContext _alc;
        private AssemblyLoadContext.ContextualReflectionScope _scope;
        private readonly CompileHelper _compileHelper;

        private readonly HashSet<WeakReference<AssemblyLoadContext>> _oldAlcs = [];

        public IRuntime Runtime { get; private set; }

        public RuntimeLoader(IProjectPath projectPath)
        {
            _projectPath = projectPath;
            _compileHelper = new(_projectPath);

            _alc = Recompile();
            Runtime = new Runtime(_projectPath);
        }

        private AssemblyLoadContext Recompile()
        {
            var dllPath = _compileHelper.TryCompile();

            _scope.Dispose();
            _alc?.Unload();

            _oldAlcs.RemoveWhere(r => !r.TryGetTarget(out _));

            if (_alc != null)
                _oldAlcs.Add(new WeakReference<AssemblyLoadContext>(_alc, false));

            var alc = new AssemblyLoadContext("Scripting", true);
            _scope = alc.EnterContextualReflection();
            alc.LoadFromAssemblyPath(dllPath);
            return alc;
        }

        public void ReloadRuntime()
        {
            Runtime.Running = false;

            _alc = Recompile();

            Runtime.Dispose();
            Runtime = new Runtime(_projectPath);
            return;
        }

        public List<string> GetComponentsNames()
        {
            List<string> components = [];
            foreach (var assembly in AssemblyLoadContext.CurrentContextualReflectionContext!.Assemblies)
                components.AddRange(GetComponentsNames(assembly));
            foreach (var assembly in AssemblyLoadContext.Default.Assemblies)
                components.AddRange(GetComponentsNames(assembly));

            return components;
        }

        private static IEnumerable<string> GetComponentsNames(Assembly assembly)
        {
            return assembly.GetTypes().
                Where(type => type.GetCustomAttribute<ComponentAttribute>() != null).
                Select(x => x.Name);
        }

        private void ConvertEntityToJson()
        {
            Entity entity = Entity.Null;
            var components = entity.GetComponentTypes();
            //jsonchema
            //components[0].;
        }

        private static List<Type> GetComponentTypes(Assembly assembly) => assembly.
            GetTypes().
            Where(type => type.GetCustomAttribute<ComponentAttribute>() != null).
            ToList();
    }
}