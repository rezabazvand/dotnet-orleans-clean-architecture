using SampleOrleans.Application.Orders;
using SampleOrleans.Domain.Orders;

namespace SampleOrleans.Host;

internal static class Demo
{
    private const int UnprotectedCalls = 100_000;
    private const int GrainCalls = 1_000;
    private const int RacingCalls = 200;

    private static readonly TimeSpan LongTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxWaitForCancellation = TimeSpan.FromSeconds(90);

    public static async Task<bool> RunAsync(IGrainFactory grains)
    {
        var passed = true;

        PlainObjectUnderConcurrency();

        passed &= await GrainUnderConcurrencyAsync(grains);
        passed &= await ConfirmRacingWithAddLineAsync(grains);
        passed &= await ConfirmationTimeoutAsync(grains);

        Console.WriteLine();
        Console.WriteLine(passed ? "All checks passed." : "Some checks FAILED.");

        return passed;
    }

    private static void PlainObjectUnderConcurrency()
    {
        Title("1. A plain Order object, hit from many threads at once");

        var order = new Order(Guid.NewGuid());
        var failedCalls = 0;

        Parallel.For(0, UnprotectedCalls, _ =>
        {
            try
            {
                order.AddLine("SKU-1", 1, 10m);
            }
            catch (Exception)
            {
                Interlocked.Increment(ref failedCalls);
            }
        });

        Console.WriteLine($"   AddLine calls   : {UnprotectedCalls}");
        Console.WriteLine($"   Lines recorded  : {order.Lines.Count}");
        Console.WriteLine($"   Calls that threw: {failedCalls}");
    }

    private static async Task<bool> GrainUnderConcurrencyAsync(IGrainFactory grains)
    {
        Title("2. The same Order behind a grain, hit by concurrent calls");

        var order = grains.GetGrain<IOrderGrain>(Guid.NewGuid());
        await order.CreateAsync(LongTimeout);

        var calls = Enumerable
            .Range(0, GrainCalls)
            .Select(_ => order.AddLineAsync("SKU-1", 1, 10m));

        await Task.WhenAll(calls);

        var summary = await order.GetSummaryAsync();
        await order.ConfirmAsync();

        Console.WriteLine($"   AddLine calls   : {GrainCalls}");
        Console.WriteLine($"   Lines recorded  : {summary.LineCount}");
        Console.WriteLine($"   Order total     : {summary.Total}");

        return Check(summary.LineCount == GrainCalls, "no update was lost, and the code has no lock");
    }

    private static async Task<bool> ConfirmRacingWithAddLineAsync(IGrainFactory grains)
    {
        Title("3. Confirm racing with AddLine on one order");

        var order = grains.GetGrain<IOrderGrain>(Guid.NewGuid());
        await order.CreateAsync(LongTimeout);
        await order.AddLineAsync("SKU-1", 1, 10m);

        var addCalls = Enumerable
            .Range(0, RacingCalls)
            .Select(_ => TryAddLineAsync(order))
            .ToList();

        var confirmCall = order.ConfirmAsync();

        var results = await Task.WhenAll(addCalls);
        await confirmCall;

        var accepted = results.Count(added => added);
        var summary = await order.GetSummaryAsync();

        Console.WriteLine($"   AddLine calls   : {RacingCalls}");
        Console.WriteLine($"   Accepted        : {accepted}");
        Console.WriteLine($"   Rejected        : {RacingCalls - accepted}");
        Console.WriteLine($"   Lines recorded  : {summary.LineCount}");
        Console.WriteLine($"   Order status    : {summary.Status}");

        var passed = Check(summary.Status == "Confirmed", "the order was confirmed");
        passed &= Check(summary.LineCount == accepted + 1, "every accepted line is on the order, every rejected one is not");

        return passed;
    }

    private static async Task<bool> ConfirmationTimeoutAsync(IGrainFactory grains)
    {
        Title($"4. Confirmation timeout with a reminder ({ShortTimeout.TotalSeconds:0}s)");

        var confirmed = await CreateOrderWithOneLineAsync(grains);
        var forgotten = await CreateOrderWithOneLineAsync(grains);

        await confirmed.ConfirmAsync();

        Console.WriteLine("   Order A: confirmed straight away.");
        Console.WriteLine("   Order B: nobody confirms it. Waiting for its reminder...");

        var deadline = DateTime.UtcNow + MaxWaitForCancellation;
        var forgottenStatus = (await forgotten.GetSummaryAsync()).Status;

        while (forgottenStatus == "Draft" && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            forgottenStatus = (await forgotten.GetSummaryAsync()).Status;
        }

        var confirmedStatus = (await confirmed.GetSummaryAsync()).Status;

        Console.WriteLine($"   Order A status  : {confirmedStatus}");
        Console.WriteLine($"   Order B status  : {forgottenStatus}");

        var passed = Check(confirmedStatus == "Confirmed", "the confirmed order was left alone");
        passed &= Check(forgottenStatus == "Cancelled", "the forgotten order cancelled itself");

        return passed;
    }

    private static async Task<IOrderGrain> CreateOrderWithOneLineAsync(IGrainFactory grains)
    {
        var order = grains.GetGrain<IOrderGrain>(Guid.NewGuid());

        await order.CreateAsync(ShortTimeout);
        await order.AddLineAsync("SKU-1", 1, 10m);

        return order;
    }

    private static async Task<bool> TryAddLineAsync(IOrderGrain order)
    {
        try
        {
            await order.AddLineAsync("SKU-1", 1, 10m);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void Title(string text)
    {
        Console.WriteLine();
        Console.WriteLine(text);
    }

    private static bool Check(bool condition, string description)
    {
        Console.WriteLine($"   [{(condition ? "OK" : "FAILED")}] {description}");

        return condition;
    }
}
