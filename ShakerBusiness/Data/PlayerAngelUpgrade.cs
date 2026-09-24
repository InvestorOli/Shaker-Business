namespace ShakerBusiness.Data;

public class PlayerAngelUpgrade
{
    public string TwitchUserId { get; set; } = "";
    public string AngelUpgradeId { get; set; } = "";

    public PlayerAccount? Account { get; set; }
}
