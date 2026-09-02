using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class CodeFixSafetyTests
{
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
            expectedActionCount: 1);
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
