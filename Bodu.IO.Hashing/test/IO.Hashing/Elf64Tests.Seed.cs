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
    /// Verifies that a seed supplied to the constructor is retained on the <see cref="Elf64.Seed" /> property.
    /// </summary>
    [TestMethod]
    public void Seed_WhenConstructedWithValue_ShouldBeRetained()
    {
        Elf64 algorithm = new(1313UL);
        Assert.AreEqual(1313UL, algorithm.Seed);
    }
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
    /// Verifies that <see cref="Elf64.Seed" /> can be reassigned before any input has been consumed.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetBeforeUse_ShouldBeRetained()
    {
        Elf64 algorithm = new() { Seed = 1313UL };
        Assert.AreEqual(1313UL, algorithm.Seed);
    }

}
