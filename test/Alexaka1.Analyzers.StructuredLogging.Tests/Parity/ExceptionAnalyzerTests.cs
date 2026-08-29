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
    public Task Exception_before_template_suppresses_later_exception()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information(new Exception(), "{One} {OtherException}", 1, new Exception());
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
