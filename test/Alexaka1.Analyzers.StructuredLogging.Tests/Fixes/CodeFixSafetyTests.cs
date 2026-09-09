using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class CodeFixSafetyTests
{
    [Fact]
    public Task Rename_context_property_does_not_offer_fix_for_const_name()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Serilog.Context;
                            class C
                            {
                                private const string Name = "user_id";

                                void M(int value)
                                {
                                    LogContext.PushProperty({|AASL0010:Name|}, value);
                                }
                            }
                            """,
            "AASL0010",
            typeof(RenameContextPropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_context_property_does_not_offer_fix_for_concatenated_name()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Serilog.Context;
                            class C
                            {
                                void M(int value)
                                {
                                    LogContext.PushProperty(value: value, name: {|AASL0010:"user" + "_id"|});
                                }
                            }
                            """,
            "AASL0010",
            typeof(RenameContextPropertyCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_does_not_offer_fix_for_logger_message_define()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(string user)
                                {
                                    LoggerMessage.Define(LogLevel.Information, new EventId(1), {|AASL0007:$"User {user.Length}"|});
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_does_not_offer_fix_for_logger_message_define_scope()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(string user)
                                {
                                    LoggerMessage.DefineScope({|AASL0007:$"User {user.Length}"|});
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_does_not_offer_fix_for_wrapper_without_value_parameters()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using System;
                            class C
                            {
                                [MessageTemplateFormatMethod("template")]
                                static void LogIt(string template) { }

                                void M(string user)
                                {
                                    LogIt({|AASL0007:$"User {user.Length}"|});
                                }
                            }

                            [AttributeUsage(AttributeTargets.Method)]
                            sealed class MessageTemplateFormatMethodAttribute : Attribute
                            {
                                public MessageTemplateFormatMethodAttribute(string name) { }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_uses_wrapper_params_overload()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            class C
                            {
                                [MessageTemplateFormatMethod("template")]
                                static void LogInformation(string template) { }

                                [MessageTemplateFormatMethod("template")]
                                static void LogInformation(string template, params object[] values) { }

                                void M(int id)
                                {
                                    LogInformation({|AASL0007:$"User {id}"|});
                                }
                            }

                            [AttributeUsage(AttributeTargets.Method)]
                            sealed class MessageTemplateFormatMethodAttribute : Attribute
                            {
                                public MessageTemplateFormatMethodAttribute(string name) { }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            class C
                            {
                                [MessageTemplateFormatMethod("template")]
                                static void LogInformation(string template) { }

                                [MessageTemplateFormatMethod("template")]
                                static void LogInformation(string template, params object[] values) { }

                                void M(int id)
                                {
                                    LogInformation("User {Id}", id);
                                }
                            }

                            [AttributeUsage(AttributeTargets.Method)]
                            sealed class MessageTemplateFormatMethodAttribute : Attribute
                            {
                                public MessageTemplateFormatMethodAttribute(string name) { }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1,
            assertTemplateArgumentCountMatches: true);
    }

    [Fact]
    public Task Remove_trailing_period_does_not_offer_fix_for_const_field_template()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                private const string Template = "Operation done.";

                                public static void Main()
                                {
                                    Log.Logger.Information({|AASL0011:Template|});
                                }
                            }
                            """,
            "AASL0011",
            typeof(RemoveTrailingPeriodCodeFixProvider));
    }

    [Fact]
    public Task Remove_trailing_period_does_not_offer_fix_for_const_identifier_tail()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            class C
                            {
                                const string Dot = ".";

                                void M(object a)
                                {
                                    Log.Information("{A}" + {|AASL0011:Dot|}, a);
                                }
                            }
                            """,
            "AASL0011",
            typeof(RemoveTrailingPeriodCodeFixProvider));
    }

    [Fact]
    public Task Remove_trailing_period_rewrites_constant_interpolated_text()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information($"Operation done{|AASL0011:.|}");
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information($"Operation done");
                                }
                            }
                            """,
            "AASL0011",
            typeof(RemoveTrailingPeriodCodeFixProvider));
    }

    [Fact]
    public Task Add_destructuring_does_not_offer_fix_for_unmapped_const_template()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                private const string Template = "{Value}";

                                public static void Main()
                                {
                                    Log.Logger.Information({|AASL0002:Template|}, new Random());
                                }
                            }
                            """,
            "AASL0002",
            typeof(AddDestructuringCodeFixProvider));
    }

    [Fact]
    public Task Add_destructuring_does_not_insert_into_unmapped_concatenation()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                private const string Prefix = "prefix";

                                public static void Main()
                                {
                                    Log.Logger.Information({|AASL0002:Prefix + " {Value}"|}, new Random());
                                }
                            }
                            """,
            "AASL0002",
            typeof(AddDestructuringCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_preserves_escaped_braces()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string userId)
                                {
                                    logger.LogInformation({|AASL0007:$"escaped {{Lit}} for {userId}"|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string userId)
                                {
                                    logger.LogInformation("escaped {{Lit}} for {UserId}", userId);
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1,
            assertTemplateArgumentCountMatches: true);
    }

    [Fact]
    public Task Convert_interpolation_ignores_non_logging_invocations_when_matching_arguments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string userId)
                                {
                                    logger.LogInformation({|AASL0007:$"user {userId}"|});
                                    Console.WriteLine("status", userId);
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string userId)
                                {
                                    logger.LogInformation("user {UserId}", userId);
                                    Console.WriteLine("status", userId);
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1,
            assertTemplateArgumentCountMatches: true);
    }

    [Fact]
    public Task Convert_verbatim_interpolation_preserves_escaped_braces()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string userId)
                                {
                                    logger.LogInformation({|AASL0007:$@"escaped {{Lit}} for {userId}"|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string userId)
                                {
                                    logger.LogInformation("escaped {{Lit}} for {UserId}", userId);
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1,
            assertTemplateArgumentCountMatches: true);
    }

    [Fact]
    public Task Convert_single_dollar_raw_interpolation_is_unchanged()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """"
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string userId)
                                {
                                    logger.LogInformation({|AASL0007:$"""user {userId}"""|});
                                }
                            }
                            """",
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string userId)
                                {
                                    logger.LogInformation("user {UserId}", userId);
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1,
            assertTemplateArgumentCountMatches: true);
    }

    [Fact]
    public Task Convert_double_dollar_raw_interpolation_escapes_literal_braces()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """"
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string name)
                                {
                                    logger.LogInformation({|AASL0007:$$"""Set {literal} to {{name}}"""|});
                                }
                            }
                            """",
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string name)
                                {
                                    logger.LogInformation("Set {{literal}} to {Name}", name);
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1,
            assertTemplateArgumentCountMatches: true);
    }

    [Fact]
    public Task Convert_triple_dollar_raw_interpolation_escapes_literal_braces()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """"
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string name)
                                {
                                    logger.LogInformation({|AASL0007:$$$"""Set {literal} and {{literal2}} to {{{name}}}"""|});
                                }
                            }
                            """",
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, string name)
                                {
                                    logger.LogInformation("Set {{literal}} and {{{{literal2}}}} to {Name}", name);
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider),
            expectedActionCount: 1,
            assertTemplateArgumentCountMatches: true);
    }

    [Fact]
    public Task Convert_interpolation_does_not_box_ref_like_value()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            ref struct RefLike { }

                            class C
                            {
                                void M(ILogger logger, RefLike value)
                                {
                                    logger.LogInformation({|AASL0007:$"user {value}"|});
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_does_not_box_pointer_value()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            unsafe class C
                            {
                                void M(ILogger logger, int* value)
                                {
                                    logger.LogInformation({|AASL0007:$"user {value}"|});
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }

    [Fact]
    public Task Convert_interpolation_does_not_box_function_pointer_value()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            unsafe class C
                            {
                                void M(ILogger logger, delegate*<int, int> value)
                                {
                                    logger.LogInformation({|AASL0007:$"user {value}"|});
                                }
                            }
                            """,
            "AASL0007",
            typeof(ConvertInterpolatedTemplateCodeFixProvider));
    }
}
