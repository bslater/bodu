// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeAead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the Authenticated Encryption with Associated Data (AEAD) function of an HPKE cipher suite, using the
/// algorithm identifiers from the IANA "HPKE AEAD Identifiers" registry defined by RFC 9180 §7.3.
/// </summary>
public enum HpkeAead : ushort
{
    /// <summary>AES-128-GCM, AEAD identifier <c>0x0001</c>.</summary>
    Aes128Gcm = 0x0001,

    /// <summary>AES-256-GCM, AEAD identifier <c>0x0002</c>.</summary>
    Aes256Gcm = 0x0002,

    /// <summary>ChaCha20Poly1305, AEAD identifier <c>0x0003</c>.</summary>
    ChaCha20Poly1305 = 0x0003,

    /// <summary>
    /// Export-only, AEAD identifier <c>0xFFFF</c>. Suites using this value support only the secret-export interface and
    /// reject encryption and decryption.
    /// </summary>
    ExportOnly = 0xFFFF,
}
