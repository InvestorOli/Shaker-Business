namespace ShakerBusiness.Data;

public class PlayerFishingProfile
{
    public string TwitchUserId { get; set; } = "";
    public long Pearls { get; set; }
    public int RodLevel { get; set; }
    public string? ActiveBaitId { get; set; }

    public PlayerAccount? Account { get; set; }
}
