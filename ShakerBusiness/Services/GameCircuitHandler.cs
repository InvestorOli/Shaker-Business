using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ShakerBusiness.Services;

public sealed class GameCircuitHandler(GameEngineService engine, ILogger<GameCircuitHandler> logger) : CircuitHandler
{
    public override async Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        engine.OnConnectionDown();

        if (!engine.IsAuthenticated || engine.IsGuest)
        {
            return;
        }

        try
        {
            await engine.FlushAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Flushing player state after connection loss failed.");
        }
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        engine.OnConnectionUp();
        return Task.CompletedTask;
    }
}
