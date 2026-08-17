using System.Collections.Concurrent;

namespace AspnetCoreMvcFull.Services;

public class VisitorPresenceService
{
    private static readonly TimeSpan ActiveWindow = TimeSpan.FromSeconds(45);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _sessions = new();

    public bool RecordHeartbeat(string sessionId)
    {
        var isNew = _sessions.TryAdd(sessionId, DateTimeOffset.UtcNow);
        _sessions[sessionId] = DateTimeOffset.UtcNow;
        RemoveExpiredSessions();
        return isNew;
    }

    public int GetActiveUserCount()
    {
        RemoveExpiredSessions();
        return _sessions.Count;
    }

    private void RemoveExpiredSessions()
    {
        var cutoff = DateTimeOffset.UtcNow - ActiveWindow;
        foreach (var session in _sessions)
        {
            if (session.Value < cutoff)
            {
                _sessions.TryRemove(session.Key, out _);
            }
        }
    }
}