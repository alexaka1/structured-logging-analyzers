using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class ExceptionAnalyzerTests
{
    [Fact]
    public Task Exception_as_template_argument()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{One} {Exc}", 1, {|AASL0005:new Exception()|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Exception_before_template_is_valid()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information(new Exception(), "{One}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Microsoft_exception_as_first_template_argument()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Microsoft.Extensions.Logging;
                                                             public static class Program
                                                             {
                                                                 public static void Main(ILogger logger, Exception ex)
                                                                 {
                                                                     logger.LogError("Failed {Error}", {|AASL0005:ex|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Microsoft_explicit_static_extension_call_keeps_receiver_argument()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Microsoft.Extensions.Logging;
                                                             public static class Program
                                                             {
                                                                 public static void Main(ILogger logger, Exception ex)
                                                                 {
                                                                     LoggerExtensions.LogError(logger, "Failed {Error}", {|AASL0005:ex|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Exception_before_template_still_reports_later_template_exception()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
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
                                                             """);
    }

    [Fact]
    public Task Exception_before_template_still_reports_later_inline_exception()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information(new Exception(), "{One} {OtherException}", 1, {|AASL0005:new Exception()|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Named_exception_slot_still_reports_later_template_exception()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     var ex = new Exception();
                                                                     var otherEx = new InvalidOperationException();
                                                                     Log.Logger.Information(exception: ex, messageTemplate: "{One} {OtherException}", 1, {|AASL0005:otherEx|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Dynamic_template_still_reports_exception()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information({|AASL0007:$"{DateTime.Now} {{Error}}"|}, {|AASL0005:new Exception()|});
                                                                 }
                                                             }
                                                             """);
    }
}
