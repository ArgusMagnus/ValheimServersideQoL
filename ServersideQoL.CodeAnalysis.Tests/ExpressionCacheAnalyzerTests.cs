using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ServersideQoL.CodeAnalysis.ExpressionCacheAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace ServersideQoL.CodeAnalysis.Tests;

[TestClass]
public sealed class ExpressionCacheAnalyzerTests
{
  [TestMethod]
  public async Task TestOk()
  {
    const string TestCode = $$"""
      class Class
      {
        [{{nameof(MustBeOnUniqueLineAttribute)}}]
        Class Unique() { return this; }
        void NotUnique() { }

        void Test()
        {
          Unique();
          Unique();
          NotUnique(); NotUnique();
          Unique()
              .Unique();
        }
      }
      sealed class {{nameof(MustBeOnUniqueLineAttribute)}} : System.Attribute;
      """;

    await Verifier.VerifyAnalyzerAsync(TestCode);
  }

  [TestMethod]
  public async Task TestError()
  {
    const string TestCode = $$"""
      class Class
      {
        [{{nameof(MustBeOnUniqueLineAttribute)}}]
        Class Unique() { return this; }

        void Test()
        {
          Unique(); Unique();
          Unique().Unique();
        }
      }
      sealed class {{nameof(MustBeOnUniqueLineAttribute)}} : System.Attribute;
      """;

    await Verifier.VerifyAnalyzerAsync(TestCode,
      new DiagnosticResult(ExpressionCacheAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithSpan(8, 15, 8, 23),
      new DiagnosticResult(ExpressionCacheAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithSpan(9, 5, 9, 13));
  }
}
