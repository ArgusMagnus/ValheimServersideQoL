using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ServersideQoL.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoOptionalParametersInPublicApiAnalyzer : DiagnosticAnalyzer
{
  public const string DiagnosticId = "ARG0003";

  static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    "Avoid optional parameters in public API",
    "Externally visible method '{0}' has an optional parameter '{1}'",
    "Compatibility",
    DiagnosticSeverity.Warning,
    isEnabledByDefault: false,
    description: "Optional parameters in externally visible APIs can make signature changes source-compatible while breaking binary compatibility.");

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

  public override void Initialize(AnalysisContext context)
  {
    context.EnableConcurrentExecution();
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

    context.RegisterSymbolAction(static context =>
    {
      var method = (IMethodSymbol)context.Symbol;

      if (!IsExternallyVisible(method))
        return;

      foreach (var parameter in method.Parameters)
      {
        if (!parameter.HasExplicitDefaultValue)
          continue;

        if (parameter.GetAttributes().Any(static x => x.AttributeClass?.ToDisplayString()
          is "System.Runtime.CompilerServices.CallerFilePathAttribute" or "System.Runtime.CompilerServices.CallerLineNumberAttribute"))
          continue;

        context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Locations.FirstOrDefault(), method.Name, parameter.Name));
      }
    }, SymbolKind.Method);

    static bool IsExternallyVisible(ISymbol symbol)
    {
      for (var current = symbol; current is { Kind: not SymbolKind.Namespace }; current = current.ContainingSymbol)
      {
        if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
          return false;
      }

      return true;
    }
  }
}
