// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2ParametersTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Argon2Parameters" /> validation.
/// </summary>
[TestClass]
public sealed class Argon2ParametersTests
{
    /// <summary>
    /// Verifies that a non-positive iteration count is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenIterationsBelowOne_ShouldThrowArgumentOutOfRangeException()
    {
        Argon2Parameters parameters = new() { Parallelism = 1, MemoryKiB = 64, Iterations = 0 };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(parameters.Validate);

        Assert.AreEqual(nameof(Argon2Parameters.Iterations), ex.ParamName);
    }

    /// <summary>
    /// Verifies that a tag length below the minimum is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenTagLengthBelowMinimum_ShouldThrowArgumentOutOfRangeException()
    {
        Argon2Parameters parameters = new() { Parallelism = 1, MemoryKiB = 64, Iterations = 1, TagLength = 2 };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(parameters.Validate);

        Assert.AreEqual(nameof(Argon2Parameters.TagLength), ex.ParamName);
    }

    /// <summary>
    /// Verifies that an unrecognized version code is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenVersionUnrecognized_ShouldThrowArgumentException()
    {
        Argon2Parameters parameters = new() { Parallelism = 1, MemoryKiB = 64, Iterations = 1, Version = 0x99 };

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(parameters.Validate);

        Assert.AreEqual(nameof(Argon2Parameters.Version), ex.ParamName);
    }

    /// <summary>
    /// Verifies that a memory cost above <see cref="Argon2Parameters.MaxMemoryKiB" /> is rejected, so an untrusted
    /// PHC string cannot drive an unbounded allocation during verification.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Validate_WhenMemoryExceedsMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        Argon2Parameters parameters = new()
        {
            Parallelism = 1,
            Iterations = 1,
            TagLength = 32,
            MemoryKiB = Argon2Parameters.MaxMemoryKiB + 1,
        };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(parameters.Validate);

        Assert.AreEqual(nameof(Argon2Parameters.MemoryKiB), ex.ParamName);
    }
}
