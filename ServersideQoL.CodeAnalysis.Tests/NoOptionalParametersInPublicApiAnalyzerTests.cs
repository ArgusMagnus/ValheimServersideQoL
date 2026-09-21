using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ServersideQoL.CodeAnalysis.NoOptionalParametersInPublicApiAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace ServersideQoL.CodeAnalysis.Tests;

[TestClass]
public sealed class NoOptionalParametersInPublicApiAnalyzerTests
{
  [TestMethod]
  public async Task TestOk()
  {
    const string TestCode = $$"""
      using System;
      using System.Linq.Expressions;

      class Class
      {
        public void Test(int i = 0) { }
      }

      public class Class2
      {
        private protected void Test(int i = 0) { }
      }
      """;

    await Verifier.VerifyAnalyzerAsync(TestCode);
  }

  [TestMethod]
  public async Task TestWarning()
  {
    const string TestCode = $$"""
      using System;
      using System.Linq.Expressions;

      public class Class
      {
        public void Test(int i = 0) { }
      }
      """;

    await Verifier.VerifyAnalyzerAsync(TestCode, new DiagnosticResult(NoOptionalParametersInPublicApiAnalyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(6, 24, 6, 25));
  }
}
