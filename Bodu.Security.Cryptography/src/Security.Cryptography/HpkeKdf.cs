// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeKdf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the Key Derivation Function (KDF) of an HPKE cipher suite, using the algorithm identifiers from the IANA
/// "HPKE KDF Identifiers" registry defined by RFC 9180 §7.2.
/// </summary>
public enum HpkeKdf : ushort
{
    /// <summary>
    /// HKDF-SHA256, KDF identifier <c>0x0001</c>.
    /// </summary>
    HkdfSha256 = 0x0001,

    /// <summary>
    /// HKDF-SHA384, KDF identifier <c>0x0002</c>.
    /// </summary>
    HkdfSha384 = 0x0002,

    /// <summary>
    /// HKDF-SHA512, KDF identifier <c>0x0003</c>.
    /// </summary>
    HkdfSha512 = 0x0003,
}
