// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein256TestVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Enumerates the (output size, operating mode) configurations exercised by <see cref="Skein256Tests" />, mirroring
/// the output sizes permitted by <see cref="Skein256" /> (128, 160, 224, 256 bits) across both the canonical plain
/// hash profile and the keyed Skein-MAC-256 profile.
/// </summary>
/// <remarks>
/// The <c>Hash</c> entries select the unkeyed plain hash; the <c>Mac</c> entries select Skein-MAC by assigning the
/// specification's <see cref="KeyedAlgorithmSpecification.TestKey" /> on construction. Per-row keys carried by
/// <see cref="KeyedHashAlgorithmKnownAnswer.Key" /> override the variant default during keyed known-answer runs.
/// </remarks>
public enum Skein256TestVariant
{
    /// <summary>Skein-256-128, plain hash profile (no key).</summary>
    Hash_128,

    /// <summary>Skein-MAC-256-128, keyed profile.</summary>
    Mac_128,

    /// <summary>Skein-256-160, plain hash profile (no key).</summary>
    Hash_160,

    /// <summary>Skein-MAC-256-160, keyed profile.</summary>
    Mac_160,

    /// <summary>Skein-256-224, plain hash profile (no key).</summary>
    Hash_224,

    /// <summary>Skein-MAC-256-224, keyed profile.</summary>
    Mac_224,

    /// <summary>Skein-256-256, plain hash profile (no key).</summary>
    Hash_256,

    /// <summary>Skein-MAC-256-256, keyed profile.</summary>
    Mac_256,
}
