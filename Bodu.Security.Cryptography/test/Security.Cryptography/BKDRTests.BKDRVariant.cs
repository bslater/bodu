// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDRTests.BKDRVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Identifies the BKDR seed configurations exercised by <see cref="BKDRTests" />: the default
    /// (seed = 131), a seed of 31, and a seed of 1313.
    /// </summary>
    public enum BKDRVariant
    {
        Default,
        Seed31,
        Seed1313,
    }
}
