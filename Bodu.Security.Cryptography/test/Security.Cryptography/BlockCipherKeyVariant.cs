// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherKeyVariant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the canonical key-size variants exercised by block-cipher families that accept 128-, 192-, or 256-bit
/// keys (AES, Camellia, Twofish). Used as the <c>TVariant</c> type parameter of
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> and as the dispatch key on each family's
/// <c>&lt;Cipher&gt;KnownAnswers.For(variant)</c> accessor.
/// </summary>
/// <remarks>
/// Replaces the family-specific <c>AesTestVariant</c>, <c>CamelliaTestVariant</c>, and <c>TwofishTestVariant</c> enums
/// that previously declared the same three values verbatim. Sharing a single enum keeps the test architecture
/// consistent across families and lets transform- and algorithm-layer flatteners reuse the same dispatch logic.
/// </remarks>
public enum BlockCipherKeyVariant
{
    /// <summary>
    /// The 128-bit (16-byte) key variant.
    /// </summary>
    Key128,

    /// <summary>
    /// The 192-bit (24-byte) key variant.
    /// </summary>
    Key192,

    /// <summary>
    /// The 256-bit (32-byte) key variant.
    /// </summary>
    Key256,
}
