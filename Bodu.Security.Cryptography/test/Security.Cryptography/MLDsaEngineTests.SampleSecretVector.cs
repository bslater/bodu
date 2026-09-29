// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.SampleSecretVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that sampling a secret vector four XOF streams at a time, through the four-way SHAKE256, yields the
    /// polynomials sampling one stream at a time does, for every vector length up to k + ℓ, so the last batch holds
    /// each of one to four polynomials, and from nonces whose low byte carries into the high one.
    /// </summary>
    /// <param name="designator">The parameter-set designator: 44 and 87 for η = 2, 65 for η = 4.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void SampleSecretVector_WhenSampledFourStreamsAtATime_ShouldMatchOneStreamAtATime(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        var random = new Random(0x0204_0009 + designator);
        byte[] rhoPrime = new byte[64];
        int[] nonceBases = [0, parameters.L, 253, 255];

        for (int polynomials = 1; polynomials <= parameters.K + parameters.L; polynomials++)
        {
            foreach (int nonceBase in nonceBases)
            {
                random.NextBytes(rhoPrime);
                int[] expected = new int[polynomials * MLDsaEngine.N];
                int[] actual = new int[expected.Length];

                MLDsaEngine.SampleSecretVector(parameters, rhoPrime, nonceBase, expected, fourWay: false);
                MLDsaEngine.SampleSecretVector(parameters, rhoPrime, nonceBase, actual, fourWay: true);

                CollectionAssert.AreEqual(expected, actual, $"{polynomials} polynomials from nonce {nonceBase}");
            }
        }
    }
}
