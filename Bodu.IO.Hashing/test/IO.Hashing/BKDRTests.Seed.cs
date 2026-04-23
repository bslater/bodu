// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDRTests.Seed.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class BKDRTests
{
    /// <summary>
    /// Verifies that the default <see cref="BKDR" /> constructor selects the canonical seed of 131.
    /// </summary>
    [TestMethod]
    public void Seed_WhenDefaultConstructed_ShouldBe131()
    {
        BKDR algorithm = new();
        Assert.AreEqual(131U, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that a seed supplied to the constructor is retained on the <see cref="BKDR.Seed" /> property.
    /// </summary>
    [TestMethod]
    public void Seed_WhenConstructedWithValue_ShouldBeRetained()
    {
        BKDR algorithm = new(1313U);
        Assert.AreEqual(1313U, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that constructing with an unsupported seed multiplier throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSeedIsUnsupported_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new BKDR(1234U));
    }

    /// <summary>
    /// Verifies that different seed values produce distinct digests for the same input.
    /// </summary>
    [TestMethod]
    public void Append_WhenSeedDiffers_ShouldProduceDifferentHash()
    {
        byte[] input = new byte[] { 0x10, 0x20, 0x30 };

        BKDR a = new(131313U);
        BKDR b = new(13131U);
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreNotEqual(a.GetCurrentHash(), b.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="BKDR.Reset" /> restores the accumulator to the seed so the next empty-input
    /// digest reflects the seed itself, encoded big-endian.
    /// </summary>
    [TestMethod]
    public void Reset_ShouldRestoreAccumulatorToSeed()
    {
        BKDR algorithm = new(13131U);
        algorithm.Append(new byte[] { 0x01, 0x02 });
        algorithm.Reset();

        byte[] actual = algorithm.GetCurrentHash();
        byte[] expected = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(expected, 13131U);

        CollectionAssert.AreEqual(expected, actual);
    }
}
