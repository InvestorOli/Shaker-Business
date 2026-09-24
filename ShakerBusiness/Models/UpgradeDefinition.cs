namespace ShakerBusiness.Models;

public sealed record UpgradeDefinition(
    string Id,
    string Name,
    string? BusinessId,
    double RequiredLifetimeEarnings,
    double Cost,
    double ProfitMultiplier,
    double AngelEffectivenessBonus = 0);
