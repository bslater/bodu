// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashTests.FinalizationRounds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CubeHashTests
{
    /// <summary>
    /// Verifies that a new instance of <see cref="CubeHash" /> has a default finalization rounds hashValue of 32.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenDefaultConstructed_ShouldBe32()
    {
        var algorithm = new CubeHash();
        Assert.AreEqual(32, algorithm.FinalizationRounds);
    }

    /// <summary>
    /// Verifies that the finalization rounds hashValue can be set and retrieved before any algorithming operation starts.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetBeforeUse_ShouldBeRetained()
    {
        var algorithm = new CubeHash { FinalizationRounds = 64 };
        Assert.AreEqual(64, algorithm.FinalizationRounds);
    }

    /// <summary>
    /// Verifies that setting <see cref="CubeHash.FinalizationRounds" /> after a algorithm computation has started does not throw an exception.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetAfterHashing_ShouldNotThrow()
    {
        var algorithm = new CubeHash();
        var input = new byte[] { 1, 2, 3 };

        algorithm.ComputeHash(input);

        // Change the finalization rounds hashValue after the first computation, and perform the second algorithm computation with the
        // new finalization rounds hashValue.
        algorithm.FinalizationRounds = 64;
        algorithm.ComputeHash(input);
    }

    /// <summary>
    /// Verifies that setting different values for <see cref="CubeHash.FinalizationRounds" /> produces different hash outputs for the
    /// same input data.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenDifferentValuesUsed_ShouldProduceDifferentHashes()
    {
        var input = new byte[] { 0x10, 0x20, 0x30 };

        var algorithmA = new CubeHash { FinalizationRounds = 32 };
        var algorithmB = new CubeHash { FinalizationRounds = 64 };

        var resultA = algorithmA.ComputeHash(input);
        var resultB = algorithmB.ComputeHash(input);

        CollectionAssert.AreNotEqual(resultA, resultB);
    }

    /// <summary>
    /// Verifies that setting an invalid hashValue for <see cref="CubeHash.FinalizationRounds" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(4097)]
    [DataRow(int.MaxValue)]
    public void FinalizationRounds_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using CubeHash algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => algorithm.FinalizationRounds = value);
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="CubeHash.FinalizationRounds" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(8)]
    [DataRow(64)]
    [DataRow(128)]
    [DataRow(256)]
    [DataRow(4096)]
    public void FinalizationRounds_WhenSetToValidValue_ShouldBeAssigned(int size)
    {
        using CubeHash algorithm = CreateAlgorithm();
        var original = algorithm.FinalizationRounds;
        algorithm.FinalizationRounds = size;

        Assert.AreEqual(size, algorithm.FinalizationRounds);
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="CubeHash.FinalizationRounds" /> updates the internal state.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetToValidValue_ShouldUpdateCorrectly()
    {
        using CubeHash algorithm = CreateAlgorithm();
        var round = 100;
        var original = algorithm.FinalizationRounds;
        algorithm.FinalizationRounds = round;

        Assert.AreEqual(round, algorithm.FinalizationRounds);
        Assert.AreNotEqual(original, algorithm.FinalizationRounds);
    }

    /// <summary>
    /// Verifies that modifying <see cref="CubeHash.FinalizationRounds" /> does not affect other configuration properties.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenChanged_ShouldNotAffectOtherProperties()
    {
        var algorithm = new CubeHash
        {
            InitializationRounds = 10,
            Rounds = 16,
            FinalizationRounds = 32,
            TransformBlockSize = 32,
            HashSize = 256
        };

        algorithm.FinalizationRounds = 20;

        Assert.AreEqual(10, algorithm.InitializationRounds, $"{nameof(CubeHash.InitializationRounds)} should remain unchanged.");
        Assert.AreEqual(16, algorithm.Rounds, $"{nameof(CubeHash.Rounds)} should remain unchanged.");
        Assert.AreEqual(20, algorithm.FinalizationRounds, $"{nameof(CubeHash.FinalizationRounds)} should update.");
        Assert.AreEqual(32, algorithm.TransformBlockSize, $"{nameof(CubeHash.TransformBlockSize)} should remain unchanged.");
        Assert.AreEqual(256, algorithm.HashSize, $"{nameof(CubeHash.HashSize)} should remain unchanged.");
    }

    /// <summary>
    /// Verifies that assigning <see cref="CubeHash.FinalizationRounds" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" /> rather than silently
    /// reconfiguring rounds mid-computation.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using CubeHash algorithm = CreateAlgorithm();
        var input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.FinalizationRounds = 64;
        });
    }
}
