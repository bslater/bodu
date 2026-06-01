// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.CryptoStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that a <see cref="HashAlgorithm" /> used through a <see cref="CryptoStream" /> in read mode
    /// produces the same final hash as <see cref="HashAlgorithm.ComputeHash(byte[])" />.
    /// </summary>
    [TestMethod]
    public void CryptoStream_WhenReadingThroughHashAlgorithm_ShouldProduceExpectedHash()
    {
        var input = CryptoTestUtilities.ByteSequence256;

        using TAlgorithm expectedAlgorithm = CreateAlgorithm();
        var expected = expectedAlgorithm.ComputeHash(input);

        using TAlgorithm algorithm = CreateAlgorithm();
        using var source = new MemoryStream(input);
        using var cryptoStream = new CryptoStream(source, algorithm, CryptoStreamMode.Read);

        cryptoStream.CopyTo(Stream.Null);

        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that a <see cref="HashAlgorithm" /> used through a <see cref="CryptoStream" /> in write mode
    /// produces the same final hash as <see cref="HashAlgorithm.ComputeHash(byte[])" />.
    /// </summary>
    [TestMethod]
    public void CryptoStream_WhenWritingThroughHashAlgorithm_ShouldProduceExpectedHash()
    {
        var input = CryptoTestUtilities.ByteSequence256;

        using TAlgorithm expectedAlgorithm = CreateAlgorithm();
        var expected = expectedAlgorithm.ComputeHash(input);

        using TAlgorithm algorithm = CreateAlgorithm();
        using Stream output = Stream.Null;
        using var cryptoStream = new CryptoStream(output, algorithm, CryptoStreamMode.Write);

        cryptoStream.Write(input, 0, input.Length);
        cryptoStream.FlushFinalBlock();

        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }
}