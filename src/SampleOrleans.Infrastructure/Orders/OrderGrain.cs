using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using SampleOrleans.Application.Orders;
using SampleOrleans.Domain.Orders;

namespace SampleOrleans.Infrastructure.Orders;

public sealed class OrderGrain : Grain, IOrderGrain, IRemindable
{
    private const string ConfirmationTimeoutReminder = "confirmation-timeout";

    private static readonly TimeSpan RetryPeriod = TimeSpan.FromMinutes(1);

    private readonly IPersistentState<OrderState> _state;
    private readonly ILogger<OrderGrain> _logger;

    private Order _order = null!;

    public OrderGrain(
        [PersistentState("order", StorageNames.Orders)] IPersistentState<OrderState> state,
        ILogger<OrderGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var orderId = this.GetPrimaryKey();

        _order = _state.RecordExists
            ? _state.State.ToDomain(orderId)
            : new Order(orderId);

        return Task.CompletedTask;
    }

    public async Task CreateAsync(TimeSpan confirmationTimeout)
    {
        if (!_state.RecordExists)
        {
            await SaveAsync();
        }

        if (_order.Status == OrderStatus.Draft)
        {
            await this.RegisterOrUpdateReminder(ConfirmationTimeoutReminder, confirmationTimeout, RetryPeriod);
        }
    }

    public async Task AddLineAsync(string sku, int quantity, decimal unitPrice)
    {
        _order.AddLine(sku, quantity, unitPrice);

        await SaveAsync();
    }

    public async Task ConfirmAsync()
    {
        _order.Confirm();

        await SaveAsync();
        await StopConfirmationReminderAsync();
    }

    public Task<OrderSummary> GetSummaryAsync()
    {
        var summary = new OrderSummary
        {
            OrderId = _order.Id,
            Status = _order.Status.ToString(),
            LineCount = _order.Lines.Count,
            Total = _order.Total
        };

        return Task.FromResult(summary);
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName != ConfirmationTimeoutReminder)
        {
            return;
        }

        if (_order.CancelIfNotConfirmed())
        {
            await SaveAsync();

            _logger.LogWarning("Order {OrderId} was not confirmed in time and has been cancelled", _order.Id);
        }

        await StopConfirmationReminderAsync();
    }

    private async Task StopConfirmationReminderAsync()
    {
        var reminder = await this.GetReminder(ConfirmationTimeoutReminder);

        if (reminder is not null)
        {
            await this.UnregisterReminder(reminder);
        }
    }

    private async Task SaveAsync()
    {
        _state.State = OrderState.FromDomain(_order);

        try
        {
            await _state.WriteStateAsync();
        }
        catch
        {
            DeactivateOnIdle();
            throw;
        }
    }
}
