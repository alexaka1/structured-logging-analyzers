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
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0001:{MyProperty}|}", new { Test = 1 });
                }
            }
            """,
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Escaped \r\n {|AASL0001:{MyProperty}|} \r\n string", new { Test = 1 });
                }
            }
            """,
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {|AASL0009:{myProperty}|} prop", 1);
                }
            }
            """,
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {|AASL0009:{@myProperty}|} prop", 1);
                }
            }
            """,
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test" + " {|AASL0009:{myProperty}|} prop", 1);
                }
            }
            """,
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    LogContext.PushProperty({|AASL0010:"test"|}, 1);
                }
            }
            """,
            /*lang=csharp*/ """
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
    public Task Rename_context_property_semantic_conventions()
    {
        return AnalyzerTestHost.VerifyFixAsync(
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
            /*lang=csharp*/ """
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    LogContext.PushProperty("order_id", 1);
                }
            }
            """,
            "AASL0010",
            typeof(RenameContextPropertyCodeFixProvider),
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task Rename_template_property_semantic_conventions()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0009:{http.response.StatusCode}|}", 200);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{http.response.status_code}", 200);
                }
            }
            """,
            "AASL0009",
            typeof(RenameTemplatePropertyCodeFixProvider),
            editorConfig: "dotnet_code_quality.AASL.property_naming = semantic_conventions");
    }

    [Fact]
    public Task Remove_trailing_period()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Test {Property} prop{|AASL0011:.|}", 1);
                }
            }
            """,
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
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
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 2);
    }

    [Fact]
    public Task Convert_interpolation_qualified_names()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
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
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation("Processed {OrderId}", order.Id);
                }
            }
            class Order { public int Id { get; set; } }
            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            codeActionIndex: 1,
            expectedActionCount: 2);
    }

    [Fact]
    public Task Convert_interpolation_conditional_access()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation({|AASL0007:$"Processed {order?.Id}"|});
                }
            }
            class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation("Processed {Id}", order?.Id);
                }
            }
            class Order { public int Id { get; set; } }
            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 2);
    }

    [Fact]
    public Task Convert_interpolation_conditional_access_qualified_names()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation({|AASL0007:$"Processed {order?.Id}"|});
                }
            }
            class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation("Processed {OrderId}", order?.Id);
                }
            }
            class Order { public int Id { get; set; } }
            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            codeActionIndex: 1,
            expectedActionCount: 2);
    }

    [Fact]
    public Task Convert_interpolation_identifier_has_single_action()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string name)
                {
                    logger.LogInformation({|AASL0007:$"User {name}"|});
                }
            }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string name)
                {
                    logger.LogInformation("User {Name}", name);
                }
            }
            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Convert_interpolation_preserves_leading_trivia()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation(/* keep */ {|AASL0007:$"Processed {order.Id}"|});
                }
            }
            class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation(/* keep */ "Processed {Id}", order.Id);
                }
            }
            class Order { public int Id { get; set; } }
            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_keeps_exception_overload()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex, Order order)
                {
                    logger.LogError(ex, {|AASL0007:$"Processed {order.Id}"|});
                }
            }
            class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex, Order order)
                {
                    logger.LogError(ex, "Processed {Id}", order.Id);
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
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string name)
                {
                    logger.LogInformation({|AASL0007:$"User '{name}'"|});
                }
            }
            """,
            /*lang=csharp*/ """
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
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Convert_interpolation_nameof()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation({|AASL0007:$"Processed {nameof(order.Id)}"|});
                }
            }
            class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation("Processed {Id}", nameof(order.Id));
                }
            }
            class Order { public int Id { get; set; } }
            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 2);
    }
}
