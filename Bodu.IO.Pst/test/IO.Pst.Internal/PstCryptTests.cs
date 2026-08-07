// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstCryptTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the two MS-PST content encodings: the §5.1 permutation and the §5.2 cyclic substitution.
/// </summary>
/// <remarks>
/// Every reference fixture in the corpus is permute-encoded, so the cyclic routine had never executed. It is the
/// harder of the two: the substitution is keyed by the block identifier and advances a counter per byte, so a
/// transposition, a dropped increment, or a key folded the wrong way would still produce output that looks encoded.
/// These tests pin the properties that distinguish a correct implementation from a plausible one.
/// </remarks>
[TestClass]
public class PstCryptTests
{
    /// <summary>The plaintext shared by the cyclic tests.</summary>
    private static byte[] Sample =>
        [.. Enumerable.Range(0, 64).Select(static i => (byte)(i * 7))];

    /// <summary>
    /// Verifies that the cyclic routine is its own inverse, which is what allows one implementation to both encode
    /// and decode a block.
    /// </summary>
    [TestMethod]
    public void Cyclic_WhenAppliedTwice_ShouldRestoreTheOriginal()
    {
        byte[] original = Sample;
        byte[] data = (byte[])original.Clone();

        PstCrypt.Cyclic(data, 0x1A2B3C4D);
        CollectionAssert.AreNotEqual(original, data);

        PstCrypt.Cyclic(data, 0x1A2B3C4D);
        CollectionAssert.AreEqual(original, data);
    }

    /// <summary>
    /// Verifies that the substitution advances per byte, so a run of identical plaintext does not encode to a run of
    /// identical ciphertext. A dropped counter increment would leave the run intact.
    /// </summary>
    [TestMethod]
    public void Cyclic_WhenInputRepeats_ShouldNotProduceARepeatingOutput()
    {
        byte[] data = new byte[32];

        PstCrypt.Cyclic(data, 0x0000_0001);

        Assert.IsGreaterThan(1, data.Distinct().Count());
    }

    /// <summary>
    /// Verifies that the key participates, so two blocks holding the same bytes at different identifiers encode
    /// differently.
    /// </summary>
    [TestMethod]
    public void Cyclic_WhenKeyDiffers_ShouldProduceDifferentOutput()
    {
        byte[] first = Sample;
        byte[] second = Sample;

        PstCrypt.Cyclic(first, 0x0000_0004);
        PstCrypt.Cyclic(second, 0x0000_0008);

        CollectionAssert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that the key is folded by exclusive-or of its halves, so two identifiers differing only in a way that
    /// cancels under that fold encode identically. This pins the fold itself rather than merely that the key matters.
    /// </summary>
    [TestMethod]
    public void Cyclic_WhenKeysFoldToTheSameValue_ShouldProduceTheSameOutput()
    {
        byte[] first = Sample;
        byte[] second = Sample;

        // 0x0000_1234 and 0x1234_0000 both fold to 0x1234 under key ^ (key >> 16).
        PstCrypt.Cyclic(first, 0x0000_1234);
        PstCrypt.Cyclic(second, 0x1234_0000);

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that an empty block is accepted and leaves nothing to do, since a caller decodes every external block
    /// without first checking its length.
    /// </summary>
    [TestMethod]
    public void Cyclic_WhenDataIsEmpty_ShouldDoNothing()
    {
        PstCrypt.Cyclic(Span<byte>.Empty, 0x1234_5678);
    }

    /// <summary>
    /// Verifies that the permutation table is a bijection over the byte range. A duplicated or missing entry would
    /// make some plaintext unrecoverable while still decoding most blocks plausibly.
    /// </summary>
    [TestMethod]
    public void PermuteDecode_ShouldMapEveryByteToADistinctValue()
    {
        byte[] all = [.. Enumerable.Range(0, 256).Select(static i => (byte)i)];

        PstCrypt.PermuteDecode(all);

        Assert.AreEqual(256, all.Distinct().Count());
    }

    /// <summary>
    /// Verifies that the permutation is applied per byte with no positional component, so the same plaintext byte
    /// decodes to the same value wherever it appears - the property that distinguishes §5.1 from §5.2.
    /// </summary>
    [TestMethod]
    public void PermuteDecode_ShouldNotDependOnPosition()
    {
        byte[] data = [0x41, 0x00, 0x41, 0xFF, 0x41];

        PstCrypt.PermuteDecode(data);

        Assert.AreEqual(data[0], data[2]);
        Assert.AreEqual(data[0], data[4]);
    }
}
