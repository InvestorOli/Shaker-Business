using System.Numerics;

namespace ShakerBusiness.Data;

public class PlayerAccount
{
    public string TwitchUserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
    public BigInteger Cash { get; set; }
    public BigInteger LifetimeEarnings { get; set; }
    public BigInteger EarningsAtLastReset { get; set; }
    public BigInteger AngelCount { get; set; }
    public DateTime LastOnlineUtc { get; set; }

    public bool IsHidden { get; set; }

    public bool IsModerator { get; set; }

    public bool IsDev { get; set; }

    public bool IsChatBanned { get; set; }

    public int OliChatRound { get; set; }

    public double OliModifierPercent { get; set; }

    public int SnakeHighScore { get; set; }

    public int TimberHighScore { get; set; }

    public long FishingXp { get; set; }

    public int ResetCount { get; set; }

    public int PrestigeCount { get; set; }

    public DateTime? LastPrestigeUtc { get; set; }

    public List<PlayerBusiness> Businesses { get; set; } = [];
    public List<PlayerUpgrade> Upgrades { get; set; } = [];
    public List<PlayerAngelUpgrade> AngelUpgrades { get; set; } = [];
}
