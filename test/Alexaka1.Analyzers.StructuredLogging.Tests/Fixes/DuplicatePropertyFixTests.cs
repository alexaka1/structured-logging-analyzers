using Alexaka1.Analyzers.StructuredLogging.CodeFixes;
using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests.Fixes;

public sealed class DuplicatePropertyFixTests
{
    [Fact]
    public Task AASL0006_uniquifies_second_duplicate()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{Test} {|AASL0006:{Test}|}", 1, 2);
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{Test} {Test2}", 1, 2);
                                }
                            }
                            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 0);
    }

    [Fact]
    public Task AASL0006_renames_from_argument_identifiers()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main(string orderId, string name)
                                {
                                    Log.Logger.Information("{|AASL0006:{Id}|} {|AASL0006:{Id}|}", orderId, name);
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main(string orderId, string name)
                                {
                                    Log.Logger.Information("{OrderId} {Id}", orderId, name);
                                }
                            }
                            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 0);
    }

    [Fact]
    public Task AASL0006_qualified_argument_names()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main(Order order, User user)
                                {
                                    Log.Logger.Information("{|AASL0006:{Count}|} {|AASL0006:{Count}|}", order.Id, user.Id);
                                }
                            }
                            public sealed class Order { public int Id { get; set; } }
                            public sealed class User { public int Id { get; set; } }
                            """,
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main(Order order, User user)
                                {
                                    Log.Logger.Information("{OrderId} {Count}", order.Id, user.Id);
                                }
                            }
                            public sealed class Order { public int Id { get; set; } }
                            public sealed class User { public int Id { get; set; } }
                            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            codeActionIndex: 1,
            expectedActionCount: 2,
            remainingCount: 0);
    }

    [Fact]
    public Task AASL0006_preserves_destructuring_operator()
    {
        return AnalyzerTestHost.VerifyFixAsync(
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{@Test} {|AASL0006:{@Test}|}", 1, 2);
                                }
                            }
                            """,
            /*lang=csharp*/ """
                            using Serilog;
                            public static class Program
                            {
                                public static void Main()
                                {
                                    Log.Logger.Information("{@Test} {@Test2}", 1, 2);
                                }
                            }
                            """,
            "AASL0006",
            typeof(RenameTemplatePropertyCodeFixProvider),
            remainingCount: 0);
    }
}
