using Orleans.Hosting;

namespace SampleOrleans.Infrastructure;

public static class DependencyInjection
{
    public static ISiloBuilder AddOrderGrains(this ISiloBuilder silo)
    {
        silo.AddMemoryGrainStorage(StorageNames.Orders);
        silo.UseInMemoryReminderService();

        return silo;
    }
}
