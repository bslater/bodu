// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesBlockCipherTests.AesTestVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Enumerates the key-size variants used to parameterise AES block cipher tests, matching the three FIPS-197
/// AES key lengths (128, 192, and 256 bits).
/// </summary>
public enum AesTestVariant
{
    /// <summary>The 128-bit (16-byte) key variant — AES-128.</summary>
    Key128,

    /// <summary>The 192-bit (24-byte) key variant — AES-192.</summary>
    Key192,

    /// <summary>The 256-bit (32-byte) key variant — AES-256.</summary>
    Key256,
}
