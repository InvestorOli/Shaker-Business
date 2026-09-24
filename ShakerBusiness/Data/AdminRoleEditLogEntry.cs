namespace ShakerBusiness.Data;

public class AdminRoleEditLogEntry
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string TargetTwitchUserId { get; set; } = "";
    public string TargetDisplayName { get; set; } = "";
    public string AdminTwitchUserId { get; set; } = "";
    public string AdminDisplayName { get; set; } = "";
    public bool AppliedLive { get; set; }
    public bool ModeratorBefore { get; set; }
    public bool ModeratorAfter { get; set; }
}
