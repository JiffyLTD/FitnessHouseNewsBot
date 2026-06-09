using System.Security.Cryptography;
using System.Text;
using FitnessHouseNewsBot.Options;
using Microsoft.Extensions.Options;

namespace FitnessHouseNewsBot.Services;

public class UiLockService
{
    private readonly UiLockOptions _options;

    public UiLockService(
        IOptions<UiLockOptions> options)
    {
        _options = options.Value;
    }

    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var configuredPassword = Encoding.UTF8.GetBytes(_options.Password);
        var providedPassword = Encoding.UTF8.GetBytes(password);

        return CryptographicOperations.FixedTimeEquals(
            configuredPassword,
            providedPassword);
    }
}
