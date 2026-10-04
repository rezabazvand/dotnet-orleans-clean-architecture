namespace SampleOrleans.Application.Orders;

public interface IOrderGrain : IGrainWithGuidKey
{
    Task CreateAsync(TimeSpan confirmationTimeout);

    Task AddLineAsync(string sku, int quantity, decimal unitPrice);

    Task ConfirmAsync();

    Task<OrderSummary> GetSummaryAsync();
}
