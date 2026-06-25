// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeKem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the Key Encapsulation Mechanism (KEM) of an HPKE cipher suite, using the algorithm identifiers from the
/// IANA "HPKE KEM Identifiers" registry defined by RFC 9180 §7.1.
/// </summary>
public enum HpkeKem : ushort
{
    /// <summary>
    /// DHKEM(X25519, HKDF-SHA256) — Diffie-Hellman KEM over Curve25519 with HKDF-SHA256, KEM identifier <c>0x0020</c>.
    /// </summary>
    X25519HkdfSha256 = 0x0020,
}
