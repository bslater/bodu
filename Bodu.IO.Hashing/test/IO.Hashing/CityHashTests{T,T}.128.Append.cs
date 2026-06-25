// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests{T,T}.128.Append.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class CityHash128Tests
{

    /// <summary>
    /// Verifies that flipping a single bit in the input changes the digest, sampled across the major
    /// internal length paths.
    /// </summary>
    /// <param name="length">The input length under test, in bytes.</param>
    [TestMethod]
    [DataRow(8)]
    [DataRow(64)]
    [DataRow(200)]
    public void Append_WhenInputDiffersByOneBit_ShouldProduceDifferentDigest(int length)
    {
        byte[] a = new byte[length];
        for (int i = 0; i < length; i++)
            a[i] = (byte)(i * 17);

        byte[] b = (byte[])a.Clone();
        b[length - 1] ^= 0x01;

        CityHash128 ha = CreateAlgorithm();
        CityHash128 hb = CreateAlgorithm();
        ha.Append(a);
        hb.Append(b);

        CollectionAssert.AreNotEqual(ha.GetCurrentHash(), hb.GetCurrentHash(),
            $"Flipping a single bit in a {length}-byte input must change the digest.");
    }

    /// <summary>
    /// Verifies that hashing a long varied input (exercising the iterative main loop plus tail reduction)
    /// produces a non-trivial 16-byte digest.
    /// </summary>
    [TestMethod]
    public void Append_WhenLongInput_ShouldProduceNonZeroDigest()
    {
        byte[] input = new byte[512];
        for (int i = 0; i < input.Length; i++)
            input[i] = (byte)((i * 31) ^ 0xA5);

        CityHash128 algorithm = CreateAlgorithm();
        algorithm.Append(input);
        byte[] digest = algorithm.GetCurrentHash();

        Assert.HasCount(16, digest);
        Assert.Contains(b => b != 0, digest);
    }

    /// <summary>
    /// Verifies that hashing the same bytes through two independent instances yields identical digests.
    /// </summary>
    /// <param name="length">The input length under test, in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(127)]
    [DataRow(128)]
    [DataRow(129)]
    [DataRow(256)]
    public void Append_WhenSameInput_ShouldProduceSameDigest(int length)
    {
        byte[] input = new byte[length];
        for (int i = 0; i < length; i++)
            input[i] = (byte)(i * 13);

        CityHash128 a = CreateAlgorithm();
        CityHash128 b = CreateAlgorithm();
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreEqual(a.GetCurrentHash(), b.GetCurrentHash(),
            $"Two independent instances must produce the same digest for a {length}-byte input.");
    }
    /// <summary>
    /// Verifies that hashing a short, varied input produces a non-trivial 16-byte digest.
    /// </summary>
    [TestMethod]
    public void Append_WhenShortInput_ShouldProduceNonZeroDigest()
    {
        byte[] input = Enumerable.Range(1, 24).Select(i => (byte)(i * 7)).ToArray();
        CityHash128 algorithm = CreateAlgorithm();
        algorithm.Append(input);
        byte[] digest = algorithm.GetCurrentHash();

        Assert.HasCount(16, digest);
        Assert.Contains(b => b != 0, digest, "Short varied input should not produce an all-zero digest.");
    }

}
