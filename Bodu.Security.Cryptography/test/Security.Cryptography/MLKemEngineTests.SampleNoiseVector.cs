// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.SampleNoiseVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
    /// <summary>
    /// Verifies that sampling a noise vector four PRF streams at a time, through the four-way SHAKE256, yields the
    /// polynomials sampling one stream at a time does, for both noise parameters, every vector length up to the nine
    /// polynomials ML-KEM-1024's encapsulation draws, so the last batch holds each of one to four polynomials, and from
    /// counter bytes that wrap.
    /// </summary>
    /// <param name="eta">The noise parameter η.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void SampleNoiseVector_WhenSampledFourStreamsAtATime_ShouldMatchOneStreamAtATime(int eta)
    {
        var random = new Random(0x0203_0009 + eta);
        byte[] seed = new byte[32];
        int[] counterBases = [0, 3, 253, 255];

        for (int polynomials = 1; polynomials <= 9; polynomials++)
        {
            foreach (int counterBase in counterBases)
            {
                random.NextBytes(seed);
                int[] expected = new int[polynomials * MLKemEngine.N];
                int[] actual = new int[expected.Length];

                MLKemEngine.SampleNoiseVector(eta, seed, counterBase, expected, fourWay: false);
                MLKemEngine.SampleNoiseVector(eta, seed, counterBase, actual, fourWay: true);

                CollectionAssert.AreEqual(expected, actual, $"{polynomials} polynomials from counter {counterBase}");
            }
        }
    }

    /// <summary>
    /// Verifies that sampling a noise vector one stream at a time yields, polynomial by polynomial, the bit-at-a-time
    /// reference's samples with consecutive counter bytes.
    /// </summary>
    /// <param name="eta">The noise parameter η.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void SampleNoiseVector_WhenSampled_ShouldMatchTheReferenceWithConsecutiveCounters(int eta)
    {
        var random = new Random(0x0203_000A + eta);
        byte[] seed = new byte[32];
        random.NextBytes(seed);
        int[] actual = new int[5 * MLKemEngine.N];
        int[] expected = new int[MLKemEngine.N];

        MLKemEngine.SampleNoiseVector(eta, seed, 254, actual, fourWay: true);

        for (int r = 0; r < 5; r++)
        {
            MLKemReference.SamplePolyCbd(eta, seed, (byte)(254 + r), expected);
            CollectionAssert.AreEqual(expected, actual.AsSpan(r * MLKemEngine.N, MLKemEngine.N).ToArray(), $"polynomial {r}");
        }
    }
}
