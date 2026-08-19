using LiveCounter.Models;
using Microsoft.EntityFrameworkCore;

namespace LiveCounter.Data;

public class LiveCounterDbContext(DbContextOptions<LiveCounterDbContext> options) : DbContext(options)
{
    public DbSet<LiveCounterState> Counters => Set<LiveCounterState>();

    public void EnsureCounterExists()
    {
        if (!Counters.Any(counter => counter.Id == 1))
        {
            Counters.Add(new LiveCounterState { Id = 1 });
            SaveChanges();
        }
    }
}
