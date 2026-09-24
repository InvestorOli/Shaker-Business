using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ShakerBusiness.Services;

public enum SlotWalletState
{
    Missing,
    Valid,
    Tampered,
}

public sealed class SlotWalletService(IDataProtectionProvider dataProtectionProvider)
{
    public const int StartingBalance = 1000;
    public const int MaxBalance = 1_000_000_000;

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Shakercasino.SlotWallet.v1");

    private sealed record WalletDto(string Owner, int Balance);

    public string Protect(string ownerId, int balance) =>
        _protector.Protect(JsonSerializer.Serialize(new WalletDto(ownerId, Math.Clamp(balance, 0, MaxBalance))));

    public (SlotWalletState State, int Balance) Read(string ownerId, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return (SlotWalletState.Missing, StartingBalance);
        }

        try
        {
            var wallet = JsonSerializer.Deserialize<WalletDto>(_protector.Unprotect(token));
            if (wallet is null || wallet.Owner != ownerId || wallet.Balance < 0 || wallet.Balance > MaxBalance)
            {
                return (SlotWalletState.Tampered, 0);
            }

            return (SlotWalletState.Valid, wallet.Balance);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            return (SlotWalletState.Tampered, 0);
        }
    }
}
