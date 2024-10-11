using Microsoft.CodeAnalysis;

namespace DeltaGenCore;
public static class DiagnosticHelper
{
    private const string Prefix = "DE";
    private const string id0001 = $"{Prefix}0001";

    public static void ReportNotPartial(this SourceProductionContext ctx, Location location, string attributeName)
    {
        string description = $"Types marked with {attributeName} and it's containing types must be specified with partial keyword";
        const string title = "Type must be specified with partial keyword";
        var descriptor = new DiagnosticDescriptor(id0001, title, description, string.Empty, DiagnosticSeverity.Warning, true);
        var error = Diagnostic.Create(descriptor, location);
        ctx.ReportDiagnostic(error);
    }
}
