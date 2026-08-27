using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class FirstMilestoneFixPolicyTests
{
    private static readonly Type[] CodeFixProviders =
    [
        typeof(AddDestructuringCodeFixProvider),
        typeof(AddContextDestructuringCodeFixProvider),
        typeof(ReplaceContextualLoggerTypeCodeFixProvider),
        typeof(MoveExceptionArgumentCodeFixProvider),
        typeof(RenameTemplatePropertyCodeFixProvider),
        typeof(RenameContextPropertyCodeFixProvider),
        typeof(RemoveTrailingPeriodCodeFixProvider),
        typeof(ConvertInterpolatedTemplateCodeFixProvider)
    ];

    [Fact]
    public void AASL0012_is_not_registered_on_any_provider()
    {
        foreach (var type in CodeFixProviders)
        {
            var provider = (CodeFixProvider)Activator.CreateInstance(type)!;
            Assert.DoesNotContain(
                DiagnosticIds.GeneratedLoggingCannotUseSemanticConventions,
                provider.FixableDiagnosticIds);
        }
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

    [Fact]
    public Task LoggerMessage_duplicate_properties_have_no_code_fix()
    {
        return VerifyNoFixesAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{|AASL0006:{OrderId}|} {|AASL0006:{OrderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            "AASL0006");
    }

    private static async Task VerifyNoFixesAsync(string source, string diagnosticId, string? editorConfig = null)
    {
        foreach (var type in CodeFixProviders)
        {
            await Infrastructure.AnalyzerTestHost.VerifyNoFixAsync(source, diagnosticId, type, editorConfig)
                .ConfigureAwait(false);
        }
    }
}
