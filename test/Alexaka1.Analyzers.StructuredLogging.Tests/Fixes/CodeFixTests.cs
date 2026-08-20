// Copyright (c) 2026 alexaka1

using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

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
                    Log.Logger.Information("{|AASL0001:{MyProperty}|}", new { Test = 1 });
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
            "AASL0001",
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
                    Log.Logger.Information("Escaped \r\n {|AASL0001:{MyProperty}|} \r\n string", new { Test = 1 });
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
            "AASL0001",
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
                    Log.Logger.Information("{|AASL0002:{MyProperty}|}", new Random());
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
            "AASL0002",
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
                    Log.Logger.Information("Test {|AASL0009:{myProperty}|} prop", 1);
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
            "AASL0009",
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
                    Log.Logger.Information("Test {|AASL0009:{@myProperty}|} prop", 1);
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
            "AASL0009",
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
                    Log.Logger.Information("Test" + " {|AASL0009:{myProperty}|} prop", 1);
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
            "AASL0009",
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
                    LogContext.PushProperty({|AASL0010:"test"|}, 1);
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
            "AASL0010",
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
                    Log.Logger.Information("Test {Property} prop{|AASL0011:.|}", 1);
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
            "AASL0011",
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
                    logger.LogInformation({|AASL0007:$"Processed {order.Id}"|});
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
            "AASL0007",
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
                    logger.LogInformation({|AASL0007:$"User '{name}'"|});
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
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }
}
