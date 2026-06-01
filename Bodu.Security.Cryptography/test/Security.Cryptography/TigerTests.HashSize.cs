// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.HashSize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    /// <summary>
    /// Verifies that a new instance of <see cref="Tiger" /> has a default hash size hashValue of 512.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenDefaultConstructed_ShouldBe192()
    {
        Tiger algorithm = CreateAlgorithm();
        Assert.AreEqual(192, algorithm.HashSize);
    }

    /// <summary>
    /// Verifies that the hash size hashValue can be set and retrieved before any algorithming operation starts.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetBeforeUse_ShouldBeRetained()
    {
        var algorithm = new Tiger { HashSize = 128 };
        Assert.AreEqual(128, algorithm.HashSize);
    }

    /// <summary>
    /// Verifies that setting <see cref="Tiger.HashSize" /> after a algorithm computation has started does not throw an exception.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetAfterHashing_ShouldNotThrow()
    {
        Tiger algorithm = CreateAlgorithm();
        var input = new byte[] { 1, 2, 3 };

        algorithm.ComputeHash(input);

        // Change the hash size hashValue after the first computation, and perform the second algorithm computation with the new hash
        // size hashValue.
        algorithm.HashSize = 128;
        algorithm.ComputeHash(input);
    }

    /// <summary>
    /// Verifies that setting an invalid hashValue for <see cref="Tiger.HashSize" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(127)]
    [DataRow(193)]
    [DataRow(161)]
    [DataRow(256)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void HashSize_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using Tiger algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => algorithm.HashSize = value);
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="Tiger.HashSize" />.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(160)]
    [DataRow(192)]
    public void HashSize_WhenSetToValidValue_ShouldBeAssigned(int size)
    {
        using Tiger algorithm = CreateAlgorithm();
        algorithm.HashSize = size;

        Assert.AreEqual(size, algorithm.HashSize);
    }

    /// <summary>
    /// Verifies that setting a valid hashValue for <see cref="Tiger.HashSize" /> updates the internal state.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetToValidValue_ShouldUpdateCorrectly()
    {
        using Tiger algorithm = CreateAlgorithm();
        var size = 160;
        var original = algorithm.HashSize;
        algorithm.HashSize = size;

        Assert.AreEqual(size, algorithm.HashSize);
        Assert.AreNotEqual(original, algorithm.HashSize);
    }

    /// <summary>
    /// Verifies that modifying <see cref="Tiger.HashSize" /> does not affect other configuration properties.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenChanged_ShouldNotAffectOtherProperties()
    {
        var algorithm = new Tiger
        {
            HashSize = 160
        };

        algorithm.HashSize = 128;

        Assert.AreEqual(TigerHashingVariant.Tiger, algorithm.Variant, $"{nameof(Tiger.Variant)} should remain unchanged.");
        Assert.AreEqual(128, algorithm.HashSize, $"{nameof(Tiger.HashSize)} should update.");
    }

    /// <summary>
    /// Verifies that reading <see cref="Tiger.HashSize" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale value.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        Tiger algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.HashSize;
        });
    }

    /// <summary>
    /// Verifies that setting <see cref="Tiger.HashSize" /> after <see cref="HashAlgorithm.TransformBlock" />
    /// has been called throws <see cref="CryptographicUnexpectedOperationException" /> rather than
    /// silently mutating the digest length mid-computation.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using Tiger algorithm = CreateAlgorithm();
        var input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.HashSize = 128;
        });
    }
}
