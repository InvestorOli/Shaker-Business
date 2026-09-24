namespace ShakerBusiness.Data;

public class PlayerFishCatch
{
    public int Id { get; set; }
    public string TwitchUserId { get; set; } = "";
    public string FishId { get; set; } = "";
    public int Level { get; set; }
    public DateTime CaughtUtc { get; set; }
}
