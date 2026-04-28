// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64Tests.Seed.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.IO.Hashing;

public partial class Elf64Tests
{
    /// <summary>
    /// Verifies that the default <see cref="Elf64" /> constructor selects a seed of zero.
    /// </summary>
    [TestMethod]
    public void Seed_WhenDefaultConstructed_ShouldBeZero()
    {
        Elf64 algorithm = new();
        Assert.AreEqual(0UL, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that a seed supplied to the constructor is retained on the <see cref="Elf64.Seed" /> property.
    /// </summary>
    [TestMethod]
    public void Seed_WhenConstructedWithValue_ShouldBeRetained()
    {
        Elf64 algorithm = new(1313UL);
        Assert.AreEqual(1313UL, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that <see cref="Elf64.Seed" /> can be reassigned before any input has been consumed.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetBeforeUse_ShouldBeRetained()
    {
        Elf64 algorithm = new() { Seed = 1313UL };
        Assert.AreEqual(1313UL, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that setting <see cref="Elf64.Seed" /> after input has been consumed throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetAfterHashingStarted_ShouldThrow()
    {
        Elf64 algorithm = new();
        algorithm.Append(new byte[] { 1, 2, 3 });

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() => algorithm.Seed = 1234UL);
    }

    /// <summary>
    /// Verifies that after <see cref="Elf64.Reset" /> the algorithm accepts a new seed again.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetAfterReset_ShouldBeAccepted()
    {
        Elf64 algorithm = new();
        algorithm.Append(new byte[] { 1, 2, 3 });
        algorithm.Reset();
        algorithm.Seed = 131UL;

        Assert.AreEqual(131UL, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that different seed values produce distinct digests for the same input.
    /// </summary>
    [TestMethod]
    public void Append_WhenSeedDiffers_ShouldProduceDifferentHash()
    {
        byte[] input = new byte[] { 0x10, 0x20, 0x30 };

        Elf64 a = new(seed: 0UL);
        Elf64 b = new(seed: 131UL);
        a.Append(input);
        b.Append(input);

        CollectionAssert.AreNotEqual(a.GetCurrentHash(), b.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="Elf64.Reset" /> restores the accumulator to the seed so the next empty-input
    /// digest reflects the seed itself, encoded big-endian.
    /// </summary>
    [TestMethod]
    public void Reset_ShouldRestoreAccumulatorToSeed()
    {
        Elf64 algorithm = new(131UL);
        algorithm.Append(new byte[] { 0x01, 0x02 });
        algorithm.Reset();

        byte[] actual = algorithm.GetCurrentHash();
        byte[] expected = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(expected, 131UL);

        CollectionAssert.AreEqual(expected, actual);
    }
}
