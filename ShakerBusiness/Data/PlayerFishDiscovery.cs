namespace ShakerBusiness.Data;

public class PlayerFishDiscovery
{
    public string TwitchUserId { get; set; } = "";
    public string FishId { get; set; } = "";
    public int BestLevel { get; set; }
    public DateTime FirstCaughtUtc { get; set; }
}
