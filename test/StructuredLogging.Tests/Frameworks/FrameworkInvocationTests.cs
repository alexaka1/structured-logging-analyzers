// Copyright (c) 2026 alexaka1

using StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace StructuredLogging.Tests.Frameworks;

public sealed class FrameworkInvocationTests
{
    [Fact]
    public Task Microsoft_extensions_logging()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            class C
            {
                void M(ILogger logger)
                {
                    logger.LogInformation("{|SLA0009:{myProperty}|}", 1);
                }
            }
            """);
    }

    [Fact]
    public Task NLog()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using NLog;
            class C
            {
                void M(Logger logger)
                {
                    logger.Info("{|SLA0009:{myProperty}|}", 1);
                }
            }
            """);
    }

    [Fact]
    public Task ZLogger()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Microsoft.Extensions.Logging;
            using ZLogger;
            class A
            {
                public A(ILogger<A> log)
                {
                    log.ZLogInformation("{|SLA0009:{myProperty}|}", 1);
                }
            }
            """);
    }

    [Fact]
    public Task Custom_attributed_method()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using System;
            class C
            {
                [MessageTemplateFormatMethod("template")]
                static void Write(string template, params object[] args) { }

                static void M()
                {
                    Write("{|SLA0009:{myProperty}|}", 1);
                }
            }

            [AttributeUsage(AttributeTargets.Method)]
            sealed class MessageTemplateFormatMethodAttribute : Attribute
            {
                public MessageTemplateFormatMethodAttribute(string name) { }
            }
            """);
    }

    [Fact]
    public Task Named_and_reordered_arguments()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using System;
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information(messageTemplate: "{|SLA0002:{MyProperty}|}", new Random());
                    Log.Logger.Information(propertyValues: new object[] { new Random() }, messageTemplate: "{|SLA0002:{MyProperty}|}");
                }
            }
            """);
    }

    [Fact]
    public Task Unrelated_invocation_is_ignored()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using System;
            public static class Program
            {
                public static void Main()
                {
                    Console.WriteLine("{myProperty}", 1);
                    string.Format("{0}", 1);
                }
            }
            """);
    }
}
