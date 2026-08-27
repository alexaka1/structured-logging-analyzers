using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class ContextDestructuringFixTests
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
}
