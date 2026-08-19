using Delta.Engine.Integration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Delta.Editor.Scripting;

public sealed class RoslynScriptCompiler : IScriptCompiler
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);
    private static readonly CSharpCompilationOptions CompilationOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        allowUnsafe: true,
        deterministic: true);

    public ScriptCompilationResult Compile(ScriptCompilationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Sources);
        ArgumentNullException.ThrowIfNull(request.References);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AssemblyName);

        var trees = request.Sources
            .Select(source => CSharpSyntaxTree.ParseText(
                SourceText.From(source.Text, Encoding.UTF8),
                ParseOptions,
                source.Path))
            .ToArray();
        var references = request.References
            .Select(reference =>
            {
                using Stream stream = reference.OpenRead();
                return MetadataReference.CreateFromStream(stream);
            })
            .ToArray();

        var compilation = CSharpCompilation.Create(
            request.AssemblyName,
            trees,
            references,
            CompilationOptions);

        using var assemblyStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        EmitResult emitResult = compilation.Emit(assemblyStream, pdbStream);
        var diagnostics = emitResult.Diagnostics
            .Select(ToDiagnostic)
            .ToArray();

        if (!emitResult.Success)
        {
            return new ScriptCompilationResult(
                false,
                ReadOnlyMemory<byte>.Empty,
                ReadOnlyMemory<byte>.Empty,
                diagnostics);
        }

        return new ScriptCompilationResult(
            true,
            assemblyStream.ToArray(),
            pdbStream.ToArray(),
            diagnostics);
    }

    private static ScriptCompilationDiagnostic ToDiagnostic(Diagnostic diagnostic)
    {
        string? sourcePath = null;
        int? line = null;
        int? column = null;

        if (diagnostic.Location != Location.None && diagnostic.Location.IsInSource)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            sourcePath = lineSpan.Path;
            line = lineSpan.StartLinePosition.Line + 1;
            column = lineSpan.StartLinePosition.Character + 1;
        }

        return new ScriptCompilationDiagnostic(
            diagnostic.Id,
            diagnostic.Severity switch
            {
                Microsoft.CodeAnalysis.DiagnosticSeverity.Hidden => ScriptDiagnosticSeverity.Hidden,
                Microsoft.CodeAnalysis.DiagnosticSeverity.Info => ScriptDiagnosticSeverity.Info,
                Microsoft.CodeAnalysis.DiagnosticSeverity.Warning => ScriptDiagnosticSeverity.Warning,
                _ => ScriptDiagnosticSeverity.Error,
            },
            diagnostic.GetMessage(),
            sourcePath,
            line,
            column);
    }
}
