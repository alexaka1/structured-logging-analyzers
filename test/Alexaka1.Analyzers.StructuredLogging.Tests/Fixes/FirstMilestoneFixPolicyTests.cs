using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class FirstMilestoneFixPolicyTests
{
    private static readonly Type[] CodeFixProviders =
    [
        typeof(AddDestructuringCodeFixProvider),
        typeof(RenameTemplatePropertyCodeFixProvider),
        typeof(RenameContextPropertyCodeFixProvider),
        typeof(RemoveTrailingPeriodCodeFixProvider),
        typeof(ConvertInterpolatedTemplateCodeFixProvider)
    ];

    [Fact]
    public void Withheld_rules_are_not_registered_on_any_provider()
    {
        string[] withheld =
        [
            DiagnosticIds.ComplexObjectInContextShouldBeDestructured,
            DiagnosticIds.ContextualLoggerMismatch,
            DiagnosticIds.ExceptionPassedAsTemplateArgument,
            DiagnosticIds.DuplicateTemplateProperty,
            DiagnosticIds.GeneratedLoggingCannotUseSemanticConventions
        ];

        foreach (var type in CodeFixProviders)
        {
            var provider = (CodeFixProvider)Activator.CreateInstance(type)!;
            Assert.Empty(provider.FixableDiagnosticIds.Intersect(withheld, StringComparer.Ordinal));
        }
    }

    [Fact]
    public Task AASL0003_has_no_code_fix()
    {
        return VerifyNoFixesAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    {|AASL0003:LogContext.PushProperty("Test", new Random())|};
                }
            }
            """,
            "AASL0003");
    }

    [Fact]
    public Task AASL0004_has_no_code_fix()
    {
        return VerifyNoFixesAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                public A({|AASL0004:ILogger<B>|} log) { }
            }
            class B { }
            """,
            "AASL0004");
    }

    [Fact]
    public Task AASL0005_has_no_code_fix()
    {
        return VerifyNoFixesAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{One} {Exc}", 1, {|AASL0005:new Exception()|});
                }
            }
            """,
            "AASL0005");
    }

    [Fact]
    public Task AASL0006_has_no_code_fix()
    {
        return VerifyNoFixesAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0006:{Test}|} {|AASL0006:{Test}|}", 1, 2);
                }
            }
            """,
            "AASL0006");
    }

    [Fact]
    public Task AASL0012_has_no_code_fix()
    {
        return VerifyNoFixesAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [{|AASL0012:LoggerMessage|}(EventId = 1, Level = LogLevel.Information, Message = "Call {http.request.method}")]
                public static partial void RequestStarted(ILogger logger, string method);
            }
            """,
            "AASL0012",
            "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    private static async Task VerifyNoFixesAsync(string source, string diagnosticId, string? editorConfig = null)
    {
        foreach (var type in CodeFixProviders)
        {
            await AnalyzerTestHost.VerifyNoFixAsync(source, diagnosticId, type, editorConfig).ConfigureAwait(false);
        }
    }
}
