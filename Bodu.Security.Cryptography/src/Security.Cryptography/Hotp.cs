// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hotp.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the HMAC-based One-Time Password (HOTP) algorithm defined in RFC 4226, generating and verifying the
/// counter-based codes used for two-factor authentication. This class cannot be instantiated.
/// </summary>
/// <remarks>
/// <para>
/// HOTP derives a short decimal code from a shared secret and a monotonically increasing counter: it computes an HMAC
/// of the 8-byte big-endian counter under the secret, applies the RFC 4226 §5.3 dynamic-truncation function to select a
/// 31-bit value from the MAC, and reduces that value modulo <c>10^digits</c> to produce a zero-padded decimal string.
/// The counter is advanced by one on each successful authentication; <see cref="Totp" /> layers a time-derived counter
/// on top of the same construction.
/// </para>
/// <para>
/// The shared secret is supplied as raw key bytes. Authenticator applications conventionally exchange the secret as a
/// Base32 string inside an <c>otpauth://</c> URI; decode it to bytes before calling these methods (for example with
/// <c>Bodu.Text.Encoding.Base32</c>) — this type deliberately takes no dependency on a particular text encoding. RFC
/// 4226 recommends a secret of at least 128 bits, and 160 bits (the SHA-1 output length) for full strength.
/// </para>
/// <para>
/// Like the rest of the library, this implementation offers best-effort side-channel resistance and has not been
/// independently audited. Code verification uses
/// <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> so that a
/// valid-but-wrong code is not distinguishable from an invalid one by comparison timing.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// byte[] secret = Base32.Decode("JBSWY3DPEHPK3PXP");  // from an otpauth:// URI
///
/// string code = Hotp.GenerateCode(secret, counter: 0);          // "996554" (6 digits, SHA-1)
/// bool ok = Hotp.VerifyCode(secret, userInput, counter: 0);
///]]>
/// </code>
/// </example>
public static partial class Hotp
{
    /// <summary>The smallest number of decimal digits permitted for a generated code.</summary>
    internal const int MinDigits = 6;

    /// <summary>The largest number of decimal digits permitted for a generated code. RFC 4226's reference truncation table caps the meaningful width at eight digits, because dynamic truncation yields a 31-bit value.</summary>
    internal const int MaxDigits = 8;

    /// <summary>The largest resynchronization look-ahead permitted by <see cref="VerifyCode(System.ReadOnlySpan{byte}, System.ReadOnlySpan{char}, long, int, out long, int, OtpHashAlgorithm)" />. Each admitted counter costs one HMAC and the window is scanned in full for constant-time behaviour, so an unbounded value is a denial-of-service lever; RFC 4226 §7.4 recommends a small window, and this ceiling stays generous while bounding the work.</summary>
    internal const int MaxLookAhead = 1024;

    /// <summary>
    /// Computes the HOTP code for a single counter value and writes it, zero-padded, into
    /// <paramref name="destination" />.
    /// </summary>
    /// <param name="secret">The shared secret key.</param>
    /// <param name="counter">The moving counter value.</param>
    /// <param name="digits">
    /// The number of decimal digits to produce; the length of <paramref name="destination" />.
    /// </param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <param name="destination">The span that receives the decimal code, one character per digit.</param>
    /// <remarks>
    /// Shared core used by both the public generation surface and by <see cref="Totp" />; assumes its arguments have
    /// already been validated.
    /// </remarks>
    internal static void GenerateInto(ReadOnlySpan<byte> secret, long counter, int digits, OtpHashAlgorithm algorithm, Span<char> destination)
    {
        Span<byte> counterBytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

        Span<byte> mac = stackalloc byte[64];
        int macLength = ComputeHmac(algorithm, secret, counterBytes, mac);
        Span<byte> hash = mac[..macLength];

        // RFC 4226 §5.3 dynamic truncation: the low nibble of the final byte selects a 4-byte window, whose high bit is
        // masked off to avoid sign ambiguity, yielding a 31-bit binary code.
        int offset = hash[macLength - 1] & 0x0F;
        uint binary = ((uint)(hash[offset] & 0x7F) << 24)
                    | ((uint)hash[offset + 1] << 16)
                    | ((uint)hash[offset + 2] << 8)
                    | hash[offset + 3];

        uint modulo = 1;
        for (int i = 0; i < digits; i++)
            modulo *= 10;

        uint code = binary % modulo;

        for (int i = digits - 1; i >= 0; i--)
        {
            destination[i] = (char)('0' + (int)(code % 10));
            code /= 10;
        }

        CryptographicOperations.ZeroMemory(hash);
    }

    /// <summary>
    /// Verifies a candidate code against the HOTP code for a single counter value in constant time.
    /// </summary>
    /// <param name="secret">The shared secret key.</param>
    /// <param name="code">The candidate code supplied by the user.</param>
    /// <param name="counter">The counter value to test.</param>
    /// <param name="digits">The expected number of decimal digits.</param>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="code" /> matches the computed code; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Shared core used by both the public verification surface and by <see cref="Totp" />; assumes its arguments have
    /// already been validated.
    /// </remarks>
    internal static bool VerifyCore(ReadOnlySpan<byte> secret, ReadOnlySpan<char> code, long counter, int digits, OtpHashAlgorithm algorithm)
    {
        // Length is not secret, so an early return here leaks nothing; FixedTimeEquals also requires equal lengths.
        if (code.Length != digits)
            return false;

        // The expected code and both widened copies are OTP secrets for the lifetime of the counter window; all
        // three are zeroed in the finally.
        Span<char> expected = stackalloc char[digits];
        Span<byte> expectedBytes = stackalloc byte[digits];
        Span<byte> actualBytes = stackalloc byte[digits];

        try
        {
            GenerateInto(secret, counter, digits, algorithm, expected);

            for (int i = 0; i < digits; i++)
            {
                expectedBytes[i] = (byte)expected[i];
                actualBytes[i] = (byte)code[i];
            }

            return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        finally
        {
            CryptographyHelper.Clear(actualBytes);
            CryptographicOperations.ZeroMemory(expectedBytes);
            CryptographyHelper.Clear(expected);
        }
    }

    /// <summary>
    /// Computes the HMAC of <paramref name="source" /> under <paramref name="secret" /> for the selected
    /// one-time-password hash algorithm, writing the result into <paramref name="destination" />.
    /// </summary>
    /// <param name="algorithm">The HMAC hash algorithm to use.</param>
    /// <param name="secret">The shared secret key.</param>
    /// <param name="source">The counter block to authenticate.</param>
    /// <param name="destination">The span that receives the MAC; must be at least the hash length long.</param>
    /// <returns>The number of bytes written, equal to the hash length of <paramref name="algorithm" />.</returns>
    private static int ComputeHmac(OtpHashAlgorithm algorithm, ReadOnlySpan<byte> secret, ReadOnlySpan<byte> source, Span<byte> destination) =>
        algorithm switch
        {
            OtpHashAlgorithm.Sha1 => HMACSHA1.HashData(secret, source, destination),
            OtpHashAlgorithm.Sha256 => HMACSHA256.HashData(secret, source, destination),
            OtpHashAlgorithm.Sha512 => HMACSHA512.HashData(secret, source, destination),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
}
