// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests.SerpentCipherTestVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the Serpent key/tweak initialisation patterns exercised by
/// <see cref="SerpentCipherTests{TTest, TCipher}" />: an all-zero key/tweak pair and a deterministic non-zero pair generated
/// by the test harness.
/// </summary>
public enum SerpentCipherTestVariant
{
    /// <summary>
    /// All-zero key and all-zero tweak (non-tweakable variants ignore the tweak).
    /// </summary>
    ZeroedKeyAndTweak,

    /// <summary>
    /// A deterministic non-zero key and tweak generated as incremental byte sequences.
    /// </summary>
    DefaultKeyAndTweak,
}
