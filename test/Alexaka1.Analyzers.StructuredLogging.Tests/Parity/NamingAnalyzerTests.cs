// Copyright (c) 2026 alexaka1

using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class NamingAnalyzerTests
{
    [Fact]
    public Task Pascal_invalid()
    {
        return AnalyzerTestHost.VerifyAsync("""
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
        return AnalyzerTestHost.VerifyAsync("""
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
        return AnalyzerTestHost.VerifyAsync("""
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
        return AnalyzerTestHost.VerifyAsync("""
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
        return AnalyzerTestHost.VerifyAsync("""
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
            """
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
            """
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
            """
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
            """
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
            """
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
    public Task Camel_case_context_property()
    {
        return AnalyzerTestHost.VerifyAsync(
            """
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
            """
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
            editorConfig: """
            dotnet_code_quality.AASL0009.property_naming = camel_case
            dotnet_code_quality.AASL0010.property_naming = snake_case
            """);
    }

    [Fact]
    public Task Context_property_naming()
    {
        return AnalyzerTestHost.VerifyAsync("""
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
    public Task Context_interpolated_name_is_ignored()
    {
        return AnalyzerTestHost.VerifyAsync("""
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
