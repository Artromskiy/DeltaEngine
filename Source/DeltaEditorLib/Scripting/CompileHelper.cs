using Delta.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Reflection;

namespace DeltaEditorLib.Scripting
{
    internal class CompileHelper(IProjectPath projectPath)
    {
        private const string CsSearch = "*.cs";
        private const string DllName = "Scripts.dll";
        private readonly IProjectPath _projectPath = projectPath;
        private readonly string dllPath = Path.Combine(projectPath.RootDirectory, DllName);
        private string RandomDllName => Path.Combine(_projectPath.RootDirectory, Path.GetRandomFileName() + DllName);


        private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp12);
        private static readonly CSharpCompilationOptions _compilationOptions = new
        (
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release
        );


        public string TryCompile()
        {
            var sourceFiles = Directory.EnumerateFiles(_projectPath.RootDirectory, CsSearch, SearchOption.AllDirectories);
            var trees = sourceFiles.Select(x =>
            {
                using var stream = File.OpenRead(x);
                return CSharpSyntaxTree.ParseText(SourceText.From(stream), _parseOptions);
            });

            MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            MetadataReference engineLib = MetadataReference.CreateFromFile(typeof(IRuntime).Assembly.Location);

            string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            List<MetadataReference> references = [mscorlib, engineLib];

            references.AddRange(Assembly.
                GetEntryAssembly()!.
                GetReferencedAssemblies().
                Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            references.AddRange(trees.
                Select(tree => tree.GetRoot().ChildNodes().
                OfType<UsingDirectiveSyntax>().
                Where(x => x.Name != null).
                Select(x => x.Name)).
            SelectMany(s => s).
            Select(u => Path.Combine(assemblyPath, u!.ToString() + ".dll")).
            Where(File.Exists).
            Select(p => MetadataReference.CreateFromFile(p)));

            var compilation = CSharpCompilation.Create(DllName, trees, references, _compilationOptions);

            var dllPath = RandomDllName;
            var result = compilation.Emit(dllPath);

            foreach (var item in result.Diagnostics)
                Debug.WriteLine(item.GetMessage());

            return dllPath;
        }
    }
}