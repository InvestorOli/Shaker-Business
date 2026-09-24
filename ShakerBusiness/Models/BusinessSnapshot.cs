using System.Numerics;

namespace ShakerBusiness.Models;

public sealed record BusinessSnapshot(
    BusinessDefinition Definition,
    int Owned,
    bool HasManager,
    double ProgressFraction,
    double EffectiveCycleSeconds,
    BigValue EffectiveRevenue,
    BigInteger BuyCost,
    int BuyQuantity,
    bool Unlocked,
    bool CanAffordBuy,
    bool CanAffordManager,
    int? NextMilestone,
    DateTime? CycleAnchorUtc);
