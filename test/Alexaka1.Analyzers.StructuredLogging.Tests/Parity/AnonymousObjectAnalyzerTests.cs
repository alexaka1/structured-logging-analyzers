using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class AnonymousObjectAnalyzerTests
{
    [Fact]
    public Task Without_destructure()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{|AASL0001:{MyProperty}|}", new { Test = 1 });
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Nested_anonymous_still_warns_on_template()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             using System;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{|AASL0001:{MyProperty}|}", new { Test = 1, Complex = new Random() });
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Anonymous_objects_in_explicit_params_arrays_are_mapped_to_holes()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             class P { }
                                                             class C
                                                             {
                                                                 void M(P p)
                                                                 {
                                                                     Log.Information("{|AASL0001:{A}|} {|AASL0002:{B}|}", new object[] { new { X = 1 }, p });
                                                                     Log.Information("{|AASL0001:{A}|}", propertyValues: new object[] { new { X = 1 } });
                                                                 }
                                                             }
                                                             """);
    }
}
