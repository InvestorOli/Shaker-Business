namespace ShakerBusiness.Services;

public enum PresenceStatus
{
    Offline,
    Idle,
    Online,
}

public static class PresenceStatusExtensions
{
    public static string DotColorClass(this PresenceStatus status) => status switch
    {
        PresenceStatus.Online => "bg-cartel-agave",
        PresenceStatus.Idle => "bg-cartel-gold",
        _ => "bg-cartel-ink/30",
    };

    public static string TextColorClass(this PresenceStatus status) => status switch
    {
        PresenceStatus.Online => "text-cartel-agave",
        PresenceStatus.Idle => "text-cartel-gold",
        _ => "text-cartel-ink/50",
    };

    public static string Label(this PresenceStatus status) => status switch
    {
        PresenceStatus.Online => "Online",
        PresenceStatus.Idle => "Abwesend",
        _ => "Offline",
    };
}

public sealed class PlayerSessionRegistry
{
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(3);

    private readonly object _lock = new();
    private readonly Dictionary<string, Guid> _activeSessions = new();
    private readonly Dictionary<string, DateTime> _lastActivityUtc = new();
    private readonly HashSet<string> _lockedUserIds = new();

    public event Action<string, Guid>? SessionSuperseded;

    public Guid RegisterSession(string twitchUserId)
    {
        var sessionId = Guid.NewGuid();
        Guid? previous = null;

        lock (_lock)
        {
            if (_activeSessions.TryGetValue(twitchUserId, out var existing))
            {
                previous = existing;
            }

            _activeSessions[twitchUserId] = sessionId;
            _lastActivityUtc[twitchUserId] = DateTime.UtcNow;
        }

        if (previous is { } oldSessionId)
        {
            SessionSuperseded?.Invoke(twitchUserId, oldSessionId);
        }

        return sessionId;
    }

    public void UnregisterSession(string twitchUserId, Guid sessionId)
    {
        lock (_lock)
        {
            if (_activeSessions.TryGetValue(twitchUserId, out var current) && current == sessionId)
            {
                _activeSessions.Remove(twitchUserId);
                _lastActivityUtc.Remove(twitchUserId);
                _lockedUserIds.Remove(twitchUserId);
            }
        }
    }

    public void SetLocked(string twitchUserId, bool locked)
    {
        lock (_lock)
        {
            if (locked)
            {
                _lockedUserIds.Add(twitchUserId);
            }
            else
            {
                _lockedUserIds.Remove(twitchUserId);
            }
        }
    }

    public void MarkOnline(string twitchUserId, Guid sessionId)
    {
        lock (_lock)
        {
            if (!_activeSessions.ContainsKey(twitchUserId))
            {
                _activeSessions[twitchUserId] = sessionId;
            }

            _lastActivityUtc[twitchUserId] = DateTime.UtcNow;
        }
    }

    public void TouchActivity(string? twitchUserId)
    {
        if (twitchUserId is null)
        {
            return;
        }

        lock (_lock)
        {
            if (_activeSessions.ContainsKey(twitchUserId))
            {
                _lastActivityUtc[twitchUserId] = DateTime.UtcNow;
            }
        }
    }

    public bool IsOnline(string twitchUserId)
    {
        lock (_lock)
        {
            return _activeSessions.ContainsKey(twitchUserId) && !_lockedUserIds.Contains(twitchUserId);
        }
    }

    public PresenceStatus GetPresence(string twitchUserId)
    {
        lock (_lock)
        {
            if (!_activeSessions.ContainsKey(twitchUserId) || _lockedUserIds.Contains(twitchUserId))
            {
                return PresenceStatus.Offline;
            }

            if (_lastActivityUtc.TryGetValue(twitchUserId, out var lastActivity) && DateTime.UtcNow - lastActivity < IdleThreshold)
            {
                return PresenceStatus.Online;
            }

            return PresenceStatus.Idle;
        }
    }

    public int OnlineCount
    {
        get
        {
            lock (_lock)
            {
                return _activeSessions.Keys.Count(id => !_lockedUserIds.Contains(id));
            }
        }
    }
}
