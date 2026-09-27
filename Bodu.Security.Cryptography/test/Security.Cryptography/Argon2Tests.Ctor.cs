// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Argon2Tests
{
    /// <summary>
    /// Verifies that a constructor rejects a parallelism outside the permitted range.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenParallelismIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Argon2id(new Argon2Parameters { MemoryKiB = 64, Iterations = 1, Parallelism = 0 });
        });

        Assert.AreEqual("Parallelism", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a constructor rejects memory below the 8 * parallelism floor.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenMemoryBelowFloor_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Argon2id(new Argon2Parameters { MemoryKiB = 4, Iterations = 1, Parallelism = 4 });
        });

        Assert.AreEqual("MemoryKiB", ex.ParamName);
    }
}
