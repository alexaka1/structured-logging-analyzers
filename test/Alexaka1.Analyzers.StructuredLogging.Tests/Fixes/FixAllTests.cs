using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class FixAllTests
{
    [Fact]
    public Task Add_destructuring_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0001:{First}|}", new { Test = 1 });
                    Log.Logger.Information("{|AASL0001:{Second}|}", new { Test = 2 });
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{@First}", new { Test = 1 });
                    Log.Logger.Information("{@Second}", new { Test = 2 });
                }
            }
            """,
            "AASL0001",
            typeof(AddDestructuringCodeFixProvider));
    }

    [Fact]
    public Task Rename_template_properties_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0009:{myProperty}|} {|AASL0009:{otherName}|}", 1, 2);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{MyProperty} {OtherName}", 1, 2);
                }
            }
            """,
            "AASL0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_context_properties_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    LogContext.PushProperty({|AASL0010:"first"|}, 1);
                    LogContext.PushProperty({|AASL0010:"second"|}, 2);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    LogContext.PushProperty("First", 1);
                    LogContext.PushProperty("Second", 2);
                }
            }
            """,
            "AASL0010",
            typeof(RenameContextPropertyCodeFixProvider));
    }

    [Fact]
    public Task Remove_trailing_periods_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("First {Property}{|AASL0011:.|}", 1);
                    Log.Logger.Information("Second {Property}{|AASL0011:.|}", 2);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("First {Property}", 1);
                    Log.Logger.Information("Second {Property}", 2);
                }
            }
            """,
            "AASL0011",
            typeof(RemoveTrailingPeriodCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolations_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order, string name)
                {
                    logger.LogInformation({|AASL0007:$"Processed {order.Id}"|});
                    logger.LogInformation({|AASL0007:$"User {name}"|});
                }
            }
            class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order, string name)
                {
                    logger.LogInformation("Processed {Id}", order.Id);
                    logger.LogInformation("User {Name}", name);
                }
            }
            class Order { public int Id { get; set; } }
            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }
}
