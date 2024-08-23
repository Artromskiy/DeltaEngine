using DeltaGen.Attributes;
using Microsoft.CodeAnalysis;

namespace DeltaGen;
internal static class DiagnosticHelper
{
    private const string Prefix = "DE";
    private const string id0001 = $"{Prefix}0001";

    public static void ReportNotPartial(this SourceProductionContext ctx, Location location)
    {
        const string description = $"Types marked with {nameof(SystemAttribute)} and it's containing types must be specified with partial keyword";
        const string title = "Systems must be specified with partial keyword";
        var descriptor = new DiagnosticDescriptor(id0001, title, description, string.Empty, DiagnosticSeverity.Warning, true);
        var error = Diagnostic.Create(descriptor, location);
        ctx.ReportDiagnostic(error);
    }
}
