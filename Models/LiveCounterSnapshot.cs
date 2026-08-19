namespace LiveCounter.Models;

public record LiveCounterSnapshot(long TotalVisits, int ActiveSessions, DateTimeOffset UpdatedAt);
