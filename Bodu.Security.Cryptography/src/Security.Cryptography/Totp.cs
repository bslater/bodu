// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Totp.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the Time-based One-Time Password (TOTP) algorithm defined in RFC 6238, generating and verifying the
/// time-derived codes used for two-factor authentication. This class cannot be instantiated.
/// </summary>
/// <remarks>
/// <para>
/// TOTP is <see cref="Hotp" /> with a counter derived from the current time: the counter is the number of whole time
/// steps of <c>periodSeconds</c> that have elapsed since an epoch (the Unix epoch by default), and the code is then
/// computed exactly as for HOTP. Because a code changes each step, verification accepts a small window of adjacent steps
/// to tolerate clock drift and transmission delay.
/// </para>
/// <para>
/// As with <see cref="Hotp" />, the shared secret is supplied as raw key bytes; decode a Base32 <c>otpauth://</c> secret
/// to bytes before calling these methods. The default 30-second period and 6-digit, SHA-1 configuration match the de facto
/// authenticator-application defaults.
/// </para>
/// <para>
/// Like the rest of the library, this implementation offers best-effort side-channel resistance and has not been
/// independently audited.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// byte[] secret = Base32.Decode("JBSWY3DPEHPK3PXP");  // from an otpauth:// URI
///
/// string code = Totp.GenerateCode(secret, DateTimeOffset.UtcNow);
/// bool ok = Totp.VerifyCode(secret, userInput, DateTimeOffset.UtcNow);   // ±1 step by default
///]]>
/// </code>
/// </example>
public static partial class Totp
{
    /// <summary>
    /// The default time-step length, in seconds (RFC 6238 <c>X</c>).
    /// </summary>
    internal const int DefaultPeriodSeconds = 30;

    /// <summary>
    /// Computes the RFC 6238 time-step counter for a timestamp relative to an epoch.
    /// </summary>
    /// <param name="timestamp">The instant to convert.</param>
    /// <param name="epoch">The epoch from which time steps are counted (RFC 6238 <c>T0</c>).</param>
    /// <param name="periodSeconds">The time-step length, in seconds.</param>
    /// <returns>The number of whole time steps elapsed since <paramref name="epoch" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timestamp" /> is earlier than <paramref name="epoch" />.</exception>
    private static long ComputeCounter(DateTimeOffset timestamp, DateTimeOffset epoch, int periodSeconds)
    {
        ThrowHelper.ThrowIfLessThan(timestamp, epoch);

        long elapsedSeconds = timestamp.ToUnixTimeSeconds() - epoch.ToUnixTimeSeconds();
        return elapsedSeconds / periodSeconds;
    }
}
