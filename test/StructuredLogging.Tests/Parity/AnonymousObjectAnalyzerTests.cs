// Copyright (c) 2026 alexaka1

using StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace StructuredLogging.Tests.Parity;

public sealed class AnonymousObjectAnalyzerTests
{
    [Fact]
    public Task Without_destructure()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|SLA0001:{MyProperty}|}", new { Test = 1 });
                }
            }
            """);
    }

    [Fact]
    public Task Nested_anonymous_still_warns_on_template()
    {
        return AnalyzerTestHost.VerifyAsync("""
            using Serilog;
            using System;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|SLA0001:{MyProperty}|}", new { Test = 1, Complex = new Random() });
                }
            }
            """);
    }
}
