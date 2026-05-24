// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTweakType.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the type of block that is being absorbed by a Skein <c>UBI</c> (Unique Block Iteration) compression call.
/// The value occupies the <c>T[119..125]</c> bits of the Threefish tweak word during that call and ensures that the
/// same chaining state can be reused across distinct stages of the Skein hash without domain collisions.
/// </summary>
/// <remarks>
/// <para>
/// The numeric values match the <c>Type</c> field assignments in Table 5 of the Skein 1.3 specification. Skein uses the
/// type to bind each UBI call to its role in the hashing pipeline — configuration, optional keying, message absorption,
/// and output extraction — preventing cross-protocol confusion between otherwise identical compressions.
/// </para>
/// <para>
/// Only the values used by the sequential Skein hash (<see cref="Key" />, <see cref="Cfg" />, <see cref="Msg" />, and
/// <see cref="Out" />) are exercised by this library. The remaining values (<see cref="Prs" />, <see cref="Pk" />,
/// <see cref="Kdf" />, and <see cref="Non" />) are defined for completeness with the Skein specification and reserved
/// for potential personalization, key-identifier, key-derivation, and nonce-driven extensions.
/// </para>
/// </remarks>
internal enum SkeinTweakType : byte
{
    /// <summary>
    /// Absorbs the MAC key during a keyed-hash (Skein-MAC) computation. Precedes the configuration block.
    /// </summary>
    Key = 0,

    /// <summary>
    /// Absorbs the 32-byte configuration string that seeds the initial chaining value.
    /// </summary>
    Cfg = 4,

    /// <summary>
    /// Absorbs an optional personalization string for domain separation between otherwise identical Skein invocations.
    /// </summary>
    Prs = 8,

    /// <summary>
    /// Absorbs a public key identifier during key-identifier-bound hashing.
    /// </summary>
    Pk = 12,

    /// <summary>
    /// Absorbs key-derivation-function identifier material.
    /// </summary>
    Kdf = 16,

    /// <summary>
    /// Absorbs a nonce to transform Skein into a pseudorandom function.
    /// </summary>
    Non = 20,

    /// <summary>
    /// Absorbs message data — the input being hashed.
    /// </summary>
    Msg = 48,

    /// <summary>
    /// Absorbs the output counter to extract digest material from the final chaining value.
    /// </summary>
    Out = 63,
}
