namespace ShakerBusiness.Data;

public class ResetLogEntry
{
    public int Id { get; set; }
    public string TwitchUserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTime TimestampUtc { get; set; }
    public string CashBefore { get; set; } = "0";
    public string LifetimeEarningsBefore { get; set; } = "0";
    public string EarningsAtLastResetBefore { get; set; } = "0";
    public string AngelCountBefore { get; set; } = "0";
    public string AngelsGained { get; set; } = "0";
    public double OliModifierPercentApplied { get; set; }
    public string AngelCountAfter { get; set; } = "0";
    public string BusinessLevelsBeforeJson { get; set; } = "{}";
    public bool IsPrestige { get; set; }
    public int PrestigeCountAfter { get; set; }
}
