// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis.CSharp;
using Alexaka1.Analyzers.StructuredLogging;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.SourceGenerated;

public sealed class LoggerMessageAnalyzerTests
{
    [Fact]
    public Task Static_partial_named_message_trailing_period()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0009:{orderId}|}{|AASL0011:.|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public Task Extension_method()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(this ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public Task SemanticConventions_name_is_valid_for_template_naming_and_warns_on_generated_logging()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [{|AASL0012:LoggerMessage|}(EventId = 1, Level = LogLevel.Information, Message = "Call {http.request.method}")]
                public static partial void RequestStarted(ILogger logger, string method);
            }
            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_pascal_name_is_flagged()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [{|AASL0012:LoggerMessage|}(EventId = 1, Level = LogLevel.Information, Message = "Order {|AASL0009:{OrderId}|} started")]
                public static partial void OrderStarted(ILogger logger, string orderId);
            }
            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_warns_on_logger_message_even_without_holes()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [{|AASL0012:LoggerMessage|}(EventId = 1, Level = LogLevel.Information, Message = "Started")]
                public static partial void Started(ILogger logger);
            }
            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_scoped_to_context_rule_does_not_warn_on_logger_message()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Call {Method}")]
                public static partial void RequestStarted(ILogger logger, string method);
            }
            """,
            editorConfig: "dotnet_code_quality.AASL0010.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_scoped_to_template_naming_warns_on_logger_message()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [{|AASL0012:LoggerMessage|}(EventId = 1, Level = LogLevel.Information, Message = "Call {http.request.method}")]
                public static partial void RequestStarted(ILogger logger, string method);
            }
            """,
            editorConfig: "dotnet_code_quality.AASL0009.property_naming = semantic_conventions");
    }

    [Fact]
    public Task Instance_logger_field()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public partial class Worker
            {
                private readonly ILogger _logger;
                public Worker(ILogger logger) { _logger = logger; }

                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0009:{orderId}|}")]
                public partial void ProcessingOrder(int orderId);
            }
            """);
    }

    [Fact]
    public Task Primary_constructor_logger()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public partial class Worker(ILogger logger)
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0009:{orderId}|}")]
                public partial void ProcessingOrder(int orderId);
            }
            """);
    }

    [Fact]
    public Task Dynamic_log_level_parameter()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Message = "Processing {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, LogLevel level, int orderId);
            }
            """);
    }

    [Fact]
    public Task Constructor_arguments()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(1, LogLevel.Information, "Processing {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public Task Omitted_message_is_not_flagged()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information)]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public Task Reordered_parameters_match_by_name()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{|AASL0006:{OrderId}|} {|AASL0006:{OrderId}|}")]
                public static partial void ProcessingOrder(int orderId, ILogger logger);
            }
            """);
    }

    [Fact]
    public Task Case_insensitive_placeholder_is_not_a_missing_parameter()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public Task Format_specifier_is_preserved_and_not_destructured()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Value is {Value:E}")]
                public static partial void UsingFormatSpecifier(ILogger logger, double value);
            }
            """);
    }

    [Fact]
    public Task First_exception_placeholder_is_left_to_syslib()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using System;
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "M1 {Ex} {Ex2}")]
                public static partial void WarningLogMethod(ILogger logger, Exception ex, Exception ex2);
            }
            """);
    }

    [Fact]
    public Task Subsequent_exception_is_an_ordinary_template_parameter()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using System;
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 110, Level = LogLevel.Debug, Message = "M1 {Ex3} {|AASL0009:{ex2}|}")]
                public static partial void ValidLogMethod(ILogger logger, Exception ex, Exception ex2, Exception ex3);
            }
            """);
    }

    [Fact]
    public Task Generic_method()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Saw {|AASL0009:{value}|}{|AASL0011:.|}")]
                public static partial void Saw<T>(ILogger logger, T value);
            }
            """);
    }

    [Fact]
    public Task Invalid_non_partial_method_does_not_emit_exception_or_constant_rules()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0009:{orderId}|}")]
                public static void ProcessingOrder(ILogger logger, int orderId) { }
            }
            """);
    }

    [Fact]
    public Task Complex_object_is_not_flagged_for_logger_message()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public sealed class Order { }
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Order {Order}")]
                public static partial void ProcessingOrder(ILogger logger, Order order);
            }
            """);
    }

    [Fact]
    public Task Anonymous_object_is_not_flagged_for_logger_message()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Order {Order}")]
                public static partial void ProcessingOrder(ILogger logger, object order);
            }
            """);
    }

    [Fact]
    public Task Positional_placeholder_with_unambiguous_parameter()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0008:{0}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public Task Constant_message_exclusive_to_method()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                const string Msg = "Processing {|AASL0009:{orderId}|}{|AASL0011:.|}";

                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = Msg)]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public async Task Shared_constant_reports_but_is_not_rewritten()
    {
        const string source = """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                const string Msg = "Processing {orderId}";

                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = Msg)]
                public static partial void ProcessingOrder(ILogger logger, int orderId);

                [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = Msg)]
                public static partial void ProcessingOrderAgain(ILogger logger, int orderId);
            }
            """;
        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);
        var naming = diagnostics.Where(d => d.Id == "AASL0009").ToList();
        Assert.NotEmpty(naming);
        Assert.All(naming, d =>
        {
            Assert.True(d.Properties.TryGetValue("AllowRewrite", out var allow));
            Assert.Equal("false", allow);
        });
    }

    [Fact]
    public Task String_constructor_argument()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage("Processing {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, LogLevel level, int orderId);
            }
            """);
    }

    [Fact]
    public Task CSharp10_constructor_message()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(1, LogLevel.Information, "Processing {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            languageVersion: LanguageVersion.CSharp10);
    }

    [Fact]
    public Task Contextual_logger_still_applies_on_containing_type()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public partial class Worker
            {
                public Worker({|AASL0004:ILogger<Other>|} log) { }

                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Go")]
                public partial void Go();
            }
            public sealed class Other { }
            """);
    }

    [Fact]
    public Task Generated_implementation_does_not_suppress_or_duplicate_attribute_diagnostics()
    {
        const string generated = """
            using Microsoft.Extensions.Logging;

            // <auto-generated/>
            #nullable enable
            partial class Log
            {
                [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "8.0.10.46610")]
                private static readonly System.Action<ILogger, int, System.Exception?> __ProcessingOrderCallback =
                    LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, nameof(ProcessingOrder)), "Processing {orderId}.", new LogDefineOptions { SkipEnabledCheck = true });

                [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "8.0.10.46610")]
                public static partial void ProcessingOrder(ILogger logger, int orderId)
                {
                    __ProcessingOrderCallback(logger, orderId, null);
                }
            }
            """;

        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0009:{orderId}|}{|AASL0011:.|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            additionalSources: new[]
            {
                ("Microsoft.Extensions.Logging.Generators/LoggerMessage.g.cs", generated)
            });
    }

    [Fact]
    public Task Same_file_generated_implementation_is_not_double_reported()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);

                [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Extensions.Logging.Generators", "8.0.0")]
                public static partial void ProcessingOrder(ILogger logger, int orderId) { }
            }
            """);
    }

    [Fact]
    public Task Unused_parameter_is_left_to_syslib()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Hello")]
                public static partial void Hello(ILogger logger, int unused);
            }
            """);
    }

    [Fact]
    public Task Case_only_duplicate_is_left_to_syslib()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{OrderId} {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public Task Level_and_message_constructor()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(LogLevel.Information, "Processing {|AASL0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """);
    }

    [Fact]
    public async Task Shared_constant_trailing_period_is_not_rewritten()
    {
        const string source = """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                const string Msg = "Processing {OrderId}.";

                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = Msg)]
                public static partial void ProcessingOrder(ILogger logger, int orderId);

                [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = Msg)]
                public static partial void ProcessingOrderAgain(ILogger logger, int orderId);
            }
            """;
        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);
        var periods = diagnostics.Where(d => d.Id == "AASL0011").ToList();
        Assert.NotEmpty(periods);
        Assert.All(periods, d =>
        {
            Assert.True(d.Properties.TryGetValue("AllowRewrite", out var allow));
            Assert.Equal("false", allow);
        });
    }

    [Fact]
    public async Task Net6_through_current_logging_abstractions_recognize_classic_constructor()
    {
        const string source = """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(1, LogLevel.Information, "Processing {orderId}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """;

        var assemblies = AnalyzerTestHost.FindLoggingAbstractionsAssemblies();
        Assert.NotEmpty(assemblies);

        foreach (var (version, path) in assemblies)
        {
            var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(
                source,
                references: AnalyzerTestHost.CreateReferencesWithLoggingAbstractions(path));
            var naming = diagnostics.Where(d => d.Id == "AASL0009").ToList();
            Assert.True(naming.Count == 1, $"Expected AASL0009 against Microsoft.Extensions.Logging.Abstractions {version} at {path}.");
        }
    }
}

