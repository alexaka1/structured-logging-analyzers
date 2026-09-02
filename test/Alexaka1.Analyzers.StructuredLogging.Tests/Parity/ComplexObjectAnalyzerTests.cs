using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Alexaka1.Analyzers.StructuredLogging.Recognition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class ComplexObjectAnalyzerTests
{
    [Fact]
    public Task Class_without_tostring()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{|AASL0002:{MyProperty}|}", new Random());
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Stringify_suppresses()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{$MyProperty}", new Random());
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Numeric_stringify()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{$MyProperty}", 3);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Non_generic_enumerable_does_not_warn()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System.Collections;
                                                             using System.Collections.Generic;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     IEnumerable list = new List<string> { "test" };
                                                                     Log.Logger.Information("{MyProperty}", list);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Nullable_numeric_stringify()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     int? a = 1;
                                                                     Log.Logger.Information("{$MyProperty}", a);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Dictionary_stringify()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System.Collections.Generic;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{$MyProperty}", new Dictionary<int, string>());
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Inherited_tostring_does_not_warn()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Information("{MyProperty}", new B());
                                                                 }
                                                             }
                                                             public class A
                                                             {
                                                                 public override string ToString() => "Custom ToString";
                                                             }
                                                             public class B : A { }
                                                             """);
    }

    [Fact]
    public Task Exception_before_template_still_destructures_argument()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     Log.Logger.Error(new MyException(), "{|AASL0002:{MyProperty}|}", new Random());
                                                                 }
                                                             }
                                                             public class MyException : Exception { }
                                                             """);
    }

    [Fact]
    public Task Context_without_destructure()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog.Context;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     {|AASL0003:LogContext.PushProperty("Test", new Random())|};
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Context_numeric_does_not_warn()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using Serilog.Context;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     LogContext.PushProperty("Test", 1);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public Task Context_explicit_destructure_does_not_warn()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             using Serilog.Context;
                                                             public static class Program
                                                             {
                                                                 public static void Main()
                                                                 {
                                                                     LogContext.PushProperty("Test", new Random(), true);
                                                                 }
                                                             }
                                                             """);
    }

    [Fact]
    public void Expanded_params_array_element_maps_to_the_whole_expression()
    {
        var source = /*lang=csharp*/ """
                                     using System;
                                     class C
                                     {
                                         static void Write(string template, params object[] args) { }

                                         static void M()
                                         {
                                             Write("{Numbers}", new[] { 1, 2 });
                                         }
                                     }
                                     """;
        var (compilation, tree, _) = AnalyzerTestHost.CreateCompilation(
            source,
            editorConfig: null,
            languageVersion: LanguageVersion.Latest);
        var cancellationToken = TestContext.Current.CancellationToken;
        var model = compilation.GetSemanticModel(tree);
        var invocation = tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single();
        var method = model.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        Assert.NotNull(method);

        var arguments = TemplateArgumentResolver.MapArguments(model, invocation, method, cancellationToken);
        var template = arguments.Single(a => a.Argument == invocation.ArgumentList.Arguments[0]);
        var mapped = PropertyArgumentMapper.ArgumentForHole(arguments, template, 0);

        Assert.Equal("new[] { 1, 2 }", mapped?.ToString());
    }

    [Fact]
    public Task Fallback_params_mapping_keeps_all_expanded_arguments()
    {
        return AnalyzerTestHost.VerifyAsync( /*lang=csharp*/ """
                                                             using System;
                                                             class Complex { }
                                                             class C
                                                             {
                                                                 [MessageTemplateFormatMethod("template")]
                                                                 static void Write(string template, params object[] args) { }

                                                                 static void M(Complex first, Complex second)
                                                                 {
                                                                     Write("{|AASL0002:{First}|} {|AASL0002:{Second}|}", first, second, invalid: 1);
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
    public void Fallback_params_mapping_is_exercised_for_an_invalid_invocation_operation()
    {
        var source = /*lang=csharp*/ """
                                     using System;
                                     class Complex { }
                                     class C
                                     {
                                         static void Write(string template, params object[] args) { }

                                         static void M(Complex first, Complex second)
                                         {
                                             Write("{First} {Second}", first, second, invalid: 1);
                                         }
                                     }
                                     """;
        var (compilation, tree, _) = AnalyzerTestHost.CreateCompilation(
            source,
            editorConfig: null,
            languageVersion: LanguageVersion.Latest);
        var cancellationToken = TestContext.Current.CancellationToken;
        var model = compilation.GetSemanticModel(tree);
        var invocation = tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single();
        var operation = model.GetOperation(invocation, cancellationToken);

        Assert.NotNull(operation);
        Assert.True(operation is IInvalidOperation);
        Assert.False(operation is IInvocationOperation);

        var method = LoggingInvocationClassifier.ResolveMethod(model, invocation, cancellationToken);
        Assert.NotNull(method);
        var arguments = TemplateArgumentResolver.MapArguments(model, invocation, method, cancellationToken);
        var template = arguments.Single(a => a.Argument == invocation.ArgumentList.Arguments[0]);

        Assert.Equal("first", PropertyArgumentMapper.ArgumentForHole(arguments, template, 0)?.ToString());
        Assert.Equal("second", PropertyArgumentMapper.ArgumentForHole(arguments, template, 1)?.ToString());
    }
}
