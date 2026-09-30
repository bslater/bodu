// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.ExpandMaskVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that expanding the mask vector four XOF streams at a time, through the four-way SHAKE256, yields the
    /// polynomials expanding one stream at a time does, for every parameter set - ℓ of 4, 5 and 7, so the last batch
    /// holds four, one or three - and for offsets κ whose nonces carry from the low byte into the high one.
    /// </summary>
    /// <param name="designator">The parameter-set designator: 44, 65 or 87.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void ExpandMaskVector_WhenExpandedFourStreamsAtATime_ShouldMatchOneStreamAtATime(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        var random = new Random(0x0204_000A + designator);
        byte[] rhoDoublePrime = new byte[64];
        int[] kappas = [0, parameters.L, 2 * parameters.L, 251, 252, 253, 254, 255, 256, 65536 - parameters.L];

        foreach (int kappa in kappas)
        {
            random.NextBytes(rhoDoublePrime);
            int[] expected = new int[parameters.L * MLDsaEngine.N];
            int[] actual = new int[expected.Length];

            MLDsaEngine.ExpandMaskVector(parameters, rhoDoublePrime, kappa, expected, fourWay: false);
            MLDsaEngine.ExpandMaskVector(parameters, rhoDoublePrime, kappa, actual, fourWay: true);

            CollectionAssert.AreEqual(expected, actual, $"κ = {kappa}");
        }
    }
}
