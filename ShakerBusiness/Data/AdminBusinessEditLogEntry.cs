namespace ShakerBusiness.Data;

public class AdminBusinessEditLogEntry
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string TargetTwitchUserId { get; set; } = "";
    public string TargetDisplayName { get; set; } = "";
    public string AdminTwitchUserId { get; set; } = "";
    public string AdminDisplayName { get; set; } = "";
    public bool AppliedLive { get; set; }
    public string BusinessId { get; set; } = "";
    public int OwnedBefore { get; set; }
    public int OwnedAfter { get; set; }
}
