using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class ContextualLoggerAnalyzerTests
{
    [Fact]
    public Task Mel_wrong_type()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
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
                                                             """);
    }

    [Fact]
    public Task Mel_correct_type()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             class A
                                                             {
                                                                 ILogger<A> _log;
                                                                 public A(ILogger<A> log)
                                                                 {
                                                                     _log = log;
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Mel_wrong_type_multiple_parameters()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             class A
                                                             {
                                                                 ILogger<B> _log;
                                                                 public A(int a, {|AASL0004:ILogger<B>|} log)
                                                                 {
                                                                     _log = log;
                                                                 }
                                                             }
                                                             class B { }
                                                             """);
    }

    [Fact]
    public Task Mel_wrong_type_across_namespaces()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             namespace X { class A { } }
                                                             namespace Y
                                                             {
                                                                 class A
                                                                 {
                                                                     ILogger<X.A> _log;
                                                                     public A({|AASL0004:ILogger<X.A>|} log)
                                                                     {
                                                                         _log = log;
                                                                     }
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Serilog_wrong_context_type()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             class A
                                                             {
                                                                 private static readonly ILogger Logger = {|AASL0004:Logger.ForContext<B>()|};
                                                             }
                                                             class B { }
                                                             """);
    }

    [Fact]
    public Task Serilog_correct_context_type()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             class A
                                                             {
                                                                 private static readonly ILogger Logger = Logger.ForContext<A>();
                                                             }
                                                             """);
    }

    [Fact]
    public Task Serilog_concrete_logger_wrong_context_type()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            using Serilog.Core;
                            public class Other { }
                            public class Orders
                            {
                                private static readonly Logger Root = new LoggerConfiguration().CreateLogger();
                                private static readonly ILogger Log = {|AASL0004:Root.ForContext<Other>()|};
                            }
                            """,
            requireSuccessfulCompilation: true);
    }

    [Fact]
    public Task Serilog_concrete_logger_correct_context_type()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            using Serilog.Core;
                            public class Orders
                            {
                                private static readonly Logger Root = new LoggerConfiguration().CreateLogger();
                                private static readonly ILogger Log = Root.ForContext<Orders>();
                            }
                            """,
            requireSuccessfulCompilation: true);
    }

    [Fact]
    public Task Serilog_logger_implementation_wrong_context_type()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            using Serilog.Events;
                            public class CustomLogger : ILogger
                            {
                                public void Write(LogEvent logEvent) { }
                                public ILogger ForContext<TSource>() => this;
                            }
                            public class Other { }
                            public class Orders
                            {
                                private static readonly CustomLogger Root = new CustomLogger();
                                private static readonly ILogger Log = {|AASL0004:Root.ForContext<Other>()|};
                            }
                            """,
            requireSuccessfulCompilation: true);
    }

    [Fact]
    public Task Unrelated_ForContext_method_is_not_reported()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            public class CustomLogger
                            {
                                public CustomLogger ForContext<TSource>() => this;
                            }
                            public class Other { }
                            public class Orders
                            {
                                private static readonly CustomLogger Log = new CustomLogger().ForContext<Other>();
                            }
                            """,
            requireSuccessfulCompilation: true);
    }

    [Fact]
    public Task Serilog_wrong_context_type_through_conditional_access()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             class A
                                                             {
                                                                 private static readonly ILogger Logger = Logger?{|AASL0004:.ForContext<B>()|};
                                                             }
                                                             class B { }
                                                             """);
    }

    [Fact]
    public Task Serilog_push_property_named_arguments_are_bound_by_parameter()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog.Context;
                                                             class A
                                                             {
                                                                 static void M()
                                                                 {
                                                                     LogContext.PushProperty(
                                                                         value: 1,
                                                                         name: {|AASL0010:"order_id"|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Serilog_push_property_named_value_is_checked_by_parameter()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog.Context;
                                                             class A
                                                             {
                                                                 static void M()
                                                                 {
                                                                     {|AASL0003:LogContext.PushProperty(
                                                                         value: new Random(),
                                                                         name: "UserId")|};
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Primary_constructor_wrong_type()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             class A({|AASL0004:ILogger<B>|} log)
                                                             {
                                                                 private readonly ILogger<B> _log = log;
                                                             }
                                                             class B { }
                                                             """);
    }

    [Fact]
    public Task Unresolved_context_types_do_not_report()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             using Serilog;
                                                             class C
                                                             {
                                                                 public C(ILogger<Unknown> logger) { }

                                                                 void M()
                                                                 {
                                                                     Log.ForContext<Unknown>();
                                                                 }
                                                             }

                                                             class D(ILogger<Unkn> logger);
                                                             class E
                                                             {
                                                                 public E(ILogger<> logger) { }
                                                             }
                                                             """);
    }
}
