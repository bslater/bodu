// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Totp.GenerateCode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public static partial class Totp
{
    /// <summary>
    /// Generates the RFC 6238 TOTP code for the specified secret and timestamp, counting time steps from the Unix
    /// epoch.
    /// </summary>
    /// <param name="secret">The shared secret key, as raw bytes.</param>
    /// <param name="timestamp">The instant for which to generate the code.</param>
    /// <param name="digits">The number of decimal digits in the returned code.</param>
    /// <param name="periodSeconds">The time-step length, in seconds.</param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <returns>The zero-padded decimal code, exactly <paramref name="digits" /> characters long.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than 6 or greater than 8, <paramref name="periodSeconds" /> is less than 1,
    /// <paramref name="algorithm" /> is not a defined <see cref="OtpHashAlgorithm" /> value, or
    /// <paramref name="timestamp" /> is earlier than the Unix epoch.
    /// </exception>
    public static string GenerateCode(ReadOnlySpan<byte> secret, DateTimeOffset timestamp, int digits = 6, int periodSeconds = DefaultPeriodSeconds, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1) =>
        GenerateCode(secret, timestamp, DateTimeOffset.UnixEpoch, digits, periodSeconds, algorithm);

    /// <summary>
    /// Generates the RFC 6238 TOTP code for the specified secret and timestamp, counting time steps from an explicit
    /// epoch.
    /// </summary>
    /// <param name="secret">The shared secret key, as raw bytes.</param>
    /// <param name="timestamp">The instant for which to generate the code.</param>
    /// <param name="epoch">The epoch from which time steps are counted (RFC 6238 <c>T0</c>).</param>
    /// <param name="digits">The number of decimal digits in the returned code.</param>
    /// <param name="periodSeconds">The time-step length, in seconds.</param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <returns>The zero-padded decimal code, exactly <paramref name="digits" /> characters long.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than 6 or greater than 8, <paramref name="periodSeconds" /> is less than 1,
    /// <paramref name="algorithm" /> is not a defined <see cref="OtpHashAlgorithm" /> value, or
    /// <paramref name="timestamp" /> is earlier than <paramref name="epoch" />.
    /// </exception>
    public static string GenerateCode(ReadOnlySpan<byte> secret, DateTimeOffset timestamp, DateTimeOffset epoch, int digits, int periodSeconds, OtpHashAlgorithm algorithm)
    {
        ThrowHelper.ThrowIfOutOfRange(digits, Hotp.MinDigits, Hotp.MaxDigits);
        ThrowHelper.ThrowIfLessThan(periodSeconds, 1);
        ThrowHelper.ThrowIfEnumValueIsUndefined(algorithm);

        long counter = ComputeCounter(timestamp, epoch, periodSeconds);

        return Hotp.GenerateCode(secret, counter, digits, algorithm);
    }
}
