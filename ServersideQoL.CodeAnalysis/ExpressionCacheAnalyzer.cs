using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Valheim.ServersideQoL.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExpressionCacheAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ARG0001";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Caller line not unique",
        "Each call to this method must be on its own line",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(static context =>
        {
            var method = (MethodDeclarationSyntax)context.Node;

            HashSet<(ISymbol, int)>? lines = null;
            foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not { } symbol)
                    continue;
                if (!symbol.GetAttributes().Any(static x => x.AttributeClass?.Name is nameof(MustBeOnUniqueLineAttribute)))
                    continue;
                var location = invocation.GetLocation();
                var lineSpan = location.GetLineSpan();
                if (!(lines ??= []).Add((symbol, lineSpan.EndLinePosition.Line)))
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location));
            }
        }, SyntaxKind.MethodDeclaration);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class MustBeOnUniqueLineAttribute : Attribute;
