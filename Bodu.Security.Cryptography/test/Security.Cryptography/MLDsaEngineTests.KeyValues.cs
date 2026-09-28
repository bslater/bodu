// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.KeyValues.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Holds the values a key keeps for signing and verification, for comparison between the ways they are produced.
    /// </summary>
    /// <param name="Matrix">The matrix Â.</param>
    /// <param name="HighOrderVector">The vector NTT(t₁·2ᵈ).</param>
    /// <param name="SecretVector1">The vector ŝ₁.</param>
    /// <param name="SecretVector2">The vector ŝ₂.</param>
    /// <param name="LowOrderVector">The vector t̂₀.</param>
    private sealed record KeyValues(int[] Matrix, int[] HighOrderVector, int[] SecretVector1, int[] SecretVector2, int[] LowOrderVector)
    {
        /// <summary>
        /// Allocates zeroed values sized for a parameter set.
        /// </summary>
        /// <param name="parameters">The parameter set.</param>
        /// <returns>The values.</returns>
        public static KeyValues Allocate(MLDsaParameters parameters) =>
            new(
                new int[parameters.K * parameters.L * MLDsaEngine.N],
                new int[parameters.K * MLDsaEngine.N],
                new int[parameters.L * MLDsaEngine.N],
                new int[parameters.K * MLDsaEngine.N],
                new int[parameters.K * MLDsaEngine.N]);

        /// <summary>
        /// Asserts that these values equal the expected ones, naming the first that differs.
        /// </summary>
        /// <param name="expected">The expected values.</param>
        public void AssertEqualTo(KeyValues expected)
        {
            CollectionAssert.AreEqual(expected.Matrix, Matrix, "matrix");
            CollectionAssert.AreEqual(expected.HighOrderVector, HighOrderVector, "NTT(t1 * 2^d)");
            CollectionAssert.AreEqual(expected.SecretVector1, SecretVector1, "s1-hat");
            CollectionAssert.AreEqual(expected.SecretVector2, SecretVector2, "s2-hat");
            CollectionAssert.AreEqual(expected.LowOrderVector, LowOrderVector, "t0-hat");
        }
    }
}
