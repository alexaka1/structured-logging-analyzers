using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Frameworks;

public sealed class BeginScopeTests
{
    [Fact]
    public Task Named_template_property_is_analyzed()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             class C
                                                             {
                                                                 void M(ILogger logger)
                                                                 {
                                                                     using var scope = logger.BeginScope("{|AASL0009:{myProperty}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Positional_template_property_is_analyzed()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             class C
                                                             {
                                                                 void M(ILogger logger)
                                                                 {
                                                                     using var scope = logger.BeginScope("{|AASL0008:{0}|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Sentence_template_is_analyzed()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             class C
                                                             {
                                                                 void M(ILogger logger)
                                                                 {
                                                                     using var scope = logger.BeginScope("Scope{|AASL0011:.|}", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Interpolated_scope_reports_non_constant_template()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Microsoft.Extensions.Logging;
                                                             class C
                                                             {
                                                                 void M(ILogger logger, string name)
                                                                 {
                                                                     using var scope = logger.BeginScope({|AASL0007:$"Scope {name}"|}, name);
                                                                 }
                                                             }
                                                             """);
    }
}
