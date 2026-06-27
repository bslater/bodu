// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the HPKE establishment mode, which controls whether a pre-shared key and/or sender authentication
/// contribute to the key schedule, per RFC 9180 §5.1.
/// </summary>
public enum HpkeMode : byte
{
    /// <summary>
    /// Base mode (<c>mode_base</c>, <c>0x00</c>) — encryption to a public key with no additional authentication.
    /// </summary>
    Base = 0x00,

    /// <summary>
    /// PSK mode (<c>mode_psk</c>, <c>0x01</c>) — augments base mode with a pre-shared key held by both parties.
    /// </summary>
    Psk = 0x01,

    /// <summary>
    /// Auth mode (<c>mode_auth</c>, <c>0x02</c>) — authenticates the sender using its static private key.
    /// </summary>
    Auth = 0x02,

    /// <summary>
    /// AuthPSK mode (<c>mode_auth_psk</c>, <c>0x03</c>) — combines sender authentication with a pre-shared key.
    /// </summary>
    AuthPsk = 0x03,
}
