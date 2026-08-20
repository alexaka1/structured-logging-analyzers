// Fixture: PropertiesNamingAnalyzerDotNet6/ZLoggerInvalidNamedProperty.cs
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Comparison_PropertiesNamingAnalyzerDotNet6_ZLoggerInvalidNamedProperty
{
    class A
    {
        public A(ILogger<A> log)
        {
            log.ZLogInformation("{myProperty}", 1);
        }
    }
}
