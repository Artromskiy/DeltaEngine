using Delta.Editor.Scripting;
using Delta.Engine.Integration;
using Delta.Engine.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Delta.Engine.EditorLib.Compile;

internal class CompileHelper(IProjectPath projectPath)
{
    private const string CsSearch = "*.cs";
    private const string Scripts = "Scripts";
    private const string Accessors = "Accessors";

    private readonly IProjectPath _projectPath = projectPath;

    private readonly IScriptCompiler _compiler = new RoslynScriptCompiler();
    private IReadOnlyList<IScriptReference>? _references;

    public ScriptCompilationResult CompileScripts()
    {
        var sources = Directory.EnumerateFiles(_projectPath.ScriptsDirectory, CsSearch, SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => new ScriptSource(path, File.ReadAllText(path)))
            .ToArray();
        return _compiler.Compile(new ScriptCompilationRequest(sources, GetReferences(), Scripts));
    }

    public ScriptCompilationResult CompileAccessors(HashSet<Type> components)
    {
        var code = AccessorGenerator.GenerateAccessors(components);
        return _compiler.Compile(new ScriptCompilationRequest(
            [new ScriptSource("Accessors.g.cs", code)],
            GetReferences(),
            Accessors));
    }

    private IReadOnlyList<IScriptReference> GetReferences()
    {
        if (_references != null)
            return _references;

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (trustedPlatformAssemblies != null)
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
                paths.Add(path);

        paths.Add(typeof(IRuntime).Assembly.Location);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (!string.IsNullOrEmpty(assembly.Location))
                paths.Add(assembly.Location);

        _references = paths
            .Where(File.Exists)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => (IScriptReference)new FileScriptReference(path))
            .ToArray();
        return _references;
    }
}
