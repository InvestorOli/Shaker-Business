namespace ShakerBusiness.Services;

public sealed class LiveGameSessionDirectory
{
    private readonly object _lock = new();
    private readonly Dictionary<string, GameEngineService> _liveInstances = new();

    public void Register(string twitchUserId, GameEngineService engine)
    {
        lock (_lock)
        {
            _liveInstances[twitchUserId] = engine;
        }
    }

    public void Unregister(string twitchUserId, GameEngineService engine)
    {
        lock (_lock)
        {
            if (_liveInstances.TryGetValue(twitchUserId, out var current) && ReferenceEquals(current, engine))
            {
                _liveInstances.Remove(twitchUserId);
            }
        }
    }

    public bool TryGet(string twitchUserId, out GameEngineService? engine)
    {
        lock (_lock)
        {
            return _liveInstances.TryGetValue(twitchUserId, out engine);
        }
    }
}
