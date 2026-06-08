// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Type.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the three Argon2 variants defined by RFC 9106, which differ only in how reference blocks are selected
/// while filling memory.
/// </summary>
/// <remarks>
/// The numeric values match the type code <c>y</c> hashed into the Argon2 pre-hashing digest and encoded in the PHC
/// string: <c>0</c> for Argon2d, <c>1</c> for Argon2i, and <c>2</c> for Argon2id.
/// </remarks>
internal enum Argon2Type
{
    /// <summary>
    /// Data-dependent addressing. Fastest and most resistant to time-memory trade-off attacks, but exposes memory
    /// access patterns to side-channel observation.
    /// </summary>
    Argon2d = 0,

    /// <summary>
    /// Data-independent addressing. Resistant to side-channel attacks, at the cost of weaker trade-off resistance.
    /// </summary>
    Argon2i = 1,

    /// <summary>
    /// Hybrid addressing: data-independent for the first half of the first pass and data-dependent thereafter. The
    /// recommended default for password hashing.
    /// </summary>
    Argon2id = 2,
}
