using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ServersideQoL.CodeAnalysis.ExpressionFactoryAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace ServersideQoL.CodeAnalysis.Tests;

[TestClass]
public sealed class ExpressionFactoryAnalyzerTests
{
  [TestMethod]
  public async Task Test()
  {
    const string TestCode = $$"""
      using System;
      using System.Linq.Expressions;

      class Class
      {
        public int m_int;
        void Test()
        {
          Func<Expression<Func<Class, int>>> f1 = static () => x => x.m_int;
          Func<Expression<Func<Class, int>>> f2 = () => x => x.m_int;
        }
      }
      """;

    await Verifier.VerifyAnalyzerAsync(TestCode, new DiagnosticResult(ExpressionFactoryAnalyzer.DiagnosticId, DiagnosticSeverity.Error).WithSpan(10, 45, 10, 63));
  }
}
