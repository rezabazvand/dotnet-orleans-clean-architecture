namespace SampleOrleans.Application.Orders;

[GenerateSerializer]
public sealed record OrderSummary
{
    [Id(0)]
    public Guid OrderId { get; init; }

    [Id(1)]
    public string Status { get; init; } = string.Empty;

    [Id(2)]
    public int LineCount { get; init; }

    [Id(3)]
    public decimal Total { get; init; }
}
