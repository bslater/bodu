// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashTests.Rounds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CubeHashTests
{
    /// <summary>
    /// Verifies that a new instance of <see cref="CubeHash" /> has a default rounds hashValue of 32.
    /// </summary>
    [TestMethod]
    public void Rounds_WhenDefaultConstructed_ShouldBe16()
    {
        var algorithm = new CubeHash();
        Assert.AreEqual(16, algorithm.Rounds);
    }

    /// <summary>
    /// Verifies that the rounds hashValue can be set and retrieved before any algorithming operation starts.
    /// </summary>
    [TestMethod]
    public void Rounds_WhenSetBeforeUse_ShouldBeRetained()
    {
        var algorithm = new CubeHash { Rounds = 32 };
        Assert.AreEqual(32, algorithm.Rounds);
    }

    /// <summary>
    /// Verifies that setting <see cref="CubeHash.Rounds" /> after a algorithm computation has started does not throw an exception.
    /// </summary>
    [TestMethod]
    public void Rounds_WhenSetAfterHashing_ShouldNotThrow()
    {
        var algorithm = new CubeHash();
        byte[] input = new byte[] { 1, 2, 3 };

        algorithm.ComputeHash(input);

        // Change the rounds hashValue after the first computation, and perform the second algorithm computation with the new rounds hashValue.
        algorithm.Rounds = 32;
        algorithm.ComputeHash(input);
    }

    /// <summary>
    /// Verifies that setting different values for <see cref="CubeHash.Rounds" /> produces different hash outputs for the same input data.
    /// </summary>
    [TestMethod]
    public void Rounds_WhenDifferentValuesUsed_ShouldProduceDifferentHashes()
    {
        byte[] input = new byte[] { 0x10, 0x20, 0x30 };

        var algorithmA = new CubeHash { Rounds = 32 };
        var algorithmB = new CubeHash { Rounds = 64 };

        byte[] resultA = algorithmA.ComputeHash(input);
        byte[] resultB = algorithmB.ComputeHash(input);

        CollectionAssert.AreNotEqual(resultA, resultB);
    }

    /// <summary>
    /// Verifies that setting an invalid hashValue for <see cref="CubeHash.Rounds" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(4097)]
    [DataRow(int.MaxValue)]
    public void Rounds_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using CubeHash algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => algorithm.Rounds = value);
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="CubeHash.Rounds" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(256)]
    [DataRow(4096)]
    public void Rounds_WhenSetToValidValue_ShouldBeAssigned(int size)
    {
        using CubeHash algorithm = CreateAlgorithm();
        int original = algorithm.Rounds;
        algorithm.Rounds = size;

        Assert.AreEqual(size, algorithm.Rounds);
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="CubeHash.Rounds" /> updates the internal state.
    /// </summary>
    [TestMethod]
    public void Rounds_WhenSetToValidValue_ShouldUpdateCorrectly()
    {
        using CubeHash algorithm = CreateAlgorithm();
        int round = 100;
        int original = algorithm.Rounds;
        algorithm.Rounds = round;

        Assert.AreEqual(round, algorithm.Rounds);
        Assert.AreNotEqual(original, algorithm.Rounds);
    }

    /// <summary>
    /// Verifies that modifying <see cref="CubeHash.Rounds" /> does not affect other configuration properties.
    /// </summary>
    [TestMethod]
    public void Rounds_WhenChanged_ShouldNotAffectOtherProperties()
    {
        var algorithm = new CubeHash
        {
            InitializationRounds = 10,
            Rounds = 16,
            FinalizationRounds = 32,
            TransformBlockSize = 32,
            HashSize = 256
        };

        algorithm.Rounds = 20;

        Assert.AreEqual(10, algorithm.InitializationRounds, $"{nameof(CubeHash.InitializationRounds)} should remain unchanged.");
        Assert.AreEqual(20, algorithm.Rounds, $"{nameof(CubeHash.Rounds)} should update.");
        Assert.AreEqual(32, algorithm.FinalizationRounds, $"{nameof(CubeHash.FinalizationRounds)} should remain unchanged.");
        Assert.AreEqual(32, algorithm.TransformBlockSize, $"{nameof(CubeHash.TransformBlockSize)} should remain unchanged.");
        Assert.AreEqual(256, algorithm.HashSize, $"{nameof(CubeHash.HashSize)} should remain unchanged.");
    }
}
