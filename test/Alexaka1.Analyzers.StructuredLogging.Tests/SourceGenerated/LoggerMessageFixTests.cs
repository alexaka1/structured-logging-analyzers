using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.SourceGenerated;

public sealed class LoggerMessageFixTests
{
    [Fact]
    public Task Rename_in_regular_attribute_string()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0009:{orderId}|}")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            "AASL0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_in_verbatim_attribute_string()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = @"Processing {|AASL0009:{orderId}|}")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = @"Processing {OrderId}")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            "AASL0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_in_raw_attribute_string()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """"
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = """Processing {|AASL0009:{orderId}|}""")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """",
            /*lang=csharp*/ """"
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = """Processing {OrderId}""")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """",
            "AASL0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Remove_trailing_period_in_attribute()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}{|AASL0011:.|}")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            "AASL0011",
            typeof(RemoveTrailingPeriodCodeFixProvider));
    }

    [Fact]
    public Task Rename_positional_placeholder()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|AASL0008:{0}|}")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}")]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            "AASL0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Exclusive_const_is_rewritten()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                const string Msg = "Processing {|AASL0009:{orderId}|}.";

                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = Msg)]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            /*lang=csharp*/ """
                            using Microsoft.Extensions.Logging;
                            public static partial class Log
                            {
                                const string Msg = "Processing {OrderId}.";

                                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = Msg)]
                                public static partial void ProcessingOrder(ILogger logger, int orderId);
                            }
                            """,
            "AASL0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }
}
