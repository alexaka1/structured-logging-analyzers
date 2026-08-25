// Copyright (c) 2026 alexaka1

using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class TemplateRuleAnalyzerTests
{
    [Fact]
    public Task Duplicate_named_properties()
    {
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0006:{Test}|} {|AASL0006:{Test}|}", 1, 2);
                }
            }
            """);
    }

    [Fact]
    public Task Positional_property()
    {
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{|AASL0008:{0}|}", 1);
                }
            }
            """);
    }

    [Fact]
    public Task Mixed_template_does_not_report_positional()
    {
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("{0} {Name}", 1, "n");
                }
            }
            """);
    }

    [Fact]
    public Task Sentence_period()
    {
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Hello {Name}{|AASL0011:.|}", "World");
                }
            }
            """);
    }

    [Fact]
    public Task Ellipsis_is_not_a_sentence()
    {
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information("Loading {Name}...", "World");
                }
            }
            """);
    }

    [Fact]
    public Task Invalid_syntax_does_not_throw()
    {
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
            using Serilog;
            public static class Program
            {
                public static void Main()
                {
                    Log.Logger.Information(%"{MyProperty}", 1);
                }
            }
            """);
    }
}
