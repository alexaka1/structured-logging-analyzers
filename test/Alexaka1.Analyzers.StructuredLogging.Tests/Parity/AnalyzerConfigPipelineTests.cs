using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class AnalyzerConfigPipelineTests
{
    [Fact]
    public async Task Editorconfig_sections_scope_naming_per_source_tree()
    {
        const string editorConfig = /*lang=editorconfig*/ """
                                                          root = true

                                                          [*.cs]
                                                          dotnet_code_quality.AASL.property_naming = camel_case

                                                          [Other.cs]
                                                          dotnet_code_quality.AASL.property_naming = snake_case
                                                          """;
        const string mainSource = /*lang=csharp*/ """
                                                  using Serilog;
                                                  public static class MainProgram
                                                  {
                                                      public static void Main()
                                                      {
                                                          Log.Logger.Information("{myProperty}", 1);
                                                      }
                                                  }
                                                  """;
        const string otherSource = /*lang=csharp*/ """
                                                   using Serilog;
                                                   public static class OtherProgram
                                                   {
                                                       public static void Write()
                                                       {
                                                           Log.Logger.Information("{|AASL0009:{myProperty}|}", 1);
                                                       }
                                                   }
                                                   """;

        var (source, expected) = Markup.Parse(otherSource);
        var diagnostics = await AnalyzerTestHost.GetWorkspaceDiagnosticsAsync(
            mainSource,
            editorConfig,
            additionalSources: new[] { ("/0/Other.cs", source) });

        Assert.Contains(expected, item => item.Id == "AASL0009");
        Assert.DoesNotContain(
            diagnostics,
            diagnostic => diagnostic.Id == "AASL0009" &&
                          diagnostic.Location.SourceTree?.FilePath.EndsWith("/Test.cs", StringComparison.Ordinal) ==
                          true);
        var otherDiagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "AASL0009" &&
                                                                            diagnostic.Location.SourceTree?.FilePath
                                                                                .EndsWith("/Other.cs",
                                                                                    StringComparison.Ordinal) == true));
        Assert.Equal("Property name 'myProperty' does not match naming rules. Suggested name is 'my_property'.",
            otherDiagnostic.GetMessage());
    }
}
