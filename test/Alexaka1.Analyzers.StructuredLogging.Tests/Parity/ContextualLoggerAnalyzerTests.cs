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
}
