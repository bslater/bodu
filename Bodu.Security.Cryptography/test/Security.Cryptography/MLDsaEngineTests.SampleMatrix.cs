// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.SampleMatrix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that sampling the matrix four XOF streams at a time, through the four-way SHAKE128, yields the entries
    /// sampling one stream at a time does, for every parameter set's shape, including ML-DSA-65's 30 entries, whose
    /// last batch holds two.
    /// </summary>
    /// <param name="designator">The parameter-set designator: 44, 65 or 87.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void SampleMatrix_WhenSampledFourStreamsAtATime_ShouldMatchOneStreamAtATime(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        var random = new Random(0x0204_0008 + designator);
        byte[] rho = new byte[32];

        for (int trial = 0; trial < 8; trial++)
        {
            random.NextBytes(rho);
            int[] expected = new int[parameters.K * parameters.L * MLDsaEngine.N];
            int[] actual = new int[expected.Length];

            MLDsaEngine.SampleMatrix(parameters, rho, expected, fourWay: false);
            MLDsaEngine.SampleMatrix(parameters, rho, actual, fourWay: true);

            CollectionAssert.AreEqual(expected, actual, $"trial {trial}");
        }
    }
}
