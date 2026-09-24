namespace ShakerBusiness.Data;

public class PlayerUpgrade
{
    public string TwitchUserId { get; set; } = "";
    public string UpgradeId { get; set; } = "";

    public PlayerAccount? Account { get; set; }
}