public sealed class LoggerMessageDefineTests
{
    [Fact]
    public Task Define_applies_style_rules_without_destructuring()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public sealed class Order { }
            public static class C
            {
                private static readonly System.Action<ILogger, Order, System.Exception?> s_log =
                    LoggerMessage.Define<Order>(LogLevel.Information, new EventId(1), "Order {|AASL0009:{order}|}{|AASL0011:.|}");
            }
            """);
    }

    [Fact]
    public Task SemanticConventions_warns_on_define()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static class C
            {
                private static readonly System.Action<ILogger, string, System.Exception?> s_log =
                    {|AASL0012:LoggerMessage.Define<string>|}(LogLevel.Information, new EventId(1), "Call {http.request.method}");
            }
            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_warns_on_define_scope()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static class C
            {
                private static readonly System.Func<ILogger, string, System.IDisposable?> s_scope =
                    {|AASL0012:LoggerMessage.DefineScope<string>|}("Starting {http.request.method}");
            }
            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_warns_on_define_when_template_is_not_constant()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
            using Microsoft.Extensions.Logging;
            public static class C
            {
                static string Format() => "Hi {Name}";
                static void M()
                {
                    _ = {|AASL0012:LoggerMessage.Define|}(LogLevel.Information, new EventId(1), {|AASL0007:Format()|});
                }
            }
            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task DefineScope_applies_naming()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static class C
            {
                private static readonly System.Func<ILogger, string, System.IDisposable?> s_scope =
                    LoggerMessage.DefineScope<string>("Starting {|AASL0009:{name}|}");
            }
            """);
    }

    [Fact]
    public Task Define_non_constant_template()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            public static class C
            {
                static string Format() => "Hi {Name}";
                static void M()
                {
                    _ = LoggerMessage.Define(LogLevel.Information, new EventId(1), {|AASL0007:Format()|});
                }
            }
            """);
    }
}
