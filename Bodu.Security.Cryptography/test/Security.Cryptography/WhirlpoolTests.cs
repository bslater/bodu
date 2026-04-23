// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WhirlpoolTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Whirlpool" /> hash algorithm.
/// </summary>
[TestClass]
public partial class WhirlpoolTests
{
    /// <summary>
    /// The canonical <c>Whirlpool</c> (ISO/IEC 10118-3) digest of the empty message.
    /// </summary>
    private const string EmptyDigestWhirlpoolFinal =
        "19FA61D75522A4669B44E39C1D2E1726C530232130D407F89AFEE0964997F7A73E83BE698B288FEBCF88E3E03C4F0757EA8964E59B63D93708B138CC42A66EB3";

    /// <summary>
    /// Verifies that a freshly constructed <see cref="Whirlpool" /> exposes a 512-bit output size.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenDefaultConstructed_ShouldBe512()
    {
        using Whirlpool algorithm = new();

        Assert.AreEqual(512, algorithm.HashSize);
    }

    /// <summary>
    /// Verifies that <see cref="Whirlpool.InputBlockSize" /> reports the 64-byte Whirlpool block size.
    /// </summary>
    [TestMethod]
    public void InputBlockSize_WhenDefaultConstructed_ShouldBe64()
    {
        using Whirlpool algorithm = new();

        Assert.AreEqual(64, algorithm.InputBlockSize);
    }

    /// <summary>
    /// Verifies that <see cref="Whirlpool.OutputBlockSize" /> reports the 64-byte Whirlpool digest size.
    /// </summary>
    [TestMethod]
    public void OutputBlockSize_WhenDefaultConstructed_ShouldBe64()
    {
        using Whirlpool algorithm = new();

        Assert.AreEqual(64, algorithm.OutputBlockSize);
    }

    /// <summary>
    /// Verifies that <see cref="Whirlpool.ComputeHash(byte[])" /> of an empty message matches the published
    /// <c>ISO/IEC 10118-3</c> test vector when <see cref="Whirlpool.Version" /> is
    /// <see cref="WhirlpoolVersion.WhirlpoolInfo3" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenEmptyInput_ForWhirlpoolInfo3_ShouldMatchIsoVector()
    {
        using Whirlpool algorithm = new();

        byte[] digest = algorithm.ComputeHash(Array.Empty<byte>());

        Assert.AreEqual(EmptyDigestWhirlpoolFinal, Convert.ToHexString(digest));
    }

    /// <summary>
    /// Verifies that <see cref="Whirlpool.ComputeHash(byte[])" /> of the single-byte input <c>"a"</c>
    /// matches the published <c>ISO/IEC 10118-3</c> test vector when <see cref="Whirlpool.Version" /> is
    /// <see cref="WhirlpoolVersion.WhirlpoolInfo3" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsLowercaseA_ForWhirlpoolInfo3_ShouldMatchIsoVector()
    {
        using Whirlpool algorithm = new();

        byte[] digest = algorithm.ComputeHash(Encoding.ASCII.GetBytes("a"));

        Assert.AreEqual(
            "8ACA2602792AEC6F11A67206531FB7D7F0DFF59413145E6973C45001D0087B42D11BC645413AEFF63A42391A39145A591A92200D560195E53B478584FDAE231A",
            Convert.ToHexString(digest));
    }

    /// <summary>
    /// Verifies that <see cref="Whirlpool.ComputeHash(byte[])" /> of <c>"abc"</c> matches the published
    /// <c>ISO/IEC 10118-3</c> test vector when <see cref="Whirlpool.Version" /> is
    /// <see cref="WhirlpoolVersion.WhirlpoolInfo3" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsAbc_ForWhirlpoolInfo3_ShouldMatchIsoVector()
    {
        using Whirlpool algorithm = new();

        byte[] digest = algorithm.ComputeHash(Encoding.ASCII.GetBytes("abc"));

        Assert.AreEqual(
            "4E2448A4C6F486BB16B6562C73B4020BF3043E3A731BCE721AE1B303D97E6D4C7181EECA2FA2FBF6543A85BAAA75BF63C17B0AC8F75F4FE76FBB0B4E30BA9C36",
            Convert.ToHexString(digest));
    }

    /// <summary>
    /// Verifies that the digest of a fixed message is deterministic across repeated
    /// <see cref="Whirlpool.ComputeHash(byte[])" /> invocations on the same instance.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledTwice_ShouldProduceEqualDigests()
    {
        using Whirlpool algorithm = new();

        byte[] input = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");
        byte[] first = algorithm.ComputeHash(input);
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that each published <see cref="WhirlpoolVersion" /> produces a digest that differs from the
    /// other two for a non-trivial input, confirming the S-box and MDS selection wiring.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenVersionDiffers_ShouldProduceDistinctDigests()
    {
        byte[] input = Encoding.ASCII.GetBytes("Whirlpool version routing test");

        byte[] info1 = ComputeWith(WhirlpoolVersion.WhirlpoolInfo1, input);
        byte[] info2 = ComputeWith(WhirlpoolVersion.WhirlpoolInfo2, input);
        byte[] info3 = ComputeWith(WhirlpoolVersion.WhirlpoolInfo3, input);

        CollectionAssert.AreNotEqual(info1, info2);
        CollectionAssert.AreNotEqual(info2, info3);
        CollectionAssert.AreNotEqual(info1, info3);

        static byte[] ComputeWith(WhirlpoolVersion version, byte[] data)
        {
            using Whirlpool algorithm = new() { Version = version };
            return algorithm.ComputeHash(data);
        }
    }

    /// <summary>
    /// Verifies that streaming the input through <see cref="HashAlgorithm.TransformBlock" /> in arbitrary
    /// chunk sizes produces the same digest as a single <see cref="Whirlpool.ComputeHash(byte[])" /> call.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputIsStreamed_ShouldMatchSinglePassDigest()
    {
        byte[] input = new byte[200];
        for (int i = 0; i < input.Length; i++)
            input[i] = (byte)i;

        using Whirlpool reference = new();
        byte[] expected = reference.ComputeHash(input);

        using Whirlpool streaming = new();
        int offset = 0;
        int[] chunkSizes = { 1, 7, 63, 64, 65, 1 };

        foreach (int chunk in chunkSizes)
        {
            int size = Math.Min(chunk, input.Length - offset);
            streaming.TransformBlock(input, offset, size, null, 0);
            offset += size;
        }

        streaming.TransformFinalBlock(input, offset, input.Length - offset);

        CollectionAssert.AreEqual(expected, streaming.Hash);
    }
}
