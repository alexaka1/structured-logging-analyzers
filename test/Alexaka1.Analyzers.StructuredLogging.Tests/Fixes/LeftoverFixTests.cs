using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class LeftoverFixTests
{
    [Fact]
    public Task AASL0003_adds_destructureObjects()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    {|AASL0003:LogContext.PushProperty("Test", new Random())|};
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
                    LogContext.PushProperty("Test", new Random(), destructureObjects: true);
                }
            }
            """,
            "AASL0003",
            typeof(AddContextDestructuringCodeFixProvider));
    }

    [Fact]
    public Task AASL0003_named_arguments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog.Context;
            public static class Program
            {
                public static void Main()
                {
                    {|AASL0003:LogContext.PushProperty(name: "Test", value: new Random())|};
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
                    LogContext.PushProperty(name: "Test", value: new Random(), destructureObjects: true);
                }
            }
            """,
            "AASL0003",
            typeof(AddContextDestructuringCodeFixProvider));
    }

    [Fact]
    public Task AASL0004_replaces_constructor_logger_category()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                public A({|AASL0004:ILogger<B>|} log) { }
            }
            class B { }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                public A(ILogger<A> log) { }
            }
            class B { }
            """,
            "AASL0004",
            typeof(ReplaceContextualLoggerTypeCodeFixProvider));
    }

    [Fact]
    public Task AASL0004_updates_matching_field()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                ILogger<B> _log;
                public A({|AASL0004:ILogger<B>|} log)
                {
                    _log = log;
                }
            }
            class B { }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                ILogger<A> _log;
                public A(ILogger<A> log)
                {
                    _log = log;
                }
            }
            class B { }
            """,
            "AASL0004",
            typeof(ReplaceContextualLoggerTypeCodeFixProvider));
    }

    [Fact]
    public Task AASL0004_primary_constructor()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A({|AASL0004:ILogger<B>|} log)
            {
                private readonly ILogger<B> _log = log;
            }
            class B { }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A(ILogger<A> log)
            {
                private readonly ILogger<A> _log = log;
            }
            class B { }
            """,
            "AASL0004",
            typeof(ReplaceContextualLoggerTypeCodeFixProvider));
    }

    [Fact]
    public Task AASL0004_uses_containing_type_across_namespaces()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            namespace X { class A { } }
            namespace Y
            {
                class A
                {
                    public A({|AASL0004:ILogger<X.A>|} log) { }
                }
            }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            namespace X { class A { } }
            namespace Y
            {
                class A
                {
                    public A(ILogger<A> log) { }
                }
            }
            """,
            "AASL0004",
            typeof(ReplaceContextualLoggerTypeCodeFixProvider));
    }

    [Fact]
    public Task AASL0004_replaces_ForContext_type_argument()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            class A
            {
                void M(ILogger log)
                {
                    {|AASL0004:log.ForContext<B>()|};
                }
            }
            class B { }
            """,
            /*lang=csharp*/ """
            using Serilog;
            class A
            {
                void M(ILogger log)
                {
                    log.ForContext<A>();
                }
            }
            class B { }
            """,
            "AASL0004",
            typeof(ReplaceContextualLoggerTypeCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_moves_exception_and_removes_hole()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{One} {Exc}", 1, {|AASL0005:new Exception()|});
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
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_only_exception_argument()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
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
                    Log.Logger.Error(new Exception(), "");
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_microsoft_extensions_logging()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex)
                {
                    logger.LogError("Failed {Op} {Error}", "save", {|AASL0005:ex|});
                }
            }
            """,
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex)
                {
                    logger.LogError(ex, "Failed {Op}", "save");
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_event_id()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex)
                {
                    logger.LogError(42, "Failed {Error}", {|AASL0005:ex|});
                }
            }
            """,
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex)
                {
                    logger.LogError(42, ex, "Failed");
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_argument_comments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex)
                {
                    logger.LogError("Failed {Op} {Error}", "save", /* keep */ {|AASL0005:ex|});
                }
            }
            """,
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex)
                {
                    logger.LogError(/* keep */ ex, "Failed {Op}", "save");
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_trailing_argument_comments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex)
                {
                    logger.LogError("Failed {Op} {Error}", "save", {|AASL0005:ex|} /* keep */);
                }
            }
            """,
            /*lang=csharp*/ """
            using System;
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger, Exception ex)
                {
                    logger.LogError(ex /* keep */, "Failed {Op}", "save");
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_interpolated_template_moves_exception_only()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information({|AASL0007:$"{DateTime.Now} {{Error}}"|}, {|AASL0005:new Exception()|});
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
                    Log.Logger.Information(new Exception(), $"{DateTime.Now} {{Error}}");
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0006_uniquifies_second_duplicate()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{Test} {|AASL0006:{Test}|}", 1, 2);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{Test} {Test2}", 1, 2);
                }
            }
            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 0);
    }

    [Fact]
    public Task AASL0006_renames_from_argument_identifiers()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId, string name)
                {
                    Log.Logger.Information("{|AASL0006:{Id}|} {|AASL0006:{Id}|}", orderId, name);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main(string orderId, string name)
                {
                    Log.Logger.Information("{OrderId} {Id}", orderId, name);
                }
            }
            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 0);
    }

    [Fact]
    public Task AASL0006_qualified_argument_names()
    {
        return AnalyzerTestHost.VerifyFixAsync(
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
                    Log.Logger.Information("{OrderId} {Count}", order.Id, user.Id);
                }
            }
            public sealed class Order { public int Id { get; set; } }
            public sealed class User { public int Id { get; set; } }
            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            codeActionIndex: 1,
            expectedActionCount: 2,
            remainingCount: 0);
    }

    [Fact]
    public Task AASL0006_preserves_destructuring_operator()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{@Test} {|AASL0006:{@Test}|}", 1, 2);
                }
            }
            """,
            /*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{@Test} {@Test2}", 1, 2);
                }
            }
            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 0);
    }

    [Fact]
    public Task AASL0005_nlog()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using System;
            using NLog;
            class C
            {
                void M(Logger logger, Exception ex)
                {
                    logger.Error("Failed {Op} {Error}", "save", {|AASL0005:ex|});
                }
            }
            """,
            /*lang=csharp*/ """
            using System;
            using NLog;
            class C
            {
                void M(Logger logger, Exception ex)
                {
                    logger.Error(ex, "Failed {Op}", "save");
                }
            }
            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0004_does_not_rewrite_nested_type()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                public A({|AASL0004:ILogger<B>|} log) { }
                class Nested
                {
                    public Nested(ILogger<B> log) { }
                }
            }
            class B { }
            """,
            /*lang=csharp*/ """
            using Microsoft.Extensions.Logging;
            class A
            {
                public A(ILogger<A> log) { }
                class Nested
                {
                    public Nested(ILogger<B> log) { }
                }
            }
            class B { }
            """,
            "AASL0004",
            typeof(ReplaceContextualLoggerTypeCodeFixProvider),
            remainingCount: 1);
    }
}
