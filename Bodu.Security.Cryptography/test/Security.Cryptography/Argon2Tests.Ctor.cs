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

    /// <summary>
    /// Verifies that each variant's bounded constructor rejects a bound of zero or below <c>-1</c>, naming the
    /// parameter.
    /// </summary>
    /// <param name="variant">The variant constructed.</param>
    /// <param name="maxDegreeOfParallelism">The invalid bound.</param>
    [TestMethod]
    [DataRow("id", 0)]
    [DataRow("id", -2)]
    [DataRow("id", int.MinValue)]
    [DataRow("i", 0)]
    [DataRow("i", -2)]
    [DataRow("d", 0)]
    [DataRow("d", -2)]
    public void Constructor_WhenMaxDegreeOfParallelismIsInvalid_ShouldThrowArgumentOutOfRangeException(string variant, int maxDegreeOfParallelism)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Create(variant, FastParameters(), maxDegreeOfParallelism);
        });

        Assert.AreEqual("maxDegreeOfParallelism", ex.ParamName);
    }

    /// <summary>
    /// Verifies that each variant's bounded constructor rejects null parameters, naming the parameter.
    /// </summary>
    /// <param name="variant">The variant constructed.</param>
    [TestMethod]
    [DataRow("id")]
    [DataRow("i")]
    [DataRow("d")]
    public void Constructor_WhenParametersIsNullAndBoundIsGiven_ShouldThrowArgumentNullException(string variant)
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Create(variant, null!, 1);
        });

        Assert.AreEqual("parameters", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the bounded constructor still validates the cost parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenBoundIsGivenAndMemoryBelowFloor_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Argon2id(new Argon2Parameters { MemoryKiB = 4, Iterations = 1, Parallelism = 4 }, 2);
        });

        Assert.AreEqual("MemoryKiB", ex.ParamName);
    }
}
