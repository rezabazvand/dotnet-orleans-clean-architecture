namespace SampleOrleans.Domain.Orders;

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}
