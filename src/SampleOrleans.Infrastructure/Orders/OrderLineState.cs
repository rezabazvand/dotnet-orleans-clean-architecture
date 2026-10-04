namespace SampleOrleans.Infrastructure.Orders;

[GenerateSerializer]
public sealed class OrderLineState
{
    [Id(0)]
    public string Sku { get; set; } = string.Empty;

    [Id(1)]
    public int Quantity { get; set; }

    [Id(2)]
    public decimal UnitPrice { get; set; }
}
