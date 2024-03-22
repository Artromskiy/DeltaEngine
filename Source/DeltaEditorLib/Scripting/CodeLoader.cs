using Delta;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Reflection;

namespace DeltaEditorLib.Scripting
{
    public static class CodeLoader
    {
        private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp12);
        private static readonly CSharpCompilationOptions _compilationOptions = new
        (
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release
        );

        const string DllName = "Scripts.dll";

        public static void TryCompile(string sourceFilesLocation, string destinationDllLocation)
        {
            TestCompileFiles.CreateTestFile(sourceFilesLocation);

            DirectoryInfo d = new(sourceFilesLocation);
            string[] sourceFiles = Directory.GetFiles(sourceFilesLocation, "*.cs", SearchOption.AllDirectories);

            List<SyntaxTree> trees = new(sourceFiles.Length);
            foreach (string file in sourceFiles)
            {
                using var stream = File.OpenRead(file);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(SourceText.From(stream), _parseOptions);
                trees.Add(tree);
            }

            MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            MetadataReference engineLib = MetadataReference.CreateFromFile(typeof(Engine).Assembly.Location);

            string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            MetadataReference[] defaultReferences = [mscorlib, engineLib];

            var compilation = CSharpCompilation.Create(DllName,
                   trees,
                   defaultReferences,
                   _compilationOptions);

            List<MetadataReference> usedReferences = [];
            var usingDirectives = compilation.SyntaxTrees.
                Select(tree => tree.GetRoot().ChildNodes().
                OfType<UsingDirectiveSyntax>().
                Where(x => x.Name != null)).
            SelectMany(s => s);

            foreach (var item in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                usedReferences.Add(MetadataReference.CreateFromFile(Assembly.Load(item).Location));

            foreach (var u in usingDirectives)
            {
                var referencePath = Path.Combine(assemblyPath, u.Name!.ToString() + ".dll");
                if(File.Exists(referencePath))
                    usedReferences.Add(MetadataReference.CreateFromFile(referencePath));
            }
            
            compilation = compilation.AddReferences(usedReferences);
            var dllPath = Path.Combine(destinationDllLocation, DllName);
            var result = compilation.Emit(dllPath);

            var scriptsDllReference = MetadataReference.CreateFromFile(dllPath);

            foreach (var item in result.Diagnostics)
                Debug.WriteLine(item.GetMessage());
        }
    }
}