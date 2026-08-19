using System.Collections.Concurrent;

namespace LiveCounter.Services;

public class LivePresence
{
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromSeconds(45);
    private readonly ConcurrentDictionary<string, DateTimeOffset> sessions = new();

    public bool Touch(string sessionId)
    {
        var isNew = sessions.TryAdd(sessionId, DateTimeOffset.UtcNow);
        sessions[sessionId] = DateTimeOffset.UtcNow;
        RemoveExpired();
        return isNew;
    }

    public int Count
    {
        get
        {
            RemoveExpired();
            return sessions.Count;
        }
    }

    private void RemoveExpired()
    {
        var cutoff = DateTimeOffset.UtcNow - ActiveWindow;
        foreach (var session in sessions)
        {
            if (session.Value < cutoff)
            {
                sessions.TryRemove(session.Key, out _);
            }
        }
    }
}
