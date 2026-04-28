// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WhirlpoolTests.Version.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class WhirlpoolTests
{
    /// <summary>
    /// Verifies that a freshly constructed <see cref="Whirlpool" /> defaults to
    /// <see cref="WhirlpoolVersion.WhirlpoolInfo3" />.
    /// </summary>
    [TestMethod]
    public void Version_WhenDefaultConstructed_ShouldReturnWhirlpoolInfo3()
    {
        using Whirlpool algorithm = new();

        Assert.AreEqual(WhirlpoolVersion.WhirlpoolInfo3, algorithm.Version);
    }

    /// <summary>
    /// Verifies that <see cref="Whirlpool.Version" /> can be reassigned before any input has been consumed.
    /// </summary>
    [TestMethod]
    public void Version_WhenSetBeforeUse_ShouldBeRetained()
    {
        using Whirlpool algorithm = new() { Version = WhirlpoolVersion.WhirlpoolInfo1 };

        Assert.AreEqual(WhirlpoolVersion.WhirlpoolInfo1, algorithm.Version);
    }

    /// <summary>
    /// Verifies that assigning an undefined <see cref="WhirlpoolVersion" /> value throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Version_WhenSetToUndefinedValue_ShouldThrowArgumentOutOfRange()
    {
        using Whirlpool algorithm = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Version = (WhirlpoolVersion)999;
        });
    }

    /// <summary>
    /// Verifies that setting <see cref="Whirlpool.Version" /> after input has been consumed throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Version_WhenSetAfterHashingStarted_ShouldThrowExactly()
    {
        using Whirlpool algorithm = new();
        algorithm.TransformBlock(new byte[] { 1, 2, 3 }, 0, 3, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.Version = WhirlpoolVersion.WhirlpoolInfo1;
        });
    }

    /// <summary>
    /// Verifies that <see cref="Whirlpool.Initialize" /> returns the algorithm to the reconfigurable state
    /// so <see cref="Whirlpool.Version" /> may be reassigned.
    /// </summary>
    [TestMethod]
    public void Version_WhenSetAfterInitialize_ShouldBeAccepted()
    {
        using Whirlpool algorithm = new();
        algorithm.TransformBlock(new byte[] { 1, 2, 3 }, 0, 3, null, 0);
        algorithm.Initialize();

        algorithm.Version = WhirlpoolVersion.WhirlpoolInfo2;

        Assert.AreEqual(WhirlpoolVersion.WhirlpoolInfo2, algorithm.Version);
    }

    /// <summary>
    /// Verifies that setting <see cref="Whirlpool.Version" /> on a disposed algorithm throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Version_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
    {
        Whirlpool algorithm = new();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Version = WhirlpoolVersion.WhirlpoolInfo1;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="Whirlpool.Version" /> on a disposed algorithm throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Version_WhenReadAfterDispose_ShouldThrowObjectDisposedException()
    {
        Whirlpool algorithm = new();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.Version;
        });
    }
}
