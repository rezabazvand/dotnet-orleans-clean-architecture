using SampleOrleans.Domain.Orders;

namespace SampleOrleans.Infrastructure.Orders;

[GenerateSerializer]
public sealed class OrderState
{
    [Id(0)]
    public string Status { get; set; } = nameof(OrderStatus.Draft);

    [Id(1)]
    public List<OrderLineState> Lines { get; set; } = [];

    public static OrderState FromDomain(Order order)
    {
        return new OrderState
        {
            Status = order.Status.ToString(),
            Lines = order.Lines
                .Select(line => new OrderLineState
                {
                    Sku = line.Sku,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                })
                .ToList()
        };
    }

    public Order ToDomain(Guid orderId)
    {
        return Order.Restore(
            orderId,
            Enum.Parse<OrderStatus>(Status),
            Lines.Select(line => new OrderLine(line.Sku, line.Quantity, line.UnitPrice)));
    }
}
