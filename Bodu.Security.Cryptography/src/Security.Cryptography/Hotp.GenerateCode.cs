// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hotp.GenerateCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public static partial class Hotp
{
    /// <summary>
    /// Generates the RFC 4226 HOTP code for the specified secret and counter.
    /// </summary>
    /// <param name="secret">The shared secret key, as raw bytes.</param>
    /// <param name="counter">The moving counter value, advanced by one on each successful authentication.</param>
    /// <param name="digits">The number of decimal digits in the returned code.</param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <returns>The zero-padded decimal code, exactly <paramref name="digits" /> characters long.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than 6 or greater than 8, or <paramref name="algorithm" /> is not a defined
    /// <see cref="OtpHashAlgorithm" /> value.
    /// </exception>
    /// <remarks>
    /// The result is returned as a string rather than an integer because a code's leading zeros are significant — for
    /// example a computed value of 84204 is the six-digit code <c>"084204"</c>.
    /// </remarks>
    public static string GenerateCode(ReadOnlySpan<byte> secret, long counter, int digits = 6, OtpHashAlgorithm algorithm = OtpHashAlgorithm.Sha1)
    {
        ThrowHelper.ThrowIfOutOfRange(digits, MinDigits, MaxDigits);
        ThrowHelper.ThrowIfEnumValueIsUndefined(algorithm);

        Span<char> code = stackalloc char[digits];
        GenerateInto(secret, counter, digits, algorithm, code);

        return new string(code);
    }
}
