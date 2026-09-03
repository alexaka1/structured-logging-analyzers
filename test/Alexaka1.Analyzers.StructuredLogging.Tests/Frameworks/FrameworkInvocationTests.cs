using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Frameworks;

public sealed class FrameworkInvocationTests
{
    [Fact]
    public Task Microsoft_extensions_logging()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             class C
                                                             {
                                                                 void M(ILogger logger)
                                                                 {
                                                                     logger.LogInformation("{|AASL0009:{myProperty}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task NLog()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using NLog;
                                                             class C
                                                             {
                                                                 void M(Logger logger)
                                                                 {
                                                                     logger.Info("{|AASL0009:{myProperty}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task ZLogger()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             using ZLogger;
                                                             class A
                                                             {
                                                                 public A(ILogger<A> log)
                                                                 {
                                                                     log.ZLogInformation("{|AASL0009:{myProperty}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task ZLogger_zero_value_overloads()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Microsoft.Extensions.Logging;
                                                             using ZLogger;
                                                             class A
                                                             {
                                                                 void M(ILogger<A> log, Exception ex, int p)
                                                                 {
                                                                     log.ZLogInformation("Done{|AASL0011:.|}");
                                                                     log.ZLogInformation("{|AASL0008:{0}|}");
                                                                     log.ZLogInformation({|AASL0007:$"{p}"|});
                                                                     log.ZLogError(ex, "Failed{|AASL0011:.|}");
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Custom_attributed_method()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             class C
                                                             {
                                                                 [MessageTemplateFormatMethod("template")]
                                                                 static void Write(string template, params object[] args) { }

                                                                 static void M()
                                                                 {
                                                                     Write("{|AASL0009:{myProperty}|}", 1);
                                                                 }
                                                             }

                                                             [AttributeUsage(AttributeTargets.Method)]
                                                             sealed class MessageTemplateFormatMethodAttribute : Attribute
                                                             {
                                                                 public MessageTemplateFormatMethodAttribute(string name) { }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Custom_attributed_method_without_logging_references()
    {
        return AnalyzerTestHost.VerifyAsync(
            /*lang=csharp*/ """
                            using System;
                            class C
                            {
                                [MessageTemplateFormatMethod("template")]
                                static void Write(string template, params object[] args) { }

                                static void M()
                                {
                                    Write("{|AASL0009:{myProperty}|}", 1);
                                }
                            }

                            [AttributeUsage(AttributeTargets.Method)]
                            sealed class MessageTemplateFormatMethodAttribute : Attribute
                            {
                                public MessageTemplateFormatMethodAttribute(string name) { }
                            }
                            """,
            references: NuGetPackageResolver.GetReferences(),
            requireSuccessfulCompilation: true);
    }

    [Fact]
    public Task Named_and_reordered_arguments()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information(messageTemplate: "{|AASL0002:{MyProperty}|}", new Random());
                                                                     Log.Logger.Information(propertyValues: new object[] { new Random() }, messageTemplate: "{|AASL0002:{MyProperty}|}");
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Microsoft_extensions_logging_does_not_report_destructuring()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Microsoft.Extensions.Logging;
                                                             class C
                                                             {
                                                                 void M(ILogger logger)
                                                                 {
                                                                     logger.LogInformation("{MyProperty}", new Random());
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task ZLogger_does_not_report_destructuring()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Microsoft.Extensions.Logging;
                                                             using ZLogger;
                                                             class A
                                                             {
                                                                 public A(ILogger<A> log)
                                                                 {
                                                                     log.ZLogInformation("{MyProperty}", new Random());
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Microsoft_extensions_logging_reports_later_template_exception()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Microsoft.Extensions.Logging;
                                                             class C
                                                             {
                                                                 void M(ILogger logger, Exception ex, Exception otherEx)
                                                                 {
                                                                     logger.LogInformation(ex, "{One} {OtherException}", 1, {|AASL0005:otherEx|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task NLog_reports_later_template_exception()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using NLog;
                                                             class C
                                                             {
                                                                 void M(Logger logger, Exception ex, Exception otherEx)
                                                                 {
                                                                     logger.Info(ex, "{One} {OtherException}", 1, {|AASL0005:otherEx|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task ZLogger_reports_later_template_exception()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Microsoft.Extensions.Logging;
                                                             using ZLogger;
                                                             class A
                                                             {
                                                                 public A(ILogger<A> log, Exception ex, Exception otherEx)
                                                                 {
                                                                     log.ZLogInformation(ex, "{One} {OtherException}", 1, {|AASL0005:otherEx|});
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Unrelated_invocation_is_ignored()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Console.WriteLine("{myProperty}", 1);
                                                                     string.Format("{0}", 1);
                                                                 }
                                                             }
                                                             """);
    }
}
