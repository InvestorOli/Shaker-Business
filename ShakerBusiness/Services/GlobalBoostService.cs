using Microsoft.EntityFrameworkCore;
using ShakerBusiness.Data;

namespace ShakerBusiness.Services;

public sealed class GlobalBoostService(IDbContextFactory<ShakerDbContext> dbFactory)
{
    private const int StateRowId = 1;

    private readonly object _lock = new();
    private double _multiplier = 1.0;
    private DateTime _expiresUtc = DateTime.MinValue;

    public double CurrentMultiplier
    {
        get
        {
            lock (_lock)
            {
                return DateTime.UtcNow < _expiresUtc ? _multiplier : 1.0;
            }
        }
    }

    public bool IsActive
    {
        get
        {
            lock (_lock)
            {
                return DateTime.UtcNow < _expiresUtc;
            }
        }
    }

    public DateTime? ExpiresUtc
    {
        get
        {
            lock (_lock)
            {
                return DateTime.UtcNow < _expiresUtc ? _expiresUtc : null;
            }
        }
    }

    public async Task LoadAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var state = await db.GlobalGameStates.FirstOrDefaultAsync(s => s.Id == StateRowId);
        if (state is null)
        {
            return;
        }

        lock (_lock)
        {
            _multiplier = state.BoostMultiplier;
            _expiresUtc = state.BoostExpiresUtc ?? DateTime.MinValue;
        }
    }

    public async Task StartAsync(double multiplier, TimeSpan duration)
    {
        var expiresUtc = DateTime.UtcNow + duration;
        lock (_lock)
        {
            _multiplier = multiplier;
            _expiresUtc = expiresUtc;
        }

        await PersistAsync(multiplier, expiresUtc);
    }

    public async Task StopAsync()
    {
        lock (_lock)
        {
            _expiresUtc = DateTime.MinValue;
        }

        await PersistAsync(_multiplier, null);
    }

    private async Task PersistAsync(double multiplier, DateTime? expiresUtc)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var state = await db.GlobalGameStates.FirstOrDefaultAsync(s => s.Id == StateRowId);
        if (state is null)
        {
            state = new GlobalGameState { Id = StateRowId };
            db.GlobalGameStates.Add(state);
        }

        state.BoostMultiplier = multiplier;
        state.BoostExpiresUtc = expiresUtc;
        await db.SaveChangesAsync();
    }
}
