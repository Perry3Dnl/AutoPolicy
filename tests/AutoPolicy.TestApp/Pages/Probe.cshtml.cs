using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoPolicy.TestApp.Pages;

public sealed class ProbeModel : PageModel
{
    public void OnGet()
    {
        DiscoveryExecutionProbe.Executed();
    }
}

public static class DiscoveryExecutionProbe
{
    private static int _count;

    public static int Count => Volatile.Read(ref _count);

    public static void Reset() => Interlocked.Exchange(ref _count, 0);

    internal static void Executed() => Interlocked.Increment(ref _count);
}
