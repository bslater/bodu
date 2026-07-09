// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Totp.VerifyCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public static partial class Totp
{
    /// <summary>
    /// Verifies a candidate code against the RFC 6238 TOTP codes within a window of time steps around the specified
    /// timestamp, counting time steps from the Unix epoch.
    /// </summary>
    /// <param name="secret">The shared secret key, as raw bytes.</param>
    /// <param name="code">The candidate code supplied by the user.</param>
    /// <param name="timestamp">The instant at which the code is being verified.</param>
    /// <param name="window">
    /// The number of time steps on each side of the current step to also accept, tolerating clock drift.
    /// </param>
    /// <param name="digits">The expected number of decimal digits.</param>
    /// <param name="periodSeconds">The time-step length, in seconds.</param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="code" /> matches any accepted time step; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than 6 or greater than 8, <paramref name="window" /> is negative,
    /// <paramref name="periodSeconds" /> is less than 1, <paramref name="algorithm" /> is not a defined
    /// <see cref="OtpHashAlgorithm" /> value, or <paramref name="timestamp" /> is earlier than the Unix epoch.
    /// </exception>
    public static bool VerifyCode(ReadOnlySpan<byte> secret, ReadOnlySpan<char> code, DateTimeOffset timestamp, int window = 1, int digits = 6, int periodSeconds = DefaultPeriodSeconds, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1) =>
        VerifyCode(secret, code, timestamp, window, out _, digits, periodSeconds, algorithm);

    /// <summary>
    /// Verifies a candidate code against the RFC 6238 TOTP codes within a window of time steps and reports which step
    /// matched, counting time steps from the Unix epoch.
    /// </summary>
    /// <param name="secret">The shared secret key, as raw bytes.</param>
    /// <param name="code">The candidate code supplied by the user.</param>
    /// <param name="timestamp">The instant at which the code is being verified.</param>
    /// <param name="window">The number of time steps on each side of the current step to also accept.</param>
    /// <param name="matchedStepOffset">
    /// When this method returns <see langword="true" />, the signed step offset that matched (<c>0</c> is the current
    /// step, negative is earlier, positive is later); otherwise, <c>0</c>. Useful for detecting persistent client clock
    /// drift.
    /// </param>
    /// <param name="digits">The expected number of decimal digits.</param>
    /// <param name="periodSeconds">The time-step length, in seconds.</param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="code" /> matches any accepted time step; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than 6 or greater than 8, <paramref name="window" /> is negative,
    /// <paramref name="periodSeconds" /> is less than 1, <paramref name="algorithm" /> is not a defined
    /// <see cref="OtpHashAlgorithm" /> value, or <paramref name="timestamp" /> is earlier than the Unix epoch.
    /// </exception>
    /// <remarks>
    /// Every step in the window is scanned; matching does not short-circuit. A wider <paramref name="window" />
    /// tolerates more drift but admits more valid codes at once, so keep it small (the default accepts the current step
    /// and one on each side).
    /// </remarks>
    public static bool VerifyCode(ReadOnlySpan<byte> secret, ReadOnlySpan<char> code, DateTimeOffset timestamp, int window, out int matchedStepOffset, int digits = 6, int periodSeconds = DefaultPeriodSeconds, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1)
    {
        ThrowHelper.ThrowIfOutOfRange(digits, Hotp.MinDigits, Hotp.MaxDigits);
        ThrowHelper.ThrowIfNegative(window);
        ThrowHelper.ThrowIfLessThan(periodSeconds, 1);
        ThrowHelper.ThrowIfEnumValueIsUndefined(algorithm);

        long counter = ComputeCounter(timestamp, DateTimeOffset.UnixEpoch, periodSeconds);

        matchedStepOffset = 0;
        bool matched = false;

        for (int i = -window; i <= window; i++)
        {
            long candidate = counter + i;
            if (candidate < 0)
                continue;

            if (Hotp.VerifyCore(secret, code, candidate, digits, algorithm) && !matched)
            {
                matchedStepOffset = i;
                matched = true;
            }
        }

        return matched;
    }
}
