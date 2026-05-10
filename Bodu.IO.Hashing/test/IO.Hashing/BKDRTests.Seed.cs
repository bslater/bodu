// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDRTests.Seed.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

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
    /// Verifies that <see cref="BKDR.Seed" /> can be reassigned before any input has been consumed.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetBeforeUse_ShouldBeRetained()
    {
        BKDR algorithm = new() { Seed = 1313U };
        Assert.AreEqual(1313U, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that setting <see cref="BKDR.Seed" /> after input has been consumed throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetAfterHashingStarted_ShouldThrow()
    {
        BKDR algorithm = new();
        algorithm.Append(new byte[] { 1, 2, 3 });

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() => algorithm.Seed = 1313U);
    }

    /// <summary>
    /// Verifies that after <see cref="BKDR.Reset" /> the algorithm accepts a new seed again.
    /// </summary>
    [TestMethod]
    public void Seed_WhenSetAfterReset_ShouldBeAccepted()
    {
        BKDR algorithm = new();
        algorithm.Append(new byte[] { 1, 2, 3 });
        algorithm.Reset();
        algorithm.Seed = 1313U;

        Assert.AreEqual(1313U, algorithm.Seed);
    }

    /// <summary>
    /// Verifies that assigning an unsupported seed throws <see cref="ArgumentException" /> and leaves the
    /// previously stored seed unchanged.
    /// </summary>
    [TestMethod]
    public void Seed_WhenAssignedUnsupportedValue_ShouldThrowAndNotModifyState()
    {
        BKDR algorithm = new();
        Assert.ThrowsExactly<ArgumentException>(() => algorithm.Seed = 1234U);
        Assert.AreEqual(131U, algorithm.Seed);
    }
}
