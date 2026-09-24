using Microsoft.EntityFrameworkCore;
using ShakerBusiness.Data;

namespace ShakerBusiness.Services;

public sealed class MaintenanceModeService(IDbContextFactory<ShakerDbContext> dbFactory)
{
    private const int StateRowId = 1;

    private readonly object _lock = new();
    private bool _enabled;

    public event Action? OnChanged;

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
            _enabled = state.MaintenanceModeEnabled;
        }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        lock (_lock)
        {
            _enabled = enabled;
        }

        await PersistAsync(enabled);
        OnChanged?.Invoke();
    }

    private async Task PersistAsync(bool enabled)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var state = await db.GlobalGameStates.FirstOrDefaultAsync(s => s.Id == StateRowId);
        if (state is null)
        {
            state = new GlobalGameState { Id = StateRowId };
            db.GlobalGameStates.Add(state);
        }

        state.MaintenanceModeEnabled = enabled;
        await db.SaveChangesAsync();
    }
}
