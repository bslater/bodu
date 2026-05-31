// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein1024TestVariant.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Enumerates the (output size, operating mode) configurations exercised by <see cref="Skein1024Tests" />, mirroring
/// the output sizes permitted by <see cref="Skein1024" /> (384, 512, 1024 bits) across both the canonical plain hash
/// profile and the keyed Skein-MAC-1024 profile.
/// </summary>
/// <remarks>
/// The <c>Hash</c> entries select the unkeyed plain hash; the <c>Mac</c> entries select Skein-MAC by assigning the
/// specification's <see cref="KeyedAlgorithmSpecification.TestKey" /> on construction. Per-row keys carried by
/// <see cref="KeyedHashAlgorithmKnownAnswer.Key" /> override the variant default during keyed known-answer runs.
/// </remarks>
public enum Skein1024TestVariant
{
    /// <summary>Skein-1024-384, plain hash profile (no key).</summary>
    Hash_384,

    /// <summary>Skein-MAC-1024-384, keyed profile.</summary>
    Mac_384,

    /// <summary>Skein-1024-512, plain hash profile (no key).</summary>
    Hash_512,

    /// <summary>Skein-MAC-1024-512, keyed profile.</summary>
    Mac_512,

    /// <summary>Skein-1024-1024, plain hash profile (no key).</summary>
    Hash_1024,

    /// <summary>Skein-MAC-1024-1024, keyed profile.</summary>
    Mac_1024,
}
