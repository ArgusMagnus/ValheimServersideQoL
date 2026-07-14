namespace Valheim.ServersideQoL.CodeAnalysis.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Valheim.ServersideQoL.CodeAnalysis.ExpressionCacheAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class ExpressionCacheAnalyzerTests
{
    [TestMethod]
    public async Task TestOk()
    {
        var testCode = $$"""
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

        await Verifier.VerifyAnalyzerAsync(testCode);
    }

    [TestMethod]
    public async Task TestError()
    {
        var testCode = $$"""
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

        await Verifier.VerifyAnalyzerAsync(testCode,
            new DiagnosticResult("ARG0001", DiagnosticSeverity.Error).WithSpan(8, 19, 8, 27),
            new DiagnosticResult("ARG0001", DiagnosticSeverity.Error).WithSpan(9, 9, 9, 17));
    }
}
