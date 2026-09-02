using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class NamingAnalyzerTests
{
    [Fact]
    public Task Constant_interpolated_text_reports_template_hole()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information($"user {|AASL0009:{{userId}}|}");
                                }
                            }
                            """);
    }

    [Fact]
    public Task Pascal_invalid()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{|AASL0009:{myProperty}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Pascal_valid()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{MyProperty}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Destructured_valid()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{@MyProperty}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Dot_in_name()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{|AASL0009:{My.Property}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Space_in_name()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{|AASL0009:{My Property}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Elastic_naming()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{|AASL0009:{myProperty}|}", 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = elastic_naming");
    }

    [Fact]
    public Task Camel_case_template_property()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{myProperty}", 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = camel_case");
    }

    [Fact]
    public Task Unprefixed_editorconfig_keys_are_ignored()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{|AASL0009:{myProperty}|}", 1);
                                }
                            }
                            """,
            editorConfig: "structured_logging_property_naming = camel_case");
    }

    [Fact]
    public Task Elastic_naming_rule_scoped_key()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{|AASL0009:{myProperty}|}", 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL0009.property_naming = elastic_naming");
    }

    [Fact]
    public Task Ignored_regex()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{MY_IGNORED.Property_}", 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.ignored_properties_regex = MY_.*");
    }

    [Fact]
    public async Task Timed_out_ignored_regex_does_not_throw()
    {
        var propertyName = new string('a', 5_000) + "b";
        var source = $$"""
                       using Serilog;
                       public static class Program
                       {
                           public static void Main()
                           {
                               Log.Logger.Information("{{{propertyName}}}", 1);
                           }
                       }
                       """;

        var outcome = await AnalyzerTestHost.AnalyzeAsync(
            source,
            editorConfig: "dotnet_code_quality.AASL.ignored_properties_regex = (a+)+$",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(outcome.Diagnostics, d => d.Id == "AASL0009");
    }

    [Fact]
    public Task Camel_case_context_property()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog.Context;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    LogContext.PushProperty({|AASL0010:"Test"|}, 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL0010.property_naming = camel_case");
    }

    [Fact]
    public Task Conflicting_rule_scoped_naming_is_isolated()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            using Serilog.Context;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{myProperty}", 1);
                                    LogContext.PushProperty({|AASL0010:"myProperty"|}, 1);
                                }
                            }
                            """,
            editorConfig: /*lang=editorconfig*/ """
                                                dotnet_code_quality.AASL0009.property_naming = camel_case
                                                dotnet_code_quality.AASL0010.property_naming = snake_case
                                                """);
    }

    [Fact]
    public Task Context_property_naming()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog.Context;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     LogContext.PushProperty({|AASL0010:"test"|}, 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task SemanticConventions_context_names_are_valid()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog.Context;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    LogContext.PushProperty("service.name", "api");
                                    LogContext.PushProperty("http.request.method", "GET");
                                    LogContext.PushProperty("http.response.status_code", 200);
                                    LogContext.PushProperty("exception.type", "IOException");
                                    LogContext.PushProperty("db.system.name", "postgresql");
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_template_names_are_valid()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("Call {http.request.method} {url.scheme}", "GET", "https");
                                    Log.Logger.Information("Status {http.response.status_code}", 200);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_does_not_warn_on_mel_log_extensions()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static class Program
                            {
                                public static void Main(ILogger logger)
                                {
                                    logger.LogInformation("Call {http.request.method}", "GET");
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_alias_semconv()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog.Context;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    LogContext.PushProperty("service.name", "api");
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semconv");
    }

    [Fact]
    public Task SemanticConventions_pascal_context_property()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog.Context;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    LogContext.PushProperty({|AASL0010:"OrderId"|}, 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_pascal_template_property()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{|AASL0009:{OrderId}|}", 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_scoped_to_context_rule()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            using Serilog.Context;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{OrderId}", 1);
                                    LogContext.PushProperty("service.name", "api");
                                    LogContext.PushProperty({|AASL0010:"OrderId"|}, 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL0010.property_naming = semantic_conventions");
    }

    [Fact]
    public Task Elastic_naming_rewrites_underscore_components()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{|AASL0009:{http.response.status_code}|}", 200);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = elastic_naming");
    }

    [Fact]
    public Task SemanticConventions_non_ascii_is_rewritten()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{|AASL0009:{MyCafé}|}", 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task SemanticConventions_leading_digit_is_not_flagged()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{1name}", 1);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task Context_interpolated_name_is_ignored()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog.Context;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     var x = "t";
                                                                     LogContext.PushProperty($"{x}est", 1);
                                                                 }
                                                             }
                                                             """);
    }
}
