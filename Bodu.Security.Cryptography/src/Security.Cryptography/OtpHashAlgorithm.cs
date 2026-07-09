// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OtpHashAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Specifies the HMAC hash algorithm used to derive one-time-password codes, as permitted by the HOTP (RFC 4226) and
/// TOTP (RFC 6238) specifications.
/// </summary>
/// <remarks>
/// <para>
/// The three members correspond exactly to the algorithms admitted by RFC 6238 and to the <c>algorithm</c> label used
/// in the <c>otpauth://</c> provisioning URI (<c>SHA1</c>, <c>SHA256</c>, <c>SHA512</c>). No other hash algorithm is
/// valid for one-time passwords, so — unlike <see cref="System.Security.Cryptography.HashAlgorithmName" /> — this
/// closed enumeration cannot express an unsupported choice.
/// </para>
/// <para>
/// <see cref="Sha1" /> is the default because RFC 4226 mandates HMAC-SHA-1 and it remains the algorithm most widely
/// interoperable with authenticator applications; its use here is a message-authentication construction, not a
/// collision-resistance one, so SHA-1 carries no practical weakness in this role.
/// </para>
/// </remarks>
public enum OtpHashAlgorithm
{
    /// <summary>
    /// HMAC-SHA-1. The RFC 4226 default and the most widely supported authenticator algorithm.
    /// </summary>
    Sha1 = 0,

    /// <summary>
    /// HMAC-SHA-256.
    /// </summary>
    Sha256 = 1,

    /// <summary>
    /// HMAC-SHA-512.
    /// </summary>
    Sha512 = 2,
}
