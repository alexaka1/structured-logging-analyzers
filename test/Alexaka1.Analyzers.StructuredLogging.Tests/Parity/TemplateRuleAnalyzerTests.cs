using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class TemplateRuleAnalyzerTests
{
    [Fact]
    public Task Empty_format_holes_keep_argument_alignment()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             sealed class Order { }
                                                             static class C
                                                             {
                                                                 static void M(Order order, int count)
                                                                 {
                                                                     Log.Information("{|AASL0002:{A:}|} {B}", order, count);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Empty_format_holes_apply_duplicate_and_naming_rules()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            static class C
                            {
                                static void M()
                                {
                                    Log.Information("{|AASL0006:{userId:}|} {|AASL0006:{userId:}|}", 1, 2);
                                }
                            }
                            """,
            editorConfig: "dotnet_code_quality.AASL.property_naming = camel_case");
    }

    [Fact]
    public Task Empty_format_hole_applies_naming_rule()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             static class C
                                                             {
                                                                 static void M()
                                                                 {
                                                                     Log.Information("{|AASL0009:{userId:}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Candidate_overloads_only_analyze_clearly_string_templates()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Microsoft.Extensions.Logging;
                                                             using Serilog;
                                                             class C
                                                             {
                                                                 void M(Microsoft.Extensions.Logging.ILogger logger, object other)
                                                                 {
                                                                     logger.LogInformation("{A} {B}", undefinedVar, other);
                                                                     logger.LogError(undefinedEx, "{A}", 1);
                                                                     logger.LogError("{A}", 1, undefinedVar);
                                                                     logger.LogInformation(new EventId(1), "{A}", undefinedVar);
                                                                     Log.Error(undefinedEx, "{A}", 1);
                                                                     logger.LogInformation("{|AASL0009:{a}|}", undefinedVar);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Escaped_constant_interpolated_text_reports_trailing_period()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information($"line\n done{|AASL0011:.|}");
                                }
                            }
                            """);
    }

    [Fact]
    public Task Duplicate_named_properties()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
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
    public Task Const_identifier_tail_reports_sentence_period()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             class C
                                                             {
                                                                 const string Dot = ".";
                                                                 const string TailP = "x.";

                                                                 void M(object a)
                                                                 {
                                                                     Log.Information("{A}" + {|AASL0011:Dot|}, a);
                                                                     Log.Information("{A}" + {|AASL0011:TailP|}, a);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Ellipsis_is_not_a_sentence()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
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
