using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class PositionalRenameFixTests
{
    [Fact]
    public Task Rename_from_identifier_argument()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", orderId);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information("{OrderId}", orderId);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Rename_from_member_access_leaf_name()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", order.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{Id}", order.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 2);
    }

    [Fact]
    public Task Rename_from_member_access_qualified_name()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", order.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{OrderId}", order.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            codeActionIndex: 1,
            expectedActionCount: 2);
    }

    [Fact]
    public Task Rename_from_conditional_access()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", order?.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{Id}", order?.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 2);
    }

    [Fact]
    public Task Rename_preserves_format_and_destructuring()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(decimal amount)
                {
                    Log.Logger.Information("{|AASL0008:{@0:N2}|}", amount);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(decimal amount)
                {
                    Log.Logger.Information("{@Amount:N2}", amount);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Rename_in_verbatim_and_raw_strings()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """"
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information(@"{|AASL0008:{0}|}", orderId);
                    Log.Logger.Information("""{|AASL0008:{0}|}""", orderId);
                }
            }
            """",
            /*lang=csharp*/ """"
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information(@"{OrderId}", orderId);
                    Log.Logger.Information("""{0}""", orderId);
                }
            }
            """",
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 1);
    }

    [Fact]
    public Task Rename_named_reordered_argument()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information(propertyValue: orderId, messageTemplate: "{|AASL0008:{0}|}");
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information(propertyValue: orderId, messageTemplate: "{OrderId}");
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Rename_microsoft_extensions_logging()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string orderId)
                {
                    logger.LogInformation("{|AASL0008:{0}|}", orderId);
                }
            }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string orderId)
                {
                    logger.LogInformation("{OrderId}", orderId);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Rename_microsoft_extensions_logging_cast_argument()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string orderId)
                {
                    logger.LogInformation("{|AASL0008:{0}|}", (object)orderId);
                }
            }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string orderId)
                {
                    logger.LogInformation("{OrderId}", (object)orderId);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Rename_microsoft_extensions_logging_keeps_hole_order_with_cast()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string orderId, string name)
                {
                    logger.LogInformation("{|AASL0008:{0}|} {|AASL0008:{1}|}", (object)orderId, name);
                }
            }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string orderId, string name)
                {
                    logger.LogInformation("{OrderId} {1}", (object)orderId, name);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 0);
    }

    [Fact]
    public Task Rename_nlog()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using NLog;
            class C
            {
                void M(Logger logger, string orderId)
                {
                    logger.Info("{|AASL0008:{0}|}", orderId);
                }
            }
            """,
            /*lang=csharp*/ """
            using NLog;
            class C
            {
                void M(Logger logger, string orderId)
                {
                    logger.Info("{OrderId}", orderId);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Rename_zlogger()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            using ZLogger;
            class A
            {
                public A(ILogger<A> log, string orderId)
                {
                    log.ZLogInformation("{|AASL0008:{0}|}", orderId);
                }
            }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            using ZLogger;
            class A
            {
                public A(ILogger<A> log, string orderId)
                {
                    log.ZLogInformation("{OrderId}", orderId);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 1);
    }

    [Fact]
    public Task Rename_uses_configured_camel_case()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", orderId);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information("{orderId}", orderId);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            editorConfig: "dotnet_code_quality.AASL.property_naming = camel_case",
            expectedActionCount: 1);
    }

    [Fact]
    public Task Literal_argument_has_no_rename()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", 1);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Anonymous_object_argument_has_no_rename()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", new { Test = 1 });
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Params_array_variable_has_no_rename()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(object[] values)
                {
                    Log.Logger.Information("{|AASL0008:{0}|} {|AASL0008:{1}|}", values);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Single_hole_params_array_variable_has_no_rename()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(object[] values)
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", values);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Logger_message_define_has_no_rename()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M()
                {
                    _ = LoggerMessage.Define<int>(LogLevel.Information, new EventId(1), "{|AASL0008:{0}|}");
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Logger_message_define_scope_has_no_rename()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M()
                {
                    _ = LoggerMessage.DefineScope<int>("{|AASL0008:{0}|}");
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_one_hole_when_the_other_argument_is_a_literal()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information("{|AASL0008:{0}|} {|AASL0008:{1}|}", orderId, 1);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId)
                {
                    Log.Logger.Information("{OrderId} {1}", orderId, 1);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 0);
    }

    [Fact]
    public Task Rename_from_nameof()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", nameof(order.Id));
                }
            }
            public sealed class Order { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{Id}", nameof(order.Id));
                }
            }
            public sealed class Order { public int Id { get; set; } }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            expectedActionCount: 2);
    }
}
