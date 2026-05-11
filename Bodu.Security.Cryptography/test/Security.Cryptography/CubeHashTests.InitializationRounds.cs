// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashTests.InitializationRounds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CubeHashTests
{
    /// <summary>
    /// Verifies that a new instance of <see cref="CubeHash" /> has a default nitialization rounds hashValue of 16.
    /// </summary>
    [TestMethod]
    public void InitializationRounds_WhenDefaultConstructed_ShouldBe16()
    {
        var algorithm = new CubeHash();
        Assert.AreEqual(16, algorithm.InitializationRounds);
    }

    /// <summary>
    /// Verifies that the nitialization rounds hashValue can be set and retrieved before any algorithming operation starts.
    /// </summary>
    [TestMethod]
    public void InitializationRounds_WhenSetBeforeUse_ShouldBeRetained()
    {
        var algorithm = new CubeHash { InitializationRounds = 32 };
        Assert.AreEqual(32, algorithm.InitializationRounds);
    }

    /// <summary>
    /// Verifies that setting <see cref="CubeHash.InitializationRounds" /> after a algorithm computation has started does not throw an exception.
    /// </summary>
    [TestMethod]
    public void InitializationRounds_WhenSetAfterHashing_ShouldNotThrow()
    {
        var algorithm = new CubeHash();
        byte[] input = new byte[] { 1, 2, 3 };

        algorithm.ComputeHash(input);

        // Change the nitialization rounds hashValue after the first computation, and perform the second algorithm computation with the
        // new nitialization rounds hashValue.
        algorithm.InitializationRounds = 32;
        algorithm.ComputeHash(input);
    }

    /// <summary>
    /// Verifies that setting different values for <see cref="CubeHash.InitializationRounds" /> produces different hash outputs for the
    /// same input data.
    /// </summary>
    [TestMethod]
    public void InitializationRounds_WhenDifferentValuesUsed_ShouldProduceDifferentHashes()
    {
        byte[] input = new byte[] { 0x10, 0x20, 0x30 };

        var algorithmA = new CubeHash { InitializationRounds = 32 };
        var algorithmB = new CubeHash { InitializationRounds = 64 };

        byte[] resultA = algorithmA.ComputeHash(input);
        byte[] resultB = algorithmB.ComputeHash(input);

        CollectionAssert.AreNotEqual(resultA, resultB);
    }

    /// <summary>
    /// Verifies that setting an invalid hashValue for <see cref="CubeHash.InitializationRounds" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(4097)]
    [DataRow(int.MaxValue)]
    public void InitializationRounds_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using CubeHash algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => algorithm.InitializationRounds = value);
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="CubeHash.InitializationRounds" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(256)]
    [DataRow(4096)]
    public void InitializationRounds_WhenSetToValidValue_ShouldBeAssigned(int size)
    {
        using CubeHash algorithm = CreateAlgorithm();
        int original = algorithm.InitializationRounds;
        algorithm.InitializationRounds = size;

        Assert.AreEqual(size, algorithm.InitializationRounds);
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="CubeHash.InitializationRounds" /> updates the internal state.
    /// </summary>
    [TestMethod]
    public void InitializationRounds_WhenSetToValidValue_ShouldUpdateCorrectly()
    {
        using CubeHash algorithm = CreateAlgorithm();
        int round = 100;
        int original = algorithm.InitializationRounds;
        algorithm.InitializationRounds = round;

        Assert.AreEqual(round, algorithm.InitializationRounds);
        Assert.AreNotEqual(original, algorithm.InitializationRounds);
    }

    /// <summary>
    /// Verifies that modifying <see cref="CubeHash.InitializationRounds" /> does not affect other configuration properties.
    /// </summary>
    [TestMethod]
    public void InitializationRounds_WhenChanged_ShouldNotAffectOtherProperties()
    {
        var algorithm = new CubeHash
        {
            InitializationRounds = 10,
            Rounds = 16,
            FinalizationRounds = 32,
            TransformBlockSize = 32,
            HashSize = 256
        };

        algorithm.InitializationRounds = 20;

        Assert.AreEqual(20, algorithm.InitializationRounds, $"{nameof(CubeHash.InitializationRounds)} should update.");
        Assert.AreEqual(16, algorithm.Rounds, $"{nameof(CubeHash.Rounds)} should remain unchanged.");
        Assert.AreEqual(32, algorithm.FinalizationRounds, $"{nameof(CubeHash.FinalizationRounds)} should remain unchanged.");
        Assert.AreEqual(32, algorithm.TransformBlockSize, $"{nameof(CubeHash.TransformBlockSize)} should remain unchanged.");
        Assert.AreEqual(256, algorithm.HashSize, $"{nameof(CubeHash.HashSize)} should remain unchanged.");
    }

    /// <summary>
    /// Verifies that assigning <see cref="CubeHash.InitializationRounds" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void InitializationRounds_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using CubeHash algorithm = CreateAlgorithm();
        byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.InitializationRounds = 32;
        });
    }
}
