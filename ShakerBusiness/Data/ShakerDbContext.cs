using System.Numerics;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShakerBusiness.Models;

namespace ShakerBusiness.Data;

public class ShakerDbContext(DbContextOptions<ShakerDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<PlayerAccount> PlayerAccounts => Set<PlayerAccount>();
    public DbSet<PlayerBusiness> PlayerBusinesses => Set<PlayerBusiness>();
    public DbSet<PlayerUpgrade> PlayerUpgrades => Set<PlayerUpgrade>();
    public DbSet<PlayerAngelUpgrade> PlayerAngelUpgrades => Set<PlayerAngelUpgrade>();
    public DbSet<ResetLogEntry> ResetLogEntries => Set<ResetLogEntry>();
    public DbSet<AdminEditLogEntry> AdminEditLogEntries => Set<AdminEditLogEntry>();
    public DbSet<AdminBusinessEditLogEntry> AdminBusinessEditLogEntries => Set<AdminBusinessEditLogEntry>();
    public DbSet<AdminRoleEditLogEntry> AdminRoleEditLogEntries => Set<AdminRoleEditLogEntry>();
    public DbSet<PlayerFishingProfile> PlayerFishingProfiles => Set<PlayerFishingProfile>();
    public DbSet<PlayerFishCatch> PlayerFishCatches => Set<PlayerFishCatch>();
    public DbSet<PlayerBaitInventoryEntry> PlayerBaitInventoryEntries => Set<PlayerBaitInventoryEntry>();
    public DbSet<PlayerFishingQuest> PlayerFishingQuests => Set<PlayerFishingQuest>();
    public DbSet<PlayerFishDiscovery> PlayerFishDiscoveries => Set<PlayerFishDiscovery>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<GlobalGameState> GlobalGameStates => Set<GlobalGameState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var bigIntegerConverter = new ValueConverter<BigInteger, string>(
            v => v.ToString(),
            v => BigInteger.Parse(v));

        modelBuilder.Entity<PlayerAccount>(entity =>
        {
            entity.HasKey(p => p.TwitchUserId);
            entity.Property(p => p.TwitchUserId).HasMaxLength(64);
            entity.Property(p => p.DisplayName).HasMaxLength(100);
            entity.Property(p => p.ProfileImageUrl).HasMaxLength(500);

            entity.Property(p => p.Cash).HasConversion(bigIntegerConverter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.LifetimeEarnings).HasConversion(bigIntegerConverter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.EarningsAtLastReset).HasConversion(bigIntegerConverter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.AngelCount).HasConversion(bigIntegerConverter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.FishingXp).HasDefaultValue(0L);
        });

        modelBuilder.Entity<PlayerBusiness>(entity =>
        {
            entity.HasKey(p => new { p.TwitchUserId, p.BusinessId });
            entity.Property(p => p.BusinessId).HasMaxLength(64);

            entity.HasOne(p => p.Account)
                .WithMany(a => a.Businesses)
                .HasForeignKey(p => p.TwitchUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerUpgrade>(entity =>
        {
            entity.HasKey(p => new { p.TwitchUserId, p.UpgradeId });
            entity.Property(p => p.UpgradeId).HasMaxLength(64);

            entity.HasOne(p => p.Account)
                .WithMany(a => a.Upgrades)
                .HasForeignKey(p => p.TwitchUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerAngelUpgrade>(entity =>
        {
            entity.HasKey(p => new { p.TwitchUserId, p.AngelUpgradeId });
            entity.Property(p => p.AngelUpgradeId).HasMaxLength(64);

            entity.HasOne(p => p.Account)
                .WithMany(a => a.AngelUpgrades)
                .HasForeignKey(p => p.TwitchUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResetLogEntry>(entity =>
        {
            entity.Property(p => p.TwitchUserId).HasMaxLength(64);
            entity.Property(p => p.DisplayName).HasMaxLength(100);
            entity.Property(p => p.CashBefore).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.LifetimeEarningsBefore).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.EarningsAtLastResetBefore).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.AngelCountBefore).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.AngelsGained).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.AngelCountAfter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.BusinessLevelsBeforeJson).HasMaxLength(2000);

            entity.HasIndex(p => p.TwitchUserId);
            entity.HasIndex(p => p.TimestampUtc);
        });

        modelBuilder.Entity<AdminEditLogEntry>(entity =>
        {
            entity.Property(p => p.TargetTwitchUserId).HasMaxLength(64);
            entity.Property(p => p.TargetDisplayName).HasMaxLength(100);
            entity.Property(p => p.AdminTwitchUserId).HasMaxLength(64);
            entity.Property(p => p.AdminDisplayName).HasMaxLength(100);
            entity.Property(p => p.CashBefore).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.CashAfter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.LifetimeEarningsBefore).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.LifetimeEarningsAfter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.AngelCountBefore).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.AngelCountAfter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.EarningsAtLastResetBefore).HasMaxLength(BigNumberFormatter.MaxStoredDigits);
            entity.Property(p => p.EarningsAtLastResetAfter).HasMaxLength(BigNumberFormatter.MaxStoredDigits);

            entity.HasIndex(p => p.TargetTwitchUserId);
            entity.HasIndex(p => p.TimestampUtc);
        });

        modelBuilder.Entity<AdminBusinessEditLogEntry>(entity =>
        {
            entity.Property(p => p.TargetTwitchUserId).HasMaxLength(64);
            entity.Property(p => p.TargetDisplayName).HasMaxLength(100);
            entity.Property(p => p.AdminTwitchUserId).HasMaxLength(64);
            entity.Property(p => p.AdminDisplayName).HasMaxLength(100);
            entity.Property(p => p.BusinessId).HasMaxLength(64);

            entity.HasIndex(p => p.TargetTwitchUserId);
            entity.HasIndex(p => p.TimestampUtc);
        });

        modelBuilder.Entity<AdminRoleEditLogEntry>(entity =>
        {
            entity.Property(p => p.TargetTwitchUserId).HasMaxLength(64);
            entity.Property(p => p.TargetDisplayName).HasMaxLength(100);
            entity.Property(p => p.AdminTwitchUserId).HasMaxLength(64);
            entity.Property(p => p.AdminDisplayName).HasMaxLength(100);

            entity.HasIndex(p => p.TargetTwitchUserId);
            entity.HasIndex(p => p.TimestampUtc);
        });

        modelBuilder.Entity<PlayerFishingProfile>(entity =>
        {
            entity.HasKey(p => p.TwitchUserId);
            entity.Property(p => p.RodLevel).HasDefaultValue(ShakerBusiness.Models.FishingData.MinRodLevel);
            entity.Property(p => p.Pearls).HasDefaultValue(ShakerBusiness.Models.FishingData.StartingPearls);
            entity.Property(p => p.ActiveBaitId).HasMaxLength(64);

            entity.HasOne(p => p.Account)
                .WithMany()
                .HasForeignKey(p => p.TwitchUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerFishCatch>(entity =>
        {
            entity.Property(p => p.TwitchUserId).HasMaxLength(64);
            entity.Property(p => p.FishId).HasMaxLength(64);

            entity.HasIndex(p => new { p.TwitchUserId, p.FishId });
        });

        modelBuilder.Entity<PlayerBaitInventoryEntry>(entity =>
        {
            entity.HasKey(p => new { p.TwitchUserId, p.BaitId });
            entity.Property(p => p.TwitchUserId).HasMaxLength(64);
            entity.Property(p => p.BaitId).HasMaxLength(64);
        });

        modelBuilder.Entity<PlayerFishingQuest>(entity =>
        {
            entity.Property(p => p.TwitchUserId).HasMaxLength(64);
            entity.Property(p => p.FishId).HasMaxLength(64);

            entity.HasIndex(p => p.TwitchUserId);
        });

        modelBuilder.Entity<PlayerFishDiscovery>(entity =>
        {
            entity.HasKey(p => new { p.TwitchUserId, p.FishId });
            entity.Property(p => p.TwitchUserId).HasMaxLength(64);
            entity.Property(p => p.FishId).HasMaxLength(64);
        });

        modelBuilder.Entity<GlobalGameState>(entity =>
        {
            entity.Property(p => p.Id).ValueGeneratedNever();
        });
    }
}
