using LiveCounter.Data;
using Microsoft.EntityFrameworkCore;

namespace LiveCounter.Services;

public class LiveCounterStore(IDbContextFactory<LiveCounterDbContext> contextFactory)
{
    public async Task<long> AddVisitAsync(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        await database.Counters
            .Where(counter => counter.Id == 1)
            .ExecuteUpdateAsync(setters => setters.SetProperty(counter => counter.TotalVisits, counter => counter.TotalVisits + 1), cancellationToken);

        return await ReadAsync(database, cancellationToken);
    }

    public async Task<long> ReadAsync(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await ReadAsync(database, cancellationToken);
    }

    private static Task<long> ReadAsync(LiveCounterDbContext database, CancellationToken cancellationToken) =>
        database.Counters
            .Where(counter => counter.Id == 1)
            .Select(counter => counter.TotalVisits)
            .SingleAsync(cancellationToken);
}
