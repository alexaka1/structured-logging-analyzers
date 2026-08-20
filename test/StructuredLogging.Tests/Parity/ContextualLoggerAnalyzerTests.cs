// Copyright (c) 2026 alexaka1

using StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace StructuredLogging.Tests.Parity;

public sealed class ContextualLoggerAnalyzerTests
{
    [Fact]
    public Task Mel_wrong_type()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            class A
            {
                ILogger<B> _log;
                public A({|SLA0004:ILogger<B>|} log)
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
        return AnalyzerTestHost.VerifyAsync("""
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
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            class A
            {
                ILogger<B> _log;
                public A(int a, {|SLA0004:ILogger<B>|} log)
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
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            namespace X { class A { } }
            namespace Y
            {
                class A
                {
                    ILogger<X.A> _log;
                    public A({|SLA0004:ILogger<X.A>|} log)
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
        return AnalyzerTestHost.VerifyAsync("""
            using Serilog;
            class A
            {
                private static readonly ILogger Logger = {|SLA0004:Logger.ForContext<B>()|};
            }
            class B { }
            """);
    }

    [Fact]
    public Task Serilog_correct_context_type()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Serilog;
            class A
            {
                private static readonly ILogger Logger = Logger.ForContext<A>();
            }
            """);
    }

    [Fact]
    public Task Primary_constructor_wrong_type()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            class A({|SLA0004:ILogger<B>|} log)
            {
                private readonly ILogger<B> _log = log;
            }
            class B { }
            """);
    }
}
