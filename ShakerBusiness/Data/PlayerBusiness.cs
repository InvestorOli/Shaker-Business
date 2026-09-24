namespace ShakerBusiness.Data;

public class PlayerBusiness
{
    public string TwitchUserId { get; set; } = "";
    public string BusinessId { get; set; } = "";
    public int Owned { get; set; }
    public bool HasManager { get; set; }
    public DateTime? CycleAnchorUtc { get; set; }

    public PlayerAccount? Account { get; set; }
}
