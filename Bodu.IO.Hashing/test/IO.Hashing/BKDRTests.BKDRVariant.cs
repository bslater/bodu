// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDRTests.BKDRVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Identifies the BKDR seed configurations exercised by <see cref="BKDRTests" />: the default seed of 131,
/// a seed of 31, and a seed of 1313.
/// </summary>
public enum BKDRVariant
{
    /// <summary>The default seed (131).</summary>
    Default,

    /// <summary>Seed = 31.</summary>
    Seed31,

    /// <summary>Seed = 1313.</summary>
    Seed1313,
}
