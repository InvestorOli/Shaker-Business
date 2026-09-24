namespace ShakerBusiness.Data;

public class GlobalGameState
{
    public int Id { get; set; }
    public double BoostMultiplier { get; set; } = 1.0;
    public DateTime? BoostExpiresUtc { get; set; }
    public bool MaintenanceModeEnabled { get; set; }
    public bool GameOverEnabled { get; set; }
    public DateTime? GameOverAtUtc { get; set; }
}
