using System.Numerics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using ShakerBusiness.Data;
using ShakerBusiness.Models;

namespace ShakerBusiness.Services;

public sealed class GameEngineService(
    AuthenticationStateProvider authStateProvider,
    IDbContextFactory<ShakerDbContext> dbFactory,
    IDataProtectionProvider dataProtectionProvider,
    IJSRuntime jsRuntime,
    PlayerSessionRegistry sessionRegistry,
    LiveGameSessionDirectory liveDirectory,
    GlobalBoostService globalBoost,
    MaintenanceModeService maintenanceMode,
    GameEndService gameEnd,
    ILogger<GameEngineService> logger) : IDisposable
{
    private readonly IDataProtector _manualClickProtector =
        dataProtectionProvider.CreateProtector("Shakercasino.ManualClickToken.v1");

    private readonly IDataProtector _chatHistoryProtector =
        dataProtectionProvider.CreateProtector("Shakercasino.OliChatHistory.v1");

    private readonly IDataProtector _guestStateProtector =
        dataProtectionProvider.CreateProtector("Shakercasino.GuestState.v1");

    private static readonly TimeSpan AutoSaveInterval = TimeSpan.FromSeconds(10);
    private const double CarryPrecisionLimit = 1e15;

    private static readonly UpgradeDefinition[] UpgradesByCost = GameData.Upgrades.OrderBy(u => u.Cost).ToArray();
    private static readonly Dictionary<string, BigInteger> UpgradeCostById = GameData.Upgrades.ToDictionary(u => u.Id, u => new BigInteger(Math.Ceiling(u.Cost)));
    private static readonly Dictionary<string, BigInteger> UpgradeRequiredLifetimeById = GameData.Upgrades.ToDictionary(u => u.Id, u => new BigInteger(u.RequiredLifetimeEarnings));
    private static readonly AngelUpgradeDefinition[] AngelUpgradesByCost = AngelUpgradeData.Upgrades.OrderBy(u => u.Cost).ToArray();
    private static readonly TimeSpan MaxOfflineCatchUp = TimeSpan.FromDays(7);
    private static readonly TimeSpan MinOfflineDurationForPopup = TimeSpan.FromSeconds(30);

    private sealed class BusinessRuntimeState
    {
        public int Owned;
        public bool HasManager;
        public DateTime? CycleAnchorUtc;

        public double RevenueCarry;
    }

    private BigInteger CreditRevenue(BusinessRuntimeState state, BigValue revenuePerCycle, double cycles = 1)
    {
        var pendingRevenue = revenuePerCycle.Multiply(cycles);
        if (!pendingRevenue.FitsDouble || pendingRevenue.ToDouble() >= CarryPrecisionLimit)
        {
            return ClampToEarningsCap(pendingRevenue.ToBigInteger());
        }

        var total = state.RevenueCarry + pendingRevenue.ToDouble();
        var whole = Math.Floor(total);
        state.RevenueCarry = total - whole;
        return ClampToEarningsCap(new BigInteger(whole));
    }

    private BigInteger ClampToEarningsCap(BigInteger gain) =>
        BigInteger.Min(gain, BigInteger.Max(BigInteger.Zero, BigNumberFormatter.EarningsCap - _lifetimeEarnings));

    public bool IsRetired => GameData.IsRetired(_prestigeCount);

    public bool CanPrestige => !_isGuest && !IsRetired && !IsGameOver && IsGameFullyUnlocked;

    private string? _twitchUserId;
    private bool _isGuest;
    private BigInteger _cash;
    private BigInteger _lifetimeEarnings;
    private BigInteger _earningsAtLastReset;
    private BigInteger _angelCount;
    private BigInteger _angelMultiplierAngels = BigInteger.MinusOne;
    private double _angelMultiplierRate = double.NaN;
    private BigValue _angelMultiplierValue = BigValue.One;
    private HashSet<string> _purchasedUpgrades = [];
    private HashSet<string> _ownedAngelUpgrades = [];
    private double _angelAllProfitMultiplier = 1.0;
    private double _cashAllProfitMultiplier = 1.0;
    private Dictionary<string, double> _cashBusinessProfitMultipliers = [];
    private double _angelEffectivenessBonus;
    private Dictionary<string, double> _angelBusinessProfitMultiplier = [];
    private Dictionary<string, BusinessRuntimeState> _businessStates = [];
    private DateTime _lastSaveUtc;
    private bool _dirty;

    private readonly SemaphoreSlim _persistLock = new(1, 1);

    private Guid _sessionId;
    private volatile bool _sessionSuperseded;

    public event Action? SessionSuperseded;

    public bool IsSessionSuperseded => _sessionSuperseded;

    private int _oliChatRound;
    private double _oliModifierPercent;
    private bool _isModerator;
    private bool _isDev;
    private bool _isChatBanned;
    private bool _isHidden;
    private int _snakeHighScore;
    private int _timberHighScore;
    private int _resetCount;
    private int _prestigeCount;
    private DateTime? _lastPrestigeUtc;

    public bool IsAuthenticated { get; private set; }

    public bool IsInitialized { get; private set; }

    public bool IsGuest => _isGuest;

    public string? CurrentTwitchUserId => _twitchUserId;

    private string IdentityKey => _twitchUserId ?? "guest";
    public string? DisplayName { get; private set; }
    public string? ProfileImageUrl { get; private set; }
    public BuyMultiplier BuyMode { get; private set; } = BuyMultiplier.X1;

    public bool IsRevenueBoostActive => globalBoost.IsActive;
    public double RevenueBoostMultiplier => globalBoost.CurrentMultiplier;
    public DateTime? RevenueBoostExpiresUtc => globalBoost.ExpiresUtc;
    public bool IsMaintenanceLocked => maintenanceMode.IsEnabled && !_isDev;
    public bool IsGameOver => gameEnd.HasEnded;

    private bool IsFrozen => _sessionSuperseded || IsGameOver;

    public BigInteger LastOfflineEarnings { get; private set; }
    public TimeSpan LastOfflineDuration { get; private set; }
    public string? LastOfflineQuip { get; private set; }

    public BigInteger Cash => _cash;
    public (string Value, string Suffix) CashSplit => BigNumberFormatter.Split(_cash);
    public BigInteger LifetimeEarnings => _lifetimeEarnings;
    public BigInteger AngelCount => _angelCount;
    public double AngelEffectivenessBaseRate => 0.02;
    public double AngelEffectivenessBonus => _angelEffectivenessBonus;
    public int PrestigeCount => _prestigeCount;
    public DateTime? LastPrestigeUtc => _lastPrestigeUtc;
    public double AngelEffectivenessRate => AngelEffectivenessBaseRate + _angelEffectivenessBonus;
    public BigValue AngelProfitMultiplier
    {
        get
        {
            var rate = AngelEffectivenessRate;
            if (_angelCount != _angelMultiplierAngels || rate != _angelMultiplierRate)
            {
                _angelMultiplierValue = BigValue.One + BigValue.FromBigInteger(_angelCount).Multiply(rate);
                _angelMultiplierAngels = _angelCount;
                _angelMultiplierRate = rate;
            }

            return _angelMultiplierValue;
        }
    }

    public BigInteger AngelPreview
    {
        get
        {
            var baseGain = BigInteger.Max(BigInteger.Zero, EconomyMath.AngelsFor(_lifetimeEarnings) - EconomyMath.AngelsFor(_earningsAtLastReset));
            return _oliChatRound >= 3
                ? BigInteger.Max(BigInteger.Zero, EconomyMath.ScaleRounded(baseGain, 1 + _oliModifierPercent))
                : baseGain;
        }
    }

    public bool HasInvestorDealWorthOffering
    {
        get
        {
            var preview = AngelPreview;
            return _oliChatRound < 3 && preview > 1 && preview > _angelCount;
        }
    }

    public int OliChatRound => _oliChatRound;

    public double OliModifierPercent => _oliModifierPercent;

    public bool IsModerator => _isModerator;

    public bool IsDev => _isDev;

    public bool CanModerateChat => _isModerator || _isDev;

    public bool IsChatBanned => _isChatBanned;

    public bool IsHidden => _isHidden;

    public int SnakeHighScore => _snakeHighScore;

    public int TimberHighScore => _timberHighScore;

    public async Task InitializeAsync()
    {
        IsInitialized = true;
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            IsAuthenticated = false;
            return;
        }

        _twitchUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        DisplayName = user.FindFirst(ClaimTypes.Name)?.Value;
        ProfileImageUrl = user.FindFirst("urn:twitch:profileimage")?.Value;

        if (_twitchUserId is null)
        {
            IsAuthenticated = false;
            return;
        }

        IsAuthenticated = true;
        _isGuest = false;

        sessionRegistry.SessionSuperseded += OnRegistrySessionSuperseded;
        _sessionId = sessionRegistry.RegisterSession(_twitchUserId);
        liveDirectory.Register(_twitchUserId, this);

        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts
            .AsSplitQuery()
            .Include(a => a.Businesses)
            .Include(a => a.Upgrades)
            .Include(a => a.AngelUpgrades)
            .FirstOrDefaultAsync(a => a.TwitchUserId == _twitchUserId);

        var now = DateTime.UtcNow;

        if (account is null)
        {
            account = new PlayerAccount
            {
                TwitchUserId = _twitchUserId,
                DisplayName = DisplayName ?? "Unbekannt",
                ProfileImageUrl = ProfileImageUrl,
                Cash = 0,
                LifetimeEarnings = 0,
                EarningsAtLastReset = 0,
                AngelCount = 0,
                LastOnlineUtc = now,
            };

            for (var i = 0; i < GameData.Businesses.Count; i++)
            {
                var business = GameData.Businesses[i];
                account.Businesses.Add(new PlayerBusiness
                {
                    TwitchUserId = _twitchUserId,
                    BusinessId = business.Id,
                    Owned = i == 0 ? 1 : 0,
                    HasManager = false,
                });
            }

            db.PlayerAccounts.Add(account);
            await db.SaveChangesAsync();
        }
        else
        {
            account.DisplayName = DisplayName ?? account.DisplayName;
            account.ProfileImageUrl = ProfileImageUrl ?? account.ProfileImageUrl;

            foreach (var business in GameData.Businesses)
            {
                if (account.Businesses.All(b => b.BusinessId != business.Id))
                {
                    var row = new PlayerBusiness { TwitchUserId = _twitchUserId, BusinessId = business.Id, Owned = 0, HasManager = false };
                    account.Businesses.Add(row);
                    db.PlayerBusinesses.Add(row);
                }
            }
        }

        _cash = account.Cash;
        _lifetimeEarnings = account.LifetimeEarnings;
        _earningsAtLastReset = account.EarningsAtLastReset;
        _angelCount = account.AngelCount;
        _oliChatRound = account.OliChatRound;
        _oliModifierPercent = account.OliModifierPercent;
        _isModerator = account.IsModerator;
        _isDev = account.IsDev;
        _isChatBanned = account.IsChatBanned;
        _isHidden = account.IsHidden;
        maintenanceMode.OnChanged -= SyncMaintenanceLock;
        maintenanceMode.OnChanged += SyncMaintenanceLock;
        SyncMaintenanceLock();
        _snakeHighScore = account.SnakeHighScore;
        _timberHighScore = account.TimberHighScore;
        _resetCount = account.ResetCount;
        _prestigeCount = account.PrestigeCount;
        _lastPrestigeUtc = account.LastPrestigeUtc;
        _purchasedUpgrades = account.Upgrades.Select(u => u.UpgradeId).ToHashSet();
        _purchasedUpgrades.RemoveWhere(id => GameData.PrestigeUpgradeLevel(id) > _prestigeCount);
        _ownedAngelUpgrades = account.AngelUpgrades.Select(u => u.AngelUpgradeId).ToHashSet();
        RecomputeUpgradeEffectCache();
        LoadBusinessStates(account.Businesses.Select(b => (b.BusinessId, b.Owned, b.HasManager, b.CycleAnchorUtc)));
        ApplyOfflineCatchUp(account.LastOnlineUtc, EarningsClockUtc(now));

        account.LastOnlineUtc = now;
        account.Cash = _cash;
        account.LifetimeEarnings = _lifetimeEarnings;
        account.EarningsAtLastReset = _earningsAtLastReset;

        foreach (var row in account.Businesses)
        {
            if (!_businessStates.TryGetValue(row.BusinessId, out var state))
            {
                continue;
            }

            row.Owned = state.Owned;
            row.HasManager = state.HasManager;
            row.CycleAnchorUtc = state.CycleAnchorUtc;
        }

        await db.SaveChangesAsync();

        _lastSaveUtc = now;
        _dirty = false;
    }

    private sealed record GuestBusinessDto(string BusinessId, int Owned, bool HasManager, DateTime? CycleAnchorUtc);

    private sealed record GuestStateDto(
        string Cash,
        string LifetimeEarnings,
        string AngelCount,
        int OliChatRound,
        double OliModifierPercent,
        DateTime LastOnlineUtc,
        List<string> PurchasedUpgradeIds,
        List<GuestBusinessDto> Businesses,
        string? EarningsAtLastReset = null,
        List<string>? OwnedAngelUpgradeIds = null);

    public async Task InitializeGuestAsync(string? encryptedState)
    {
        _isGuest = true;
        _twitchUserId = null;
        DisplayName = "Gast";
        ProfileImageUrl = null;
        IsAuthenticated = true;

        var now = DateTime.UtcNow;
        GuestStateDto? loaded = null;

        if (!string.IsNullOrEmpty(encryptedState))
        {
            try
            {
                var json = _guestStateProtector.Unprotect(encryptedState);
                loaded = JsonSerializer.Deserialize<GuestStateDto>(json);
            }
            catch (CryptographicException)
            {
            }
            catch (JsonException)
            {
            }
        }

        if (loaded is not null)
        {
            _cash = BigInteger.Parse(loaded.Cash);
            _lifetimeEarnings = BigInteger.Parse(loaded.LifetimeEarnings);
            _earningsAtLastReset = string.IsNullOrEmpty(loaded.EarningsAtLastReset) ? BigInteger.Zero : BigInteger.Parse(loaded.EarningsAtLastReset);
            _angelCount = BigInteger.Parse(loaded.AngelCount);
            _oliChatRound = loaded.OliChatRound;
            _oliModifierPercent = loaded.OliModifierPercent;
            _purchasedUpgrades = loaded.PurchasedUpgradeIds.ToHashSet();
            _ownedAngelUpgrades = (loaded.OwnedAngelUpgradeIds ?? []).ToHashSet();
            LoadBusinessStates(loaded.Businesses.Select(b => (b.BusinessId, b.Owned, b.HasManager, b.CycleAnchorUtc)));
            ApplyOfflineCatchUp(loaded.LastOnlineUtc, EarningsClockUtc(now));
        }
        else
        {
            _cash = 0;
            _lifetimeEarnings = 0;
            _earningsAtLastReset = 0;
            _angelCount = 0;
            _oliChatRound = 0;
            _oliModifierPercent = 0;
            _purchasedUpgrades = [];
            _ownedAngelUpgrades = [];
            LoadBusinessStates(GameData.Businesses.Select((b, i) => (b.Id, i == 0 ? 1 : 0, false, (DateTime?)null)));
            LastOfflineEarnings = 0;
        }

        RecomputeUpgradeEffectCache();
        _lastSaveUtc = now;
        _dirty = true;
        await PersistAsync();
    }

    private void LoadBusinessStates(IEnumerable<(string BusinessId, int Owned, bool HasManager, DateTime? CycleAnchorUtc)> rows)
    {
        _businessStates = rows
            .Where(r => GameData.Businesses.Any(def => def.Id == r.BusinessId))
            .ToDictionary(
                r => r.BusinessId,
                r => new BusinessRuntimeState { Owned = r.Owned, HasManager = r.HasManager, CycleAnchorUtc = r.CycleAnchorUtc });

        if (_businessStates.Values.All(s => s.Owned <= 0) &&
            _businessStates.TryGetValue(GameData.Businesses[0].Id, out var starterState))
        {
            starterState.Owned = 1;
        }

        var highestOwnedIndex = -1;
        for (var i = 0; i < GameData.Businesses.Count; i++)
        {
            if (_businessStates.TryGetValue(GameData.Businesses[i].Id, out var s) && s.Owned > 0)
            {
                highestOwnedIndex = i;
            }
        }

        for (var i = 0; i < highestOwnedIndex; i++)
        {
            if (_businessStates.TryGetValue(GameData.Businesses[i].Id, out var s) && s.Owned <= 0)
            {
                s.Owned = 1;
            }
        }
    }

    private DateTime EarningsClockUtc(DateTime now) => gameEnd.HasEnded && gameEnd.EndsAtUtc is { } end ? end : now;

    private void ApplyOfflineCatchUp(DateTime lastOnlineUtc, DateTime now)
    {
        var elapsedOffline = now - lastOnlineUtc;
        if (elapsedOffline > MaxOfflineCatchUp)
        {
            elapsedOffline = MaxOfflineCatchUp;
        }

        LastOfflineEarnings = 0;
        LastOfflineDuration = TimeSpan.Zero;
        LastOfflineQuip = null;

        if (elapsedOffline <= TimeSpan.Zero)
        {
            return;
        }

        var globalSync = GetGlobalSyncMultiplier();
        var totalOfflineGain = BigInteger.Zero;
        var offlineFloor = now - elapsedOffline;
        var capFloor = now - MaxOfflineCatchUp;

        foreach (var (businessId, state) in _businessStates)
        {
            if (!state.HasManager || state.Owned <= 0)
            {
                continue;
            }

            var definition = GameData.GetBusiness(businessId);
            var cycleSeconds = GameData.EffectiveCycleSeconds(definition, state.Owned, globalSync);

            if (cycleSeconds <= 0)
            {
                var completedInstant = Math.Floor(elapsedOffline.TotalSeconds * 10);
                if (completedInstant >= 1)
                {
                    var revenue = EffectiveRevenue(definition, state.Owned, boostMultiplierOverride: 1.0);
                    var gain = CreditRevenue(state, revenue, completedInstant);
                    _cash += gain;
                    _lifetimeEarnings += gain;
                    totalOfflineGain += gain;
                }

                continue;
            }

            var effectiveAnchor = state.CycleAnchorUtc is { } anchor && anchor > capFloor ? anchor : offlineFloor;
            var elapsed = (now - effectiveAnchor).TotalSeconds;
            var completed = Math.Floor(elapsed / cycleSeconds);
            if (completed >= 1)
            {
                var revenue = EffectiveRevenue(definition, state.Owned, boostMultiplierOverride: 1.0);
                var gain = CreditRevenue(state, revenue, completed);
                _cash += gain;
                _lifetimeEarnings += gain;
                totalOfflineGain += gain;
                state.CycleAnchorUtc = effectiveAnchor.AddSeconds(completed * cycleSeconds);
            }
        }

        LastOfflineEarnings = elapsedOffline >= MinOfflineDurationForPopup ? totalOfflineGain : BigInteger.Zero;
        LastOfflineDuration = elapsedOffline >= MinOfflineDurationForPopup ? elapsedOffline : TimeSpan.Zero;
        LastOfflineQuip = elapsedOffline >= MinOfflineDurationForPopup ? GameData.OliWelcomeBackLine(elapsedOffline) : null;
    }

    public void SetBuyMode(BuyMultiplier mode) => BuyMode = mode;

    public int GetOwned(string businessId) => _businessStates.TryGetValue(businessId, out var state) ? state.Owned : 0;

    public IReadOnlyList<BusinessSnapshot> GetSnapshots()
    {
        var snapshots = new List<BusinessSnapshot>(GameData.Businesses.Count);
        var now = DateTime.UtcNow;
        var previousOwned = true;
        var globalSync = GetGlobalSyncMultiplier();

        foreach (var definition in GameData.Businesses)
        {
            var state = _businessStates[definition.Id];
            var cycleSeconds = GameData.EffectiveCycleSeconds(definition, state.Owned, globalSync);
            var revenue = EffectiveRevenue(definition, state.Owned);

            double progress;

            if (state.Owned > 0)
            {
                if (cycleSeconds <= 0)
                {
                    progress = 1;
                }
                else if (state.HasManager)
                {
                    state.CycleAnchorUtc ??= now;
                    var elapsed = (now - state.CycleAnchorUtc.Value).TotalSeconds % cycleSeconds;
                    progress = elapsed / cycleSeconds;
                }
                else if (state.CycleAnchorUtc is null)
                {
                    progress = 0;
                }
                else
                {
                    var elapsed = (now - state.CycleAnchorUtc.Value).TotalSeconds;
                    progress = Math.Min(1, elapsed / cycleSeconds);
                }
            }
            else
            {
                progress = 0;
            }

            var quantity = ResolveQuantity(definition, state.Owned);
            var displayQuantity = Math.Max(1, quantity);
            var costBig = EconomyMath.CostForQuantity(definition.InitialCost, definition.Coefficient, state.Owned, displayQuantity);
            var managerCostBig = new BigInteger(Math.Ceiling(definition.ManagerCost));
            var unlocked = previousOwned;

            snapshots.Add(new BusinessSnapshot(
                definition,
                state.Owned,
                state.HasManager,
                progress,
                cycleSeconds,
                revenue,
                costBig,
                displayQuantity,
                unlocked,
                CanAffordBuy: unlocked && _cash >= costBig,
                CanAffordManager: unlocked && !state.HasManager && state.Owned > 0 && _cash >= managerCostBig,
                GameData.NextMilestone(definition.Id, state.Owned),
                state.CycleAnchorUtc));

            previousOwned = state.Owned > 0;
        }

        return snapshots;
    }

    public IReadOnlyList<(BusinessDefinition Business, int Owned, IReadOnlyList<(int Threshold, double Multiplier)> Tiers)> GetUnlockRows()
    {
        return GameData.Businesses
            .Where(b => GameData.UnlockProfitBonuses.ContainsKey(b.Id))
            .Select(b => (b, _businessStates.TryGetValue(b.Id, out var state) ? state.Owned : 0, GameData.UnlockProfitBonuses[b.Id]))
            .ToList();
    }

    private bool IsUpgradeUnlocked(UpgradeDefinition upgrade) =>
        upgrade.BusinessId is not null || _lifetimeEarnings >= UpgradeRequiredLifetimeById[upgrade.Id];

    public IReadOnlyList<(UpgradeDefinition Upgrade, bool Unlocked, bool CanAfford)> GetUpgradeRows() =>
        UpgradesByCost
            .Where(u => !_purchasedUpgrades.Contains(u.Id))
            .Select(u => (u, IsUpgradeUnlocked(u), _cash >= UpgradeCostById[u.Id]))
            .ToList();

    public IReadOnlyList<(PrestigeUpgradeDefinition Perk, bool Unlocked, bool Purchased)> GetPrestigeUpgradeRows() =>
        GameData.PrestigeUpgrades
            .Select(p => (p, p.PrestigeLevel <= _prestigeCount, _purchasedUpgrades.Contains(p.Upgrade.Id)))
            .ToList();

    public bool HasPrestigeUpgradeToClaim =>
        !_isGuest && !IsRetired && GameData.PrestigeUpgrades.Any(p => p.PrestigeLevel <= _prestigeCount && !_purchasedUpgrades.Contains(p.Upgrade.Id));

    public async Task<bool> BuyPrestigeUpgradeAsync(string upgradeId) =>
        await ClaimPrestigeUpgradesAsync(GameData.PrestigeUpgrades.Where(p => p.Upgrade.Id == upgradeId)) > 0;

    public async Task<int> BuyAllPrestigeUpgradesAsync() =>
        await ClaimPrestigeUpgradesAsync(GameData.PrestigeUpgrades);

    private async Task<int> ClaimPrestigeUpgradesAsync(IEnumerable<PrestigeUpgradeDefinition> candidates)
    {
        if (IsFrozen || _isGuest || IsRetired)
        {
            return 0;
        }

        var claimed = 0;
        foreach (var perk in candidates)
        {
            if (perk.PrestigeLevel <= _prestigeCount && _purchasedUpgrades.Add(perk.Upgrade.Id))
            {
                claimed++;
            }
        }

        if (claimed > 0)
        {
            RecomputeUpgradeEffectCache();
            _dirty = true;
            await PersistAsync();
        }

        return claimed;
    }

    public bool HasAffordableUpgrade =>
        UpgradesByCost.Any(u => !_purchasedUpgrades.Contains(u.Id) && IsUpgradeUnlocked(u) && _cash >= UpgradeCostById[u.Id]);

    public async Task TickAsync()
    {
        if (!IsAuthenticated || _sessionSuperseded || IsMaintenanceLocked || IsGameOver)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var changed = false;
        var globalSync = GetGlobalSyncMultiplier();

        foreach (var (businessId, state) in _businessStates)
        {
            if (state.Owned <= 0)
            {
                continue;
            }

            var definition = GameData.GetBusiness(businessId);
            var cycleSeconds = GameData.EffectiveCycleSeconds(definition, state.Owned, globalSync);

            if (state.HasManager)
            {
                if (cycleSeconds <= 0)
                {
                    var revenue = EffectiveRevenue(definition, state.Owned);
                    var instantGain = CreditRevenue(state, revenue);
                    _cash += instantGain;
                    _lifetimeEarnings += instantGain;
                    changed = true;
                    continue;
                }

                state.CycleAnchorUtc ??= now;
                var elapsed = (now - state.CycleAnchorUtc.Value).TotalSeconds;
                var completed = Math.Floor(elapsed / cycleSeconds);
                if (completed >= 1)
                {
                    var revenue = EffectiveRevenue(definition, state.Owned);
                    var gain = CreditRevenue(state, revenue, completed);
                    _cash += gain;
                    _lifetimeEarnings += gain;
                    state.CycleAnchorUtc = state.CycleAnchorUtc.Value.AddSeconds(completed * cycleSeconds);
                    changed = true;
                }
            }
            else if (state.CycleAnchorUtc is not null)
            {
                var elapsed = (now - state.CycleAnchorUtc.Value).TotalSeconds;
                if (cycleSeconds > 0 && elapsed >= cycleSeconds)
                {
                    var revenue = EffectiveRevenue(definition, state.Owned);
                    var gain = CreditRevenue(state, revenue);
                    _cash += gain;
                    _lifetimeEarnings += gain;
                    state.CycleAnchorUtc = null;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            _dirty = true;
        }

        if (_dirty && now - _lastSaveUtc >= AutoSaveInterval)
        {
            await PersistAsync();
        }
    }

    public async Task FlushAsync()
    {
        if (_dirty)
        {
            await PersistAsync();
        }
    }

    public async Task SetHiddenAsync(bool hidden)
    {
        if (_isGuest || _twitchUserId is null || _sessionSuperseded)
        {
            return;
        }

        _isHidden = hidden;
        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts.FindAsync(_twitchUserId);
        if (account is not null)
        {
            account.IsHidden = hidden;
            await db.SaveChangesAsync();
        }
    }

    private const int SnakeMaxPossibleScore = 15 * 15 - 3;
    private const double SnakeMinEventIntervalMs = 130;

    private int _snakeRunScore;
    private DateTime _snakeLastEventUtc = DateTime.MinValue;

    public void OnSnakeRunStarted()
    {
        if (_sessionSuperseded)
        {
            return;
        }

        _snakeRunScore = 0;
        _snakeLastEventUtc = DateTime.UtcNow;
    }

    public void OnSnakeFoodEaten()
    {
        if (_sessionSuperseded || _snakeRunScore >= SnakeMaxPossibleScore)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _snakeLastEventUtc).TotalMilliseconds < SnakeMinEventIntervalMs)
        {
            return;
        }

        _snakeLastEventUtc = now;
        _snakeRunScore++;
    }

    public async Task OnSnakeGameOverAsync()
    {
        if (_isGuest || _twitchUserId is null || _sessionSuperseded || _snakeRunScore <= _snakeHighScore)
        {
            return;
        }

        _snakeHighScore = _snakeRunScore;
        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts.FindAsync(_twitchUserId);
        if (account is not null && _snakeRunScore > account.SnakeHighScore)
        {
            account.SnakeHighScore = _snakeRunScore;
            await db.SaveChangesAsync();
        }
    }

    private const double TimberMinEventIntervalMs = 60;
    private const int TimberMaxPossibleScore = 10000;

    private int _timberRunScore;
    private DateTime _timberLastEventUtc = DateTime.MinValue;

    public void OnTimberRunStarted()
    {
        if (_sessionSuperseded)
        {
            return;
        }

        _timberRunScore = 0;
        _timberLastEventUtc = DateTime.MinValue;
    }

    public void OnTimberChopScored()
    {
        if (_sessionSuperseded || _timberRunScore >= TimberMaxPossibleScore)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _timberLastEventUtc).TotalMilliseconds < TimberMinEventIntervalMs)
        {
            return;
        }

        _timberLastEventUtc = now;
        _timberRunScore++;
    }

    public async Task OnTimberGameOverAsync()
    {
        if (_isGuest || _twitchUserId is null || _sessionSuperseded || _timberRunScore <= _timberHighScore)
        {
            return;
        }

        _timberHighScore = _timberRunScore;
        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts.FindAsync(_twitchUserId);
        if (account is not null && _timberRunScore > account.TimberHighScore)
        {
            account.TimberHighScore = _timberRunScore;
            await db.SaveChangesAsync();
        }
    }

    public async Task BuyBusinessAsync(string businessId)
    {
        if (IsFrozen)
        {
            return;
        }

        var definition = GameData.GetBusiness(businessId);
        var state = _businessStates[businessId];
        var quantity = ResolveQuantity(definition, state.Owned);
        if (quantity <= 0 || (long)state.Owned + quantity > int.MaxValue)
        {
            return;
        }

        var cost = EconomyMath.CostForQuantity(definition.InitialCost, definition.Coefficient, state.Owned, quantity);
        if (_cash < cost)
        {
            return;
        }

        _cash -= cost;
        state.Owned += quantity;
        _dirty = true;
        await PersistAsync();
    }

    public async Task BuyManagerAsync(string businessId)
    {
        if (IsFrozen)
        {
            return;
        }

        var definition = GameData.GetBusiness(businessId);
        var state = _businessStates[businessId];
        if (state.HasManager || state.Owned <= 0)
        {
            return;
        }

        var cost = new BigInteger(Math.Ceiling(definition.ManagerCost));
        if (_cash < cost)
        {
            return;
        }

        _cash -= cost;
        state.HasManager = true;
        state.CycleAnchorUtc = DateTime.UtcNow;
        _dirty = true;
        await PersistAsync();
    }

    public async Task<int> BuyAllAffordableManagersAsync()
    {
        if (IsFrozen)
        {
            return 0;
        }

        var boughtCount = 0;
        while (true)
        {
            var next = GetSnapshots()
                .Where(s => s.CanAffordManager)
                .OrderBy(s => s.Definition.ManagerCost)
                .Select(s => s.Definition)
                .FirstOrDefault();

            if (next is null)
            {
                break;
            }

            var state = _businessStates[next.Id];
            _cash -= new BigInteger(Math.Ceiling(next.ManagerCost));
            state.HasManager = true;
            state.CycleAnchorUtc = DateTime.UtcNow;
            boughtCount++;
        }

        if (boughtCount > 0)
        {
            _dirty = true;
            await PersistAsync();
        }

        return boughtCount;
    }

    public BigInteger? ManualClick(string businessId)
    {
        if (IsFrozen)
        {
            return null;
        }

        var state = _businessStates[businessId];
        if (state.HasManager || state.Owned <= 0)
        {
            return null;
        }

        var definition = GameData.GetBusiness(businessId);
        var globalSync = GetGlobalSyncMultiplier();
        var cycleSeconds = GameData.EffectiveCycleSeconds(definition, state.Owned, globalSync);

        if (cycleSeconds <= 0)
        {
            var revenue = EffectiveRevenue(definition, state.Owned);
            var gain = CreditRevenue(state, revenue);
            _cash += gain;
            _lifetimeEarnings += gain;
            _dirty = true;
            return gain;
        }

        if (state.CycleAnchorUtc is null)
        {
            var startUtc = DateTime.UtcNow;
            state.CycleAnchorUtc = startUtc;
            _dirty = true;
            LastManualClickToken = (businessId, ProtectManualClickToken(businessId, startUtc));
        }

        return null;
    }

    public (string BusinessId, string Token)? LastManualClickToken { get; private set; }

    private string ProtectManualClickToken(string businessId, DateTime startUtc) =>
        _manualClickProtector.Protect($"{IdentityKey}|{businessId}|{startUtc.Ticks}");

    public void RestoreManualChargeFromToken(string businessId, string token)
    {
        if (_sessionSuperseded)
        {
            return;
        }

        if (!_businessStates.TryGetValue(businessId, out var state) || state.HasManager || state.Owned <= 0 || state.CycleAnchorUtc is not null)
        {
            return;
        }

        string payload;
        try
        {
            payload = _manualClickProtector.Unprotect(token);
        }
        catch (CryptographicException)
        {
            return;
        }

        var parts = payload.Split('|');
        if (parts.Length != 3 || parts[0] != IdentityKey || parts[1] != businessId || !long.TryParse(parts[2], out var ticks))
        {
            return;
        }

        var startUtc = new DateTime(ticks, DateTimeKind.Utc);
        var now = DateTime.UtcNow;
        var definition = GameData.GetBusiness(businessId);
        var cycleSeconds = GameData.EffectiveCycleSeconds(definition, state.Owned, GetGlobalSyncMultiplier());

        if (startUtc > now || cycleSeconds <= 0 || (now - startUtc) > TimeSpan.FromSeconds(cycleSeconds))
        {
            return;
        }

        state.CycleAnchorUtc = startUtc;
        _dirty = true;
    }

    public async Task BuyUpgradeAsync(string upgradeId)
    {
        if (IsFrozen)
        {
            return;
        }

        var upgrade = GameData.Upgrades.FirstOrDefault(u => u.Id == upgradeId);
        if (upgrade is null || _purchasedUpgrades.Contains(upgradeId))
        {
            return;
        }

        if (!IsUpgradeUnlocked(upgrade))
        {
            return;
        }

        var cost = UpgradeCostById[upgrade.Id];
        if (_cash < cost)
        {
            return;
        }

        _cash -= cost;
        _purchasedUpgrades.Add(upgradeId);
        RecomputeUpgradeEffectCache();
        _dirty = true;
        await PersistAsync();
    }

    public async Task<int> BuyAllAffordableUpgradesAsync()
    {
        if (IsFrozen)
        {
            return 0;
        }

        var boughtCount = 0;
        foreach (var upgrade in UpgradesByCost)
        {
            if (_purchasedUpgrades.Contains(upgrade.Id) || !IsUpgradeUnlocked(upgrade))
            {
                continue;
            }

            var cost = UpgradeCostById[upgrade.Id];
            if (_cash < cost)
            {
                continue;
            }

            _cash -= cost;
            _purchasedUpgrades.Add(upgrade.Id);
            boughtCount++;
        }

        if (boughtCount > 0)
        {
            RecomputeUpgradeEffectCache();
            _dirty = true;
            await PersistAsync();
        }

        return boughtCount;
    }

    public async Task<BigInteger> ClaimAngelsAsync()
    {
        if (IsFrozen)
        {
            return BigInteger.Zero;
        }

        var cashBefore = _cash;
        var lifetimeEarningsBefore = _lifetimeEarnings;
        var earningsAtLastResetBefore = _earningsAtLastReset;
        var angelCountBefore = _angelCount;
        var businessLevelsBefore = _businessStates
            .Where(kv => kv.Value.Owned > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value.Owned);

        var gained = AngelPreview;
        var oliModifierPercentApplied = _oliChatRound >= 3 ? _oliModifierPercent : 0.0;
        _angelCount += gained;
        _earningsAtLastReset = _lifetimeEarnings;
        _cash = 0;

        foreach (var state in _businessStates.Values)
        {
            state.Owned = 0;
            state.HasManager = false;
            state.CycleAnchorUtc = null;
        }

        if (_businessStates.TryGetValue(GameData.Businesses[0].Id, out var firstState))
        {
            firstState.Owned = 1;
        }

        _purchasedUpgrades.Clear();
        _ownedAngelUpgrades.Clear();
        RecomputeUpgradeEffectCache();
        _oliChatRound = 0;
        _oliModifierPercent = 0;
        _resetCount++;
        _dirty = true;
        await PersistAsync();

        if (_twitchUserId is not null)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                db.ResetLogEntries.Add(new ResetLogEntry
                {
                    TwitchUserId = _twitchUserId,
                    DisplayName = DisplayName ?? _twitchUserId,
                    TimestampUtc = DateTime.UtcNow,
                    CashBefore = cashBefore.ToString(),
                    LifetimeEarningsBefore = lifetimeEarningsBefore.ToString(),
                    EarningsAtLastResetBefore = earningsAtLastResetBefore.ToString(),
                    AngelCountBefore = angelCountBefore.ToString(),
                    AngelsGained = gained.ToString(),
                    OliModifierPercentApplied = oliModifierPercentApplied,
                    AngelCountAfter = _angelCount.ToString(),
                    BusinessLevelsBeforeJson = JsonSerializer.Serialize(businessLevelsBefore),
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Writing the angel claim log entry for {TwitchUserId} failed.", _twitchUserId);
            }
        }

        return gained;
    }

    public async Task<bool> PrestigeAsync()
    {
        if (IsFrozen)
        {
            return false;
        }

        if (!CanPrestige)
        {
            return false;
        }

        var cashBefore = _cash;
        var lifetimeEarningsBefore = _lifetimeEarnings;
        var earningsAtLastResetBefore = _earningsAtLastReset;
        var angelCountBefore = _angelCount;
        var businessLevelsBefore = _businessStates
            .Where(kv => kv.Value.Owned > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value.Owned);

        _cash = 0;
        _lifetimeEarnings = 0;
        _earningsAtLastReset = 0;
        _angelCount = 0;

        foreach (var state in _businessStates.Values)
        {
            state.Owned = 0;
            state.HasManager = false;
            state.CycleAnchorUtc = null;
        }

        if (_businessStates.TryGetValue(GameData.Businesses[0].Id, out var firstState))
        {
            firstState.Owned = 1;
        }

        _purchasedUpgrades.Clear();
        _ownedAngelUpgrades.Clear();
        RecomputeUpgradeEffectCache();
        _oliChatRound = 0;
        _oliModifierPercent = 0;
        _prestigeCount++;
        _lastPrestigeUtc = DateTime.UtcNow;
        _dirty = true;
        await PersistAsync();

        if (_twitchUserId is not null && !_sessionSuperseded)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                db.ResetLogEntries.Add(new ResetLogEntry
                {
                    TwitchUserId = _twitchUserId,
                    DisplayName = DisplayName ?? _twitchUserId,
                    TimestampUtc = DateTime.UtcNow,
                    CashBefore = cashBefore.ToString(),
                    LifetimeEarningsBefore = lifetimeEarningsBefore.ToString(),
                    EarningsAtLastResetBefore = earningsAtLastResetBefore.ToString(),
                    AngelCountBefore = angelCountBefore.ToString(),
                    AngelsGained = "0",
                    OliModifierPercentApplied = 0,
                    AngelCountAfter = "0",
                    BusinessLevelsBeforeJson = JsonSerializer.Serialize(businessLevelsBefore),
                    IsPrestige = true,
                    PrestigeCountAfter = _prestigeCount,
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Writing the prestige log entry for {TwitchUserId} failed.", _twitchUserId);
            }
        }

        return true;
    }

    public async Task AdminKillSessionAsync()
    {
        _sessionSuperseded = true;
        SessionSuperseded?.Invoke();

        await _persistLock.WaitAsync();
        _persistLock.Release();

        if (_twitchUserId is not null)
        {
            sessionRegistry.UnregisterSession(_twitchUserId, _sessionId);
            liveDirectory.Unregister(_twitchUserId, this);
        }
    }

    public async Task<(string Reply, bool IsFinal)> SendOliChatMessageAsync()
    {
        if (IsFrozen)
        {
            return ("", false);
        }

        if (_oliChatRound >= 3)
        {
            var lockedPctText = (_oliModifierPercent >= 0 ? "+" : "-") + Math.Round(Math.Abs(_oliModifierPercent) * 100) + "%";
            var lockedReply = string.Format(
                GameData.OliDealLockedReplies[Random.Shared.Next(GameData.OliDealLockedReplies.Length)],
                lockedPctText);
            return (lockedReply, true);
        }

        if (AngelPreview <= 1)
        {
            return ("Du brauchst mindestens 2 ausstehende Investor-Anteile, bevor Oli überhaupt mit dir redet.", false);
        }

        _oliChatRound++;
        string reply;
        var isFinal = _oliChatRound >= 3;

        if (_oliChatRound == 1)
        {
            reply = GameData.OliStallReplies1[Random.Shared.Next(GameData.OliStallReplies1.Length)];
        }
        else if (_oliChatRound == 2)
        {
            reply = GameData.OliStallReplies2[Random.Shared.Next(GameData.OliStallReplies2.Length)];
        }
        else
        {
            var annoyed = Random.Shared.NextDouble() < 0.30;
            _oliModifierPercent = Math.Round(annoyed ? -Random.Shared.NextDouble() * 0.15 : Random.Shared.NextDouble() * 0.25, 2);

            var pctText = (annoyed ? "-" : "+") + Math.Round(Math.Abs(_oliModifierPercent) * 100) + "%";
            var pool = annoyed ? GameData.OliAnnoyedReplies : GameData.OliNiceReplies;
            reply = string.Format(pool[Random.Shared.Next(pool.Length)], pctText);
        }

        _dirty = true;
        await PersistAsync();
        return (reply, isFinal);
    }

    public string ProtectChatHistory(string json) =>
        _chatHistoryProtector.Protect($"{IdentityKey}|{json}");

    public string? UnprotectChatHistory(string token)
    {
        string payload;
        try
        {
            payload = _chatHistoryProtector.Unprotect(token);
        }
        catch (CryptographicException)
        {
            return null;
        }

        var separatorIndex = payload.IndexOf('|');
        if (separatorIndex < 0 || payload[..separatorIndex] != IdentityKey)
        {
            return null;
        }

        return payload[(separatorIndex + 1)..];
    }

    private BigValue EffectiveRevenue(BusinessDefinition definition, int owned, double? boostMultiplierOverride = null)
    {
        var stihlOwned = _businessStates.TryGetValue("ShakerStihl", out var stihlState) ? stihlState.Owned : 0;

        var cashSide = BigValue.FromDouble(definition.InitialRevenue * owned)
            .Multiply(_cashBusinessProfitMultipliers.TryGetValue(definition.Id, out var upgradeMult) ? upgradeMult : 1.0)
            .Multiply(GameData.UnlockProfitMultiplier(definition.Id, owned))
            .Multiply(GameData.NewspaperCrossMultiplier(definition.Id, stihlOwned))
            .Multiply(_cashAllProfitMultiplier)
            .Multiply(GetGlobalSyncProfitMultiplier());

        var angelSide = AngelProfitMultiplier
            .Multiply(_angelAllProfitMultiplier)
            .Multiply(_angelBusinessProfitMultiplier.TryGetValue(definition.Id, out var angelBizMult) ? angelBizMult : 1.0);

        return (cashSide * angelSide).Multiply(boostMultiplierOverride ?? globalBoost.CurrentMultiplier);
    }

    private void RecomputeUpgradeEffectCache()
    {
        _cashAllProfitMultiplier = GameData.AllProfitMultiplier(_purchasedUpgrades, _prestigeCount);
        _cashBusinessProfitMultipliers = GameData.Businesses.ToDictionary(b => b.Id, b => GameData.BusinessUpgradeMultiplier(b.Id, _purchasedUpgrades));
        _angelAllProfitMultiplier = 1.0;
        _angelEffectivenessBonus = 0;
        _angelBusinessProfitMultiplier = GameData.Businesses.ToDictionary(b => b.Id, _ => 1.0);

        foreach (var id in _ownedAngelUpgrades)
        {
            var definition = AngelUpgradeData.GetById(id);
            if (definition is null)
            {
                continue;
            }

            switch (definition.Type)
            {
                case AngelUpgradeEffectType.AllProfitMultiplier:
                    _angelAllProfitMultiplier *= definition.Value;
                    break;
                case AngelUpgradeEffectType.BusinessProfitMultiplier when definition.BusinessId is not null:
                    _angelBusinessProfitMultiplier[definition.BusinessId] *= definition.Value;
                    break;
                case AngelUpgradeEffectType.AngelEffectivenessBonus:
                    _angelEffectivenessBonus += definition.Value;
                    break;
            }
        }

        _angelEffectivenessBonus += GameData.CashAngelEffectivenessBonus(_purchasedUpgrades);
    }

    public IReadOnlyList<(AngelUpgradeDefinition Upgrade, bool CanAfford)> GetAngelUpgradeRows() =>
        AngelUpgradesByCost
            .Where(u => !_ownedAngelUpgrades.Contains(u.Id))
            .Select(u => (u, _angelCount >= u.Cost))
            .ToList();

    private IEnumerable<AngelUpgradeDefinition> AngelUpgradeCandidatesByCost =>
        AngelUpgradesByCost.Where(u => !_ownedAngelUpgrades.Contains(u.Id));

    public bool HasAngelUpgradeWithinBudget =>
        AngelUpgradeCandidatesByCost.FirstOrDefault() is { } cheapest && cheapest.Cost <= _angelCount / 100;

    public async Task<int> BuyAffordableAngelUpgradesWithinBudgetAsync()
    {
        if (IsFrozen)
        {
            return 0;
        }

        var budget = _angelCount / 100;
        var spent = BigInteger.Zero;
        var boughtCount = 0;

        foreach (var upgrade in AngelUpgradeCandidatesByCost)
        {
            if (spent + upgrade.Cost > budget)
            {
                break;
            }

            _angelCount -= upgrade.Cost;
            spent += upgrade.Cost;
            _ownedAngelUpgrades.Add(upgrade.Id);

            if (upgrade is { Type: AngelUpgradeEffectType.BusinessInstantCount, BusinessId: not null } &&
                _businessStates.TryGetValue(upgrade.BusinessId, out var state))
            {
                state.Owned += (int)upgrade.Value;
            }

            boughtCount++;
        }

        if (boughtCount > 0)
        {
            RecomputeUpgradeEffectCache();
            _dirty = true;
            await PersistAsync();
        }

        return boughtCount;
    }

    public async Task BuyAngelUpgradeAsync(string angelUpgradeId)
    {
        if (IsFrozen)
        {
            return;
        }

        var upgrade = AngelUpgradeData.GetById(angelUpgradeId);
        if (upgrade is null || _ownedAngelUpgrades.Contains(angelUpgradeId) || _angelCount < upgrade.Cost)
        {
            return;
        }

        _angelCount -= upgrade.Cost;
        _ownedAngelUpgrades.Add(angelUpgradeId);

        if (upgrade is { Type: AngelUpgradeEffectType.BusinessInstantCount, BusinessId: not null } &&
            _businessStates.TryGetValue(upgrade.BusinessId, out var state))
        {
            state.Owned += (int)upgrade.Value;
        }

        RecomputeUpgradeEffectCache();
        _dirty = true;
        await PersistAsync();
    }

    private double GetGlobalSyncMultiplier() =>
        GameData.GlobalSyncSpeedMultiplier(_businessStates.ToDictionary(kv => kv.Key, kv => kv.Value.Owned));

    private double GetGlobalSyncProfitMultiplier()
    {
        if (_businessStates.Count < GameData.Businesses.Count)
        {
            return GameData.GlobalSyncProfitMultiplierForMin(0);
        }

        var minOwned = int.MaxValue;
        foreach (var state in _businessStates.Values)
        {
            minOwned = Math.Min(minOwned, state.Owned);
        }

        return GameData.GlobalSyncProfitMultiplierForMin(minOwned);
    }

    public double UnlockProgressPercent =>
        GameData.UnlockProgressPercent(_businessStates.ToDictionary(kv => kv.Key, kv => kv.Value.Owned));

    public bool IsGameFullyUnlocked
    {
        get
        {
            var (achieved, total) = GameData.UnlockProgress(_businessStates.ToDictionary(kv => kv.Key, kv => kv.Value.Owned));
            return total > 0 && achieved == total;
        }
    }

    public (int MinOwned, double SpeedMultiplier, double ProfitMultiplier, int? NextThreshold) GetCapitalistStatus()
    {
        var owned = _businessStates.ToDictionary(kv => kv.Key, kv => kv.Value.Owned);
        var minOwned = owned.Count == 0 ? 0 : owned.Values.Min();
        var nextThreshold = GameData.CapitalistSpeedThresholds
            .Concat(GameData.CapitalistProfitThresholds)
            .Where(t => t > minOwned)
            .OrderBy(t => t)
            .Cast<int?>()
            .FirstOrDefault();

        return (minOwned, GameData.GlobalSyncSpeedMultiplier(owned), GameData.GlobalSyncProfitMultiplier(owned), nextThreshold);
    }

    private int ResolveQuantity(BusinessDefinition definition, int owned)
    {
        return BuyMode switch
        {
            BuyMultiplier.X1 => 1,
            BuyMultiplier.X10 => 10,
            BuyMultiplier.X100 => 100,
            BuyMultiplier.Next => Math.Max(1, (GameData.NextMilestone(definition.Id, owned) ?? owned + 100) - owned),
            BuyMultiplier.Max => EconomyMath.MaxAffordable(definition.InitialCost, definition.Coefficient, owned, _cash),
            _ => 1,
        };
    }

    private async Task PersistAsync()
    {
        if (_sessionSuperseded)
        {
            logger.LogWarning("Skipped persisting state for {TwitchUserId} because session {SessionId} is superseded.", _twitchUserId, _sessionId);
            return;
        }

        if (!_isGuest && _twitchUserId is null)
        {
            return;
        }

        await _persistLock.WaitAsync();
        try
        {
            if (_isGuest)
            {
                await PersistGuestStateAsync();
            }
            else
            {
                await PersistDbStateAsync();
            }
        }
        finally
        {
            _persistLock.Release();
        }
    }

    private async Task PersistDbStateAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.PlayerAccounts
            .AsSplitQuery()
            .Include(a => a.Businesses)
            .Include(a => a.Upgrades)
            .Include(a => a.AngelUpgrades)
            .FirstOrDefaultAsync(a => a.TwitchUserId == _twitchUserId);

        if (account is null)
        {
            return;
        }

        account.Cash = _cash;
        account.LifetimeEarnings = _lifetimeEarnings;
        account.EarningsAtLastReset = _earningsAtLastReset;
        account.AngelCount = _angelCount;
        account.OliChatRound = _oliChatRound;
        account.OliModifierPercent = _oliModifierPercent;
        account.ResetCount = _resetCount;
        account.PrestigeCount = _prestigeCount;
        account.LastPrestigeUtc = _lastPrestigeUtc;
        account.LastOnlineUtc = DateTime.UtcNow;

        foreach (var row in account.Businesses)
        {
            if (_businessStates.TryGetValue(row.BusinessId, out var state))
            {
                row.Owned = state.Owned;
                row.HasManager = state.HasManager;
                row.CycleAnchorUtc = state.CycleAnchorUtc;
            }
        }

        var existingUpgradeIds = account.Upgrades.Select(u => u.UpgradeId).ToHashSet();

        foreach (var upgradeId in _purchasedUpgrades.Except(existingUpgradeIds))
        {
            db.PlayerUpgrades.Add(new PlayerUpgrade { TwitchUserId = _twitchUserId!, UpgradeId = upgradeId });
        }

        foreach (var stale in account.Upgrades.Where(u => !_purchasedUpgrades.Contains(u.UpgradeId)).ToList())
        {
            db.PlayerUpgrades.Remove(stale);
        }

        var existingAngelUpgradeIds = account.AngelUpgrades.Select(u => u.AngelUpgradeId).ToHashSet();

        foreach (var angelUpgradeId in _ownedAngelUpgrades.Except(existingAngelUpgradeIds))
        {
            db.PlayerAngelUpgrades.Add(new PlayerAngelUpgrade { TwitchUserId = _twitchUserId!, AngelUpgradeId = angelUpgradeId });
        }

        foreach (var stale in account.AngelUpgrades.Where(u => !_ownedAngelUpgrades.Contains(u.AngelUpgradeId)).ToList())
        {
            db.PlayerAngelUpgrades.Remove(stale);
        }

        if (_sessionSuperseded)
        {
            return;
        }

        await db.SaveChangesAsync();
        _dirty = false;
        _lastSaveUtc = DateTime.UtcNow;
    }

    private async Task PersistGuestStateAsync()
    {
        var dto = new GuestStateDto(
            _cash.ToString(),
            _lifetimeEarnings.ToString(),
            _angelCount.ToString(),
            _oliChatRound,
            _oliModifierPercent,
            DateTime.UtcNow,
            _purchasedUpgrades.ToList(),
            _businessStates.Select(kv => new GuestBusinessDto(kv.Key, kv.Value.Owned, kv.Value.HasManager, kv.Value.CycleAnchorUtc)).ToList(),
            _earningsAtLastReset.ToString(),
            _ownedAngelUpgrades.ToList());

        var json = JsonSerializer.Serialize(dto);
        var encrypted = _guestStateProtector.Protect(json);

        try
        {
            await jsRuntime.InvokeVoidAsync("shakerEffects.storeGuestState", encrypted);
        }
        catch (JSDisconnectedException)
        {
        }

        _dirty = false;
        _lastSaveUtc = DateTime.UtcNow;
    }

    private void OnRegistrySessionSuperseded(string twitchUserId, Guid supersededSessionId)
    {
        if (twitchUserId == _twitchUserId && supersededSessionId == _sessionId)
        {
            _sessionSuperseded = true;
            logger.LogWarning("Session {SessionId} for {TwitchUserId} was superseded by a newer session.", supersededSessionId, twitchUserId);
            SessionSuperseded?.Invoke();
        }
    }

    public void OnConnectionDown()
    {
        if (_twitchUserId is not null)
        {
            liveDirectory.Unregister(_twitchUserId, this);
        }
    }

    public void OnConnectionUp()
    {
        if (_twitchUserId is not null && !_sessionSuperseded)
        {
            sessionRegistry.MarkOnline(_twitchUserId, _sessionId);
            SyncMaintenanceLock();
            liveDirectory.Register(_twitchUserId, this);
        }
    }

    private void SyncMaintenanceLock()
    {
        if (_twitchUserId is not null)
        {
            sessionRegistry.SetLocked(_twitchUserId, IsMaintenanceLocked);
        }
    }

    public void Dispose()
    {
        sessionRegistry.SessionSuperseded -= OnRegistrySessionSuperseded;
        maintenanceMode.OnChanged -= SyncMaintenanceLock;

        if (_twitchUserId is not null)
        {
            sessionRegistry.UnregisterSession(_twitchUserId, _sessionId);
            liveDirectory.Unregister(_twitchUserId, this);
        }
    }
}
