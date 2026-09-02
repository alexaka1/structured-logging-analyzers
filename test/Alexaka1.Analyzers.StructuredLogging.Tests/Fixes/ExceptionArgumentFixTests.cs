using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class ExceptionArgumentFixTests
{
    [Fact]
    public Task AASL0005_moves_exception_and_removes_hole()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{One} {Exc}", 1, {|AASL0005:new Exception()|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information(new Exception(), "{One}", 1);
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_only_exception_argument()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Error("{Error}", {|AASL0005:new Exception()|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Error(new Exception(), "");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_microsoft_extensions_logging()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError("Failed {Op} {Error}", "save", {|AASL0005:ex|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(ex, "Failed {Op}", "save");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_microsoft_extensions_logging_two_argument_call()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError("Failed {Error}", {|AASL0005:ex|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(ex, "Failed");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_event_id()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(42, "Failed {Error}", {|AASL0005:ex|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(42, ex, "Failed");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_argument_comments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError("Failed {Op} {Error}", "save", /* keep */ {|AASL0005:ex|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(/* keep */ ex, "Failed {Op}", "save");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_trailing_argument_comments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError("Failed {Op} {Error}", "save", {|AASL0005:ex|} /* keep */);
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(ex /* keep */, "Failed {Op}", "save");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_leading_single_line_comments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError("Failed {Op} {Error}", "save", // keep
                                        {|AASL0005:ex|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(// keep
                                        ex, "Failed {Op}", "save");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_trailing_single_line_comments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError("Failed {Op} {Error}", "save", {|AASL0005:ex|} // keep
                                    );
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(// keep
                            ex, "Failed {Op}", "save"        );
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_preserves_unaffected_separator_comments()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError("Failed {Op} {Error}",/* keep-save */ "save", {|AASL0005:ex|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex)
                                {
                                    logger.LogError(ex, "Failed {Op}",/* keep-save */ "save");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_interpolated_template_moves_exception_only()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information({|AASL0007:$"{DateTime.Now} {{Error}}"|}, {|AASL0005:new Exception()|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information(new Exception(), $"{DateTime.Now} {{Error}}");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_nlog()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using NLog;
                            class C
                            {
                                void M(Logger logger, Exception ex)
                                {
                                    logger.Error("Failed {Op} {Error}", "save", {|AASL0005:ex|});
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using System;
                            using NLog;
                            class C
                            {
                                void M(Logger logger, Exception ex)
                                {
                                    logger.Error(ex, "Failed {Op}", "save");
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_occupied_exception_slot_has_no_fix()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    var ex = new Exception();
                                    var otherEx = new Exception();
                                    Log.Logger.Information(ex, "{One} {OtherException}", 1, {|AASL0005:otherEx|});
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }

    [Fact]
    public Task AASL0005_occupied_microsoft_exception_slot_has_no_fix()
    {
        return AnalyzerTestHost.VerifyNoFixAsync(
            /*lang=csharp*/ """
                            using System;
                            using Microsoft.Extensions.Logging;
                            class C
                            {
                                void M(ILogger logger, Exception ex, Exception otherEx)
                                {
                                    logger.LogError(ex, "Failed {Op} {Error}", "save", {|AASL0005:otherEx|});
                                }
                            }
                            """,
            "AASL0005",
            typeof(MoveExceptionArgumentCodeFixProvider));
    }
}
