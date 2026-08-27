using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Net10BlazorExample.Components.Pages;

public partial class Counter
{
    [Inject]
    private ILogger<Counter> Logger { get; set; } = default!;

    private int count = 1;

    protected override void OnInitialized()
    {
        Logger.LogInformation("Hello {Name}", "world");
        // Intentional AASL0009 and AASL0011: build-time diagnostic coverage for .razor.cs.
        Logger.LogInformation("Clicked {count}.", count);
    }
}
