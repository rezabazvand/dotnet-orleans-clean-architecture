using SampleOrleans.Domain.Primitives;

namespace SampleOrleans.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public Guid Id { get; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public decimal Total => _lines.Sum(line => line.Total);

    public Order(Guid id)
    {
        Id = id;
        Status = OrderStatus.Draft;
    }

    public static Order Restore(Guid id, OrderStatus status, IEnumerable<OrderLine> lines)
    {
        var order = new Order(id)
        {
            Status = status
        };

        order._lines.AddRange(lines);

        return order;
    }

    public void AddLine(string sku, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException($"Order {Id} is {Status}; lines can only be added to a draft order.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Unit price cannot be negative.");
        }

        _lines.Add(new OrderLine(sku, quantity, unitPrice));
    }

    public void Confirm()
    {
        if (Status == OrderStatus.Confirmed)
        {
            return;
        }

        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException($"Order {Id} is cancelled and cannot be confirmed.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainException($"Order {Id} has no lines and cannot be confirmed.");
        }

        Status = OrderStatus.Confirmed;
    }

    public bool CancelIfNotConfirmed()
    {
        if (Status != OrderStatus.Draft)
        {
            return false;
        }

        Status = OrderStatus.Cancelled;

        return true;
    }
}
