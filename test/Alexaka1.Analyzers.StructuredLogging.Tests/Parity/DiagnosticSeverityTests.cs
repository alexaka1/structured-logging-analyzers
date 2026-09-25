using Microsoft.CodeAnalysis;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class DiagnosticSeverityTests
{
    [Theory]
    [InlineData("AASL0001", DiagnosticSeverity.Warning)]
    [InlineData("AASL0002", DiagnosticSeverity.Info)]
    [InlineData("AASL0003", DiagnosticSeverity.Info)]
    [InlineData("AASL0004", DiagnosticSeverity.Warning)]
    [InlineData("AASL0005", DiagnosticSeverity.Warning)]
    [InlineData("AASL0006", DiagnosticSeverity.Warning)]
    [InlineData("AASL0007", DiagnosticSeverity.Warning)]
    [InlineData("AASL0008", DiagnosticSeverity.Info)]
    [InlineData("AASL0009", DiagnosticSeverity.Info)]
    [InlineData("AASL0010", DiagnosticSeverity.Info)]
    [InlineData("AASL0011", DiagnosticSeverity.Info)]
    [InlineData("AASL0012", DiagnosticSeverity.Info)]
    public void RulesHaveExpectedDefaults(string id, DiagnosticSeverity severity)
    {
        var descriptor = Assert.Single(Descriptors.All, descriptor => descriptor.Id == id);

        Assert.True(descriptor.IsEnabledByDefault);
        Assert.Equal(severity, descriptor.DefaultSeverity);
    }
}
