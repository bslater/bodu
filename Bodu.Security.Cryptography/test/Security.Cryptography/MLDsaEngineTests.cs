// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="MLDsaEngine" /> arithmetic: the modular reductions, the transforms, the
/// coefficient-wise products and the decomposition, held to <see cref="MLDsaReference" /> and to plain integer
/// arithmetic.
/// </summary>
[TestClass]
public partial class MLDsaEngineTests
{
    /// <summary>The coefficient modulus q = 8380417.</summary>
    private const int Q = MLDsaEngine.Q;

    /// <summary>γ₂ = (q − 1) / 32, the parameter of ML-DSA-65 and ML-DSA-87.</summary>
    private const int WideGamma2 = (Q - 1) / 32;

    /// <summary>γ₂ = (q − 1) / 88, the parameter of ML-DSA-44.</summary>
    private const int NarrowGamma2 = (Q - 1) / 88;

    /// <summary>
    /// Returns the representative of a value modulo q in [0, q).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value modulo q.</returns>
    private static int Mod(long value) =>
        (int)(((value % Q) + Q) % Q);

    /// <summary>
    /// Returns polynomials whose coefficients sit at the ends of [0, q) and at a few patterns, then seeded ones.
    /// </summary>
    /// <returns>The polynomials.</returns>
    private static IEnumerable<int[]> Polynomials()
    {
        yield return new int[MLDsaEngine.N];
        yield return Enumerable.Repeat(Q - 1, MLDsaEngine.N).ToArray();
        yield return Enumerable.Range(0, MLDsaEngine.N).Select(i => i % 2 == 0 ? Q - 1 : 0).ToArray();
        yield return Enumerable.Range(0, MLDsaEngine.N).Select(i => i == 0 ? 1 : 0).ToArray();
        yield return Enumerable.Range(0, MLDsaEngine.N).Select(i => i == MLDsaEngine.N - 1 ? Q - 1 : 0).ToArray();

        var random = new Random(0x0204_0001);
        for (int iteration = 0; iteration < 200; iteration++)
            yield return Enumerable.Range(0, MLDsaEngine.N).Select(_ => random.Next(Q)).ToArray();
    }
}
