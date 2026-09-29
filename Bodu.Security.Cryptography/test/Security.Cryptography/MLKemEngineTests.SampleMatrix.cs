// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.SampleMatrix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that sampling the matrix four XOF streams at a time, through the four-way SHAKE128, yields the entries
    /// sampling one stream at a time does, for every rank, including ML-KEM-768's nine entries, whose last batch
    /// holds one.
    /// </summary>
    /// <param name="k">The rank: 2, 3 or 4.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void SampleMatrix_WhenSampledFourStreamsAtATime_ShouldMatchOneStreamAtATime(int k)
    {
        MLKemParameters parameters = Parameters(k);
        var random = new Random(0x0203_0008 + k);
        byte[] rho = new byte[32];

        for (int trial = 0; trial < 8; trial++)
        {
            random.NextBytes(rho);
            int[] expected = new int[k * k * MLKemEngine.N];
            int[] actual = new int[expected.Length];

            MLKemEngine.SampleMatrix(parameters, rho, expected, fourWay: false);
            MLKemEngine.SampleMatrix(parameters, rho, actual, fourWay: true);

            CollectionAssert.AreEqual(expected, actual, $"trial {trial}");
        }
    }
}
