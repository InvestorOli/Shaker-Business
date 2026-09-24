namespace ShakerBusiness.Models;

public sealed record BusinessDefinition(
    string Id,
    string Name,
    string ImagePath,
    double InitialCost,
    double Coefficient,
    double InitialCycleSeconds,
    double InitialRevenue,
    double ManagerCost,
    int InstantMilestoneOwned);
