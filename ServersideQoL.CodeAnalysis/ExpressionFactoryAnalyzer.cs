using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Valheim.ServersideQoL.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExpressionFactoryAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ARG0002";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Expression factories must be static",
        "Expression factories must be static",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];
    static readonly string __expressionTypeStartsWith = $"{typeof(Expression).FullName}<";


    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(static context =>
        {
            var lambda = (ParenthesizedLambdaExpressionSyntax)context.Node;
            if (lambda.Body is not SimpleLambdaExpressionSyntax expressionLambda)
                return;
            if (context.SemanticModel.GetSymbolInfo(lambda).Symbol is not IMethodSymbol { IsStatic: false } symbol || !symbol.ReturnType.ToDisplayString().StartsWith(__expressionTypeStartsWith))
                return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, lambda.GetLocation()));

        }, SyntaxKind.ParenthesizedLambdaExpression);
    }
}