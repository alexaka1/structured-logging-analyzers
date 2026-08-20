// Copyright (c) 2026 alexaka1

using StructuredLogging.CodeFixes;
using StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace StructuredLogging.Tests.Fixes;

public sealed class CodeFixTests
{
    [Fact]
    public Task Add_destructuring_anonymous()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|SLA0001:{MyProperty}|}", new { Test = 1 });
                }
            }
            """,
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{@MyProperty}", new { Test = 1 });
                }
            }
            """,
            "SLA0001",
            typeof(AddDestructuringCodeFixProvider));
    }

    [Fact]
    public Task Add_destructuring_escaped_string()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Escaped \r\n {|SLA0001:{MyProperty}|} \r\n string", new { Test = 1 });
                }
            }
            """,
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Escaped \r\n {@MyProperty} \r\n string", new { Test = 1 });
                }
            }
            """,
            "SLA0001",
            typeof(AddDestructuringCodeFixProvider));
    }

    [Fact]
    public Task Add_destructuring_complex()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using System;
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|SLA0002:{MyProperty}|}", new Random());
                }
            }
            """,
            """
            using System;
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{@MyProperty}", new Random());
                }
            }
            """,
            "SLA0002",
            typeof(AddDestructuringCodeFixProvider));
    }

    [Fact]
    public Task Rename_template_property()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {|SLA0009:{myProperty}|} prop", 1);
                }
            }
            """,
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {MyProperty} prop", 1);
                }
            }
            """,
            "SLA0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_destructured_template_property()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {|SLA0009:{@myProperty}|} prop", 1);
                }
            }
            """,
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {@MyProperty} prop", 1);
                }
            }
            """,
            "SLA0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_concatenated_template_property()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test" + " {|SLA0009:{myProperty}|} prop", 1);
                }
            }
            """,
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test" + " {MyProperty} prop", 1);
                }
            }
            """,
            "SLA0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_context_property()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    LogContext.PushProperty({|SLA0010:"test"|}, 1);
                }
            }
            """,
            """
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    LogContext.PushProperty("Test", 1);
                }
            }
            """,
            "SLA0010",
            typeof(RenameContextPropertyCodeFixProvider));
    }

    [Fact]
    public Task Remove_trailing_period()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {Property} prop{|SLA0011:.|}", 1);
                }
            }
            """,
            """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {Property} prop", 1);
                }
            }
            """,
            "SLA0011",
            typeof(RemoveTrailingPeriodCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation({|SLA0007:$"Processed {order.Id}"|});
                }
            }
            class Order { public int Id { get; set; } }
            """,
            """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation("Processed {Id}", order.Id);
                }
            }
            class Order { public int Id { get; set; } }
            """,
            "SLA0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_preserves_quotes()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string name)
                {
                    logger.LogInformation({|SLA0007:$"User '{name}'"|});
                }
            }
            """,
            """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string name)
                {
                    logger.LogInformation("User '{Name}'", name);
                }
            }
            """,
            "SLA0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }
}
