// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashTests.CubeHashVariants.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Identifies the CubeHash variants exercised by <see cref="CubeHashTests" />.
/// </summary>
/// <remarks>
/// The first four values represent the standard output sizes with the NIST SHA-3 reference parameters
/// (i=16, r=16, b=32, f=32) and are exercised via the <see cref="CubeHash(int)"/> constructor.
/// The remaining values cover parameter-sweep combinations and use the property-setter API.
/// Each parameter-sweep name encodes the <c>I_r_b_f_h</c> form: initialisation rounds,
/// transform rounds, block bytes, finalisation rounds, and hash bits.
/// </remarks>
public enum CubeHashVariants
{
    /// <summary>224-bit output, standard parameters (i=16, r=16, b=32, f=32).</summary>
    CubeHash_224,

    /// <summary>256-bit output, standard parameters (i=16, r=16, b=32, f=32).</summary>
    CubeHash_256,

    /// <summary>384-bit output, standard parameters (i=16, r=16, b=32, f=32).</summary>
    CubeHash_384,

    /// <summary>512-bit output, standard parameters (i=16, r=16, b=32, f=32).</summary>
    CubeHash_512,

    CubeHash16_16_32_32_512,
    CubeHash160_16_32_160_512,
    CubeHash80_8_1_80_512,
    CubeHash10_1_1_10_512,
    CubeHash160_16_32_160_256,
    CubeHash80_8_1_80_256,
    CubeHash10_1_1_10_256
}
