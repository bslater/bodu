// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.SamplePolyCbd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that the word-at-a-time binomial sampler matches the bit-at-a-time reference for both noise parameters
    /// over seeded seeds and every counter byte's low values.
    /// </summary>
    /// <param name="eta">The noise parameter η.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void SamplePolyCbd_WhenSeedsAreSeeded_ShouldMatchTheReference(int eta)
    {
        var random = new Random(0x0203_0004 + eta);
        byte[] seed = new byte[32];

        for (int iteration = 0; iteration < 64; iteration++)
        {
            random.NextBytes(seed);
            byte counter = (byte)iteration;
            int[] expected = new int[MLKemEngine.N];
            int[] actual = new int[MLKemEngine.N];

            MLKemReference.SamplePolyCbd(eta, seed, counter, expected);
            MLKemEngine.SamplePolyCbd(eta, seed, counter, actual);

            CollectionAssert.AreEqual(expected, actual, $"iteration {iteration}");
        }
    }
}
