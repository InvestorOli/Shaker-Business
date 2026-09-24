using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ShakerBusiness.Data;

namespace ShakerBusiness.Services;

public sealed class GameEndService : IDisposable
{
    private const int StateRowId = 1;

    private readonly IDbContextFactory<ShakerDbContext>? _dbFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger? _logger;
    private readonly Timer _watcher;
    private readonly object _lock = new();
    private bool _enabled;
    private DateTime? _endsAtUtc;
    private int _endedRaised;

    public GameEndService(IDbContextFactory<ShakerDbContext> dbFactory, TimeProvider timeProvider, ILogger<GameEndService> logger)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider;
        _logger = logger;
        _watcher = new Timer(_ => NotifyIfEnded(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    internal GameEndService(bool enabled, DateTime? endsAtUtc, TimeProvider timeProvider, ILogger? logger = null)
    {
        _enabled = enabled && endsAtUtc is not null;
        _endsAtUtc = endsAtUtc;
        _timeProvider = timeProvider;
        _logger = logger;
        _watcher = new Timer(_ => NotifyIfEnded(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public event Action? Ended;

    public event Action? Changed;

    public bool IsEnabled
    {
        get
        {
            lock (_lock)
            {
                return _enabled;
            }
        }
    }

    public DateTime? EndsAtUtc
    {
        get
        {
            lock (_lock)
            {
                return _endsAtUtc;
            }
        }
    }

    public bool HasEnded => IsEnabled && EndsAtUtc is { } end && UtcNow >= end;

    public TimeSpan Remaining => IsEnabled && EndsAtUtc is { } end && end > UtcNow ? end - UtcNow : TimeSpan.Zero;

    public string? EndsAtLocalText => EndsAtUtc is { } end ? FormatLocal(end) : null;

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task LoadAsync()
    {
        if (_dbFactory is null)
        {
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var state = await db.GlobalGameStates.FirstOrDefaultAsync(s => s.Id == StateRowId);
        if (state is null)
        {
            return;
        }

        lock (_lock)
        {
            _enabled = state.GameOverEnabled && state.GameOverAtUtc is not null;
            _endsAtUtc = state.GameOverAtUtc;
        }

        NotifyIfEnded();
    }

    public async Task SetAsync(bool enabled, DateTime? endsAtUtc)
    {
        var normalized = endsAtUtc is { } value ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : (DateTime?)null;
        var newEnabled = enabled && normalized is not null;
        bool changed;
        lock (_lock)
        {
            changed = _enabled != newEnabled || _endsAtUtc != normalized;
            _enabled = newEnabled;
            _endsAtUtc = normalized;
            if (changed)
            {
                _endedRaised = 0;
            }
        }

        if (_dbFactory is not null)
        {
            await PersistAsync(newEnabled, normalized);
        }

        if (changed)
        {
            Changed?.Invoke();
        }

        NotifyIfEnded();
    }

    private async Task PersistAsync(bool enabled, DateTime? endsAtUtc)
    {
        await using var db = await _dbFactory!.CreateDbContextAsync();
        var state = await db.GlobalGameStates.FirstOrDefaultAsync(s => s.Id == StateRowId);
        if (state is null)
        {
            state = new GlobalGameState { Id = StateRowId };
            db.GlobalGameStates.Add(state);
        }

        state.GameOverEnabled = enabled;
        state.GameOverAtUtc = endsAtUtc;
        await db.SaveChangesAsync();
    }

    internal void NotifyIfEnded()
    {
        if (!HasEnded || Interlocked.Exchange(ref _endedRaised, 1) != 0)
        {
            return;
        }

        foreach (var handler in Ended?.GetInvocationList() ?? [])
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "A game end handler failed");
            }
        }
    }

    private static string FormatLocal(DateTime endsAtUtc) =>
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Berlin", out var berlin)
            ? TimeZoneInfo.ConvertTimeFromUtc(endsAtUtc, berlin).ToString("dd.MM.yyyy 'um' HH:mm 'Uhr'", CultureInfo.InvariantCulture)
            : endsAtUtc.ToString("dd.MM.yyyy 'um' HH:mm 'Uhr (UTC)'", CultureInfo.InvariantCulture);

    public void Dispose() => _watcher.Dispose();
}
