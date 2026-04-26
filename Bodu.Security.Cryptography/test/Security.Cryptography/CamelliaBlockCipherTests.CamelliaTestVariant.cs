// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipherTests.CamelliaTestVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Enumerates the key-size variants used to parameterise Camellia block cipher tests.
/// </summary>
public enum CamelliaTestVariant
{
    /// <summary>The 128-bit (16-byte) key variant.</summary>
    Key128,

    /// <summary>The 192-bit (24-byte) key variant.</summary>
    Key192,

    /// <summary>The 256-bit (32-byte) key variant.</summary>
    Key256,
}
