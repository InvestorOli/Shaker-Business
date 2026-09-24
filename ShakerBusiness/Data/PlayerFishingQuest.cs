namespace ShakerBusiness.Data;

public class PlayerFishingQuest
{
    public int Id { get; set; }
    public string TwitchUserId { get; set; } = "";
    public string FishId { get; set; } = "";
    public bool Completed { get; set; }
    public DateTime BatchUtc { get; set; }
}
