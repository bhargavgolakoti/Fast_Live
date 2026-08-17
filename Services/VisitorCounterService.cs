using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Services;

public class VisitorCounterService(IDbContextFactory<CrmDbContext> contextFactory)
{
    public async Task<long> RegisterVisitAsync(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        await database.VisitorCounters
            .Where(counter => counter.Id == 1)
            .ExecuteUpdateAsync(setters => setters.SetProperty(counter => counter.Count, counter => counter.Count + 1), cancellationToken);

        return await GetCountAsync(database, cancellationToken);
    }

    public async Task<long> GetCountAsync(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await GetCountAsync(database, cancellationToken);
    }

    private static async Task<long> GetCountAsync(CrmDbContext database, CancellationToken cancellationToken)
    {
        return await database.VisitorCounters
            .Where(counter => counter.Id == 1)
            .Select(counter => counter.Count)
            .SingleAsync(cancellationToken);
    }
}