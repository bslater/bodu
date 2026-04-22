// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashTests.CubeHashVariants.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Identifies the CubeHash parameter tuples exercised by <see cref="CubeHashTests" />.
    /// Each value encodes the <c>I_r_b_f_h</c> form: initialisation rounds, transform rounds,
    /// block bytes, finalisation rounds, and hash bits.
    /// </summary>
    public enum CubeHashVariants
    {
        CubeHash16_16_32_32_512,
        CubeHash160_16_32_160_512,
        CubeHash80_8_1_80_512,
        CubeHash10_1_1_10_512,
        CubeHash160_16_32_160_256,
        CubeHash80_8_1_80_256,
        CubeHash10_1_1_10_256
    }
}
