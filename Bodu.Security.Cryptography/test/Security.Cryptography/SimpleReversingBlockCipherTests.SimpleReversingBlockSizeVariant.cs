// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingBlockCipherTests.SimpleReversingBlockSizeVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the block size variant to use when constructing a <see cref="SimpleReversingBlockCipher" />
/// during testing.
/// </summary>
public enum SimpleReversingBlockSizeVariant
{
    /// <summary>128-bit (16-byte) block — the default block size.</summary>
    Block128 = 128,

    /// <summary>192-bit (24-byte) block.</summary>
    Block192 = 192,

    /// <summary>256-bit (32-byte) block.</summary>
    Block256 = 256,
}
