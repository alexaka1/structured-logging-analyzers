// Copyright (c) 2026 alexaka1

using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;
using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Parity;

public sealed class ComplexObjectAnalyzerTests
{
    [Fact]
    public Task Class_without_tostring()
    {
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
        return AnalyzerTestHost.VerifyAsync(/*lang=csharp*/ """
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
}
