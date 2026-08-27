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

    [Fact]
    public Task Convert_interpolations_qualified_names_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation({|AASL0007:$"Processed {order.Id}"|});
                    logger.LogInformation({|AASL0007:$"Counted {order.Total}"|});
                }
            }
            class Order { public int Id { get; set; } public int Total { get; set; } }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Order order)
                {
                    logger.LogInformation("Processed {OrderId}", order.Id);
                    logger.LogInformation("Counted {OrderTotal}", order.Total);
                }
            }
            class Order { public int Id { get; set; } public int Total { get; set; } }
            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            codeActionIndex: 1);
    }

    [Fact]
    public Task Rename_positional_holes_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId, string name)
                {
                    Log.Logger.Information("{|AASL0008:{0}|} {|AASL0008:{1}|}", orderId, name);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId, string name)
                {
                    Log.Logger.Information("{OrderId} {Name}", orderId, name);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_microsoft_extensions_logging_positional_holes_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string orderId, string name)
                {
                    logger.LogInformation("{|AASL0008:{0}|} {|AASL0008:{1}|}", orderId, name);
                }
            }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, string orderId, string name)
                {
                    logger.LogInformation("{OrderId} {Name}", orderId, name);
                }
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_positional_holes_qualified_names_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", order.Id);
                    Log.Logger.Information("{|AASL0008:{0}|}", order.Total);
                }
            }
            public sealed class Order { public int Id { get; set; } public int Total { get; set; } }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order)
                {
                    Log.Logger.Information("{OrderId}", order.Id);
                    Log.Logger.Information("{OrderTotal}", order.Total);
                }
            }
            public sealed class Order { public int Id { get; set; } public int Total { get; set; } }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider),
            codeActionIndex: 1);
    }

    [Fact]
    public Task Rename_duplicate_derived_positional_names()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order, User user)
                {
                    Log.Logger.Information("{|AASL0008:{0}|} {|AASL0008:{1}|}", order.Id, user.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            public sealed class User { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order, User user)
                {
                    Log.Logger.Information("{Id} {Id2}", order.Id, user.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            public sealed class User { public int Id { get; set; } }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_logger_message_positional_holes_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{|AASL0008:{0}|} {|AASL0008:{1}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId, string name);
            }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "{OrderId} {Name}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId, string name);
            }
            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Destructure_context_objects_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    {|AASL0003:LogContext.PushProperty("First", new Random())|};
                    {|AASL0003:LogContext.PushProperty("Second", new Random())|};
                }
            }
            """,
            /*lang=csharp*/ """
            using System;
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    LogContext.PushProperty("First", new Random(), destructureObjects: true);
                    LogContext.PushProperty("Second", new Random(), destructureObjects: true);
                }
            }
            """,
            "AASL0003",
            typeof(AddContextDestructuringCodeFixProvider));
    }

    [Fact]
    public Task Replace_contextual_logger_types_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                public A({|AASL0004:ILogger<B>|} log) { }
            }
            class C
            {
                public C({|AASL0004:ILogger<B>|} log) { }
            }
            class B { }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                public A(ILogger<A> log) { }
            }
            class C
            {
                public C(ILogger<C> log) { }
            }
            class B { }
            """,
            "AASL0004",
            typeof(ReplaceContextualLoggerTypeCodeFixProvider));
    }

    [Fact]
    public Task Move_exception_arguments_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{One} {Exc}", 1, {|AASL0005:new Exception()|});
                    Log.Logger.Error("{Error}", {|AASL0005:new Exception()|});
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
                    Log.Logger.Information(new Exception(), "{One}", 1);
                    Log.Logger.Error(new Exception(), "");
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task Uniquify_duplicate_properties_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId, string name)
                {
                    Log.Logger.Information("{|AASL0006:{Count}|} {|AASL0006:{Count}|}", orderId, name);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId, string name)
                {
                    Log.Logger.Information("{OrderId} {Name}", orderId, name);
                }
            }
            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Uniquify_duplicate_properties_qualified_names_document()
    {
        return AnalyzerTestHost.VerifyFixAllAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order, User user)
                {
                    Log.Logger.Information("{|AASL0006:{Count}|} {|AASL0006:{Count}|}", order.Id, user.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            public sealed class User { public int Id { get; set; } }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(Order order, User user)
                {
                    Log.Logger.Information("{OrderId} {UserId}", order.Id, user.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            public sealed class User { public int Id { get; set; } }
            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            codeActionIndex: 1);
    }
}
