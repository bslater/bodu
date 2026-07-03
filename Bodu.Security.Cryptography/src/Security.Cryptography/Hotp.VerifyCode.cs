// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hotp.VerifyCode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public static partial class Hotp
{
    /// <summary>
    /// Verifies a candidate code against the RFC 4226 HOTP code for the specified secret and counter.
    /// </summary>
    /// <param name="secret">The shared secret key, as raw bytes.</param>
    /// <param name="code">The candidate code supplied by the user.</param>
    /// <param name="counter">The counter value to test.</param>
    /// <param name="digits">The expected number of decimal digits.</param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <returns><see langword="true" /> if <paramref name="code" /> matches the code computed for <paramref name="counter" />; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than 6 or greater than 8, or <paramref name="algorithm" /> is not a defined
    /// <see cref="OtpHashAlgorithm" /> value.
    /// </exception>
    /// <remarks>
    /// The comparison is performed in constant time. A <paramref name="code" /> whose length differs from
    /// <paramref name="digits" /> returns <see langword="false" /> immediately, since the length is not secret.
    /// </remarks>
    public static bool VerifyCode(ReadOnlySpan<byte> secret, ReadOnlySpan<char> code, long counter, int digits = 6, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1)
    {
        ThrowHelper.ThrowIfOutOfRange(digits, MinDigits, MaxDigits);
        ThrowHelper.ThrowIfEnumValueIsUndefined(algorithm);

        return VerifyCore(secret, code, counter, digits, algorithm);
    }

    /// <summary>
    /// Verifies a candidate code against a window of counter values, supporting the RFC 4226 §7.4 resynchronization
    /// strategy, and reports the counter that matched.
    /// </summary>
    /// <param name="secret">The shared secret key, as raw bytes.</param>
    /// <param name="code">The candidate code supplied by the user.</param>
    /// <param name="counter">The first (lowest) counter value to test.</param>
    /// <param name="lookAhead">The number of additional counter values to test beyond <paramref name="counter" />.</param>
    /// <param name="matchedCounter">
    /// When this method returns <see langword="true" />, the counter value that matched; otherwise, <c>-1</c>. The server
    /// should store <c>matchedCounter + 1</c> as the next expected counter.
    /// </param>
    /// <param name="digits">The expected number of decimal digits.</param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <returns><see langword="true" /> if <paramref name="code" /> matches any counter in the inclusive range <c>[counter, counter + lookAhead]</c>; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than 6 or greater than 8, <paramref name="lookAhead" /> is negative, or
    /// <paramref name="algorithm" /> is not a defined <see cref="OtpHashAlgorithm" /> value.
    /// </exception>
    /// <remarks>
    /// The full window is always scanned — matching does not short-circuit — so the loop's duration does not reveal which
    /// counter matched. A wide <paramref name="lookAhead" /> weakens security by admitting more candidate codes; RFC 4226
    /// recommends a small look-ahead combined with throttling of failed attempts.
    /// </remarks>
    public static bool VerifyCode(ReadOnlySpan<byte> secret, ReadOnlySpan<char> code, long counter, int lookAhead, out long matchedCounter, int digits = 6, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1)
    {
        ThrowHelper.ThrowIfOutOfRange(digits, MinDigits, MaxDigits);
        ThrowHelper.ThrowIfNegative(lookAhead);
        ThrowHelper.ThrowIfEnumValueIsUndefined(algorithm);

        matchedCounter = -1;
        bool matched = false;

        for (long c = counter; c <= counter + lookAhead; c++)
        {
            if (VerifyCore(secret, code, c, digits, algorithm) && !matched)
            {
                matchedCounter = c;
                matched = true;
            }
        }

        return matched;
    }
}
