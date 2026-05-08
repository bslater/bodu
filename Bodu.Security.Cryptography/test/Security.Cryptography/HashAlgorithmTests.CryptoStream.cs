// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.CryptoStream.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        byte[] input = CryptoTestUtilities.ByteSequence256;

        using var expectedAlgorithm = CreateAlgorithm();
        byte[] expected = expectedAlgorithm.ComputeHash(input);

        using var algorithm = CreateAlgorithm();
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
        byte[] input = CryptoTestUtilities.ByteSequence256;

        using var expectedAlgorithm = CreateAlgorithm();
        byte[] expected = expectedAlgorithm.ComputeHash(input);

        using var algorithm = CreateAlgorithm();
        using var output = Stream.Null;
        using var cryptoStream = new CryptoStream(output, algorithm, CryptoStreamMode.Write);

        cryptoStream.Write(input, 0, input.Length);
        cryptoStream.FlushFinalBlock();

        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }
}