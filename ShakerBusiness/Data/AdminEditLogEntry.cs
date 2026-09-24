namespace ShakerBusiness.Data;

public class AdminEditLogEntry
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string TargetTwitchUserId { get; set; } = "";
    public string TargetDisplayName { get; set; } = "";
    public string AdminTwitchUserId { get; set; } = "";
    public string AdminDisplayName { get; set; } = "";
    public bool AppliedLive { get; set; }
    public string CashBefore { get; set; } = "0";
    public string CashAfter { get; set; } = "0";
    public string LifetimeEarningsBefore { get; set; } = "0";
    public string LifetimeEarningsAfter { get; set; } = "0";
    public string AngelCountBefore { get; set; } = "0";
    public string AngelCountAfter { get; set; } = "0";
    public string EarningsAtLastResetBefore { get; set; } = "0";
    public string EarningsAtLastResetAfter { get; set; } = "0";
    public double OliModifierPercentBefore { get; set; }
    public double OliModifierPercentAfter { get; set; }
    public int PrestigeCountBefore { get; set; }
    public int PrestigeCountAfter { get; set; }
}
