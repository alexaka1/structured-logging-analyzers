// Copyright (c) 2026 alexaka1

using StructuredLogging.CodeFixes;
using StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace StructuredLogging.Tests.SourceGenerated;

public sealed class LoggerMessageFixTests
{
    [Fact]
    public Task Rename_in_regular_attribute_string()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|SLA0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            "SLA0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_in_verbatim_attribute_string()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = @"Processing {|SLA0009:{orderId}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = @"Processing {OrderId}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            "SLA0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Rename_in_raw_attribute_string()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """"
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = """Processing {|SLA0009:{orderId}|}""")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """",
            """"
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = """Processing {OrderId}""")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """",
            "SLA0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Remove_trailing_period_in_attribute()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}{|SLA0011:.|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            "SLA0011",
            typeof(RemoveTrailingPeriodCodeFixProvider));
    }

    [Fact]
    public Task Rename_positional_placeholder()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {|SLA0008:{0}|}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing {OrderId}")]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            "SLA0008",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }

    [Fact]
    public Task Exclusive_const_is_rewritten()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                const string Msg = "Processing {|SLA0009:{orderId}|}.";

                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = Msg)]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            """
            using Microsoft.Extensions.Logging;
            public static partial class Log
            {
                const string Msg = "Processing {OrderId}.";

                [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = Msg)]
                public static partial void ProcessingOrder(ILogger logger, int orderId);
            }
            """,
            "SLA0009",
            typeof(RenameTemplatePropertyCodeFixProvider));
    }
}
