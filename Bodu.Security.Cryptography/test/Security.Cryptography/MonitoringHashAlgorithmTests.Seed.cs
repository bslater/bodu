// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringHashAlgorithmTests.Seed.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class MonitoringHashAlgorithmTests
{
    /// <summary>
    /// Verifies that a new instance of <see cref="Elf64" /> has a default seed hashValue of zero.
    /// </summary>
    [TestMethod]
    public void Seed_WhenDefaultConstructed_ShouldBeZero()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        Assert.AreEqual<ulong>(0, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that the seed hashValue can be set and retrieved before any algorithming operation starts.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetBeforeUse_ShouldBeRetained()
    {
        var algorithm = new MonitoringHashAlgorithm { Seed = 10 };
        Assert.AreEqual<uint>(10, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that setting <see cref="Elf64.Seed" /> after a algorithm computation has begun throws a <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetAfterHashingStarted_ShouldThrowExactly()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var input = new byte[] { 1, 2, 3 };
        algorithm.TransformBlock(input, 0, input.Length, input, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() => algorithm.Seed = 1234);
    }

    /// <summary>
    /// Verifies that setting <see cref="Elf64.Seed" /> after a algorithm computation has started does not throw a <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetAfterHashing_ShouldNotThrow()
    {
        MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        var input = new byte[] { 1, 2, 3 };

        algorithm.ComputeHash(input);

        // Change the seed hashValue after the first computation, and perform the second algorithm computation with the new seed hashValue.
        algorithm.Seed = 131;
        algorithm.ComputeHash(input);
    }

    /// <summary>
    /// Verifies that using different seed values results in different algorithm outputs for the same input.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WithDifferentSeeds_ShouldReturnDifferentResults()
    {
        var input = new byte[] { 0x10, 0x20, 0x30 };

        var algorithmA = new MonitoringHashAlgorithm { Seed = 10 };
        var algorithmB = new MonitoringHashAlgorithm { Seed = 20 };

        var resultA = algorithmA.ComputeHash(input);
        var resultB = algorithmB.ComputeHash(input);

        CollectionAssert.AreNotEqual(resultA, resultB);
    }

    /// <summary>
    /// Verifies that calling <see cref="Elf64.Initialise" /> resets the internal algorithm state to the seed hashValue.
    /// </summary>
    [TestMethod]
    public void Initialize_ShouldResetHashStateToSeed()
    {
        var algorithm = new MonitoringHashAlgorithm { Seed = 10 };

        _ = algorithm.ComputeHash([0x01, 0x02]);
        algorithm.Initialize();

        var fresh = algorithm.ComputeHash([]);

        // Should match seed state as algorithm result
        var expected = BitConverter.GetBytes((uint)10);

        CollectionAssert.AreEqual(expected, fresh);
    }
}
