// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecretBytesTests.Random.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SecretBytesTests
{
    /// <summary>
    /// Verifies that <see cref="SecretBytes.Random" /> produces an instance of the requested length.
    /// </summary>
    [TestMethod]
    public void Random_WhenLengthIsPositive_ShouldProduceInstanceOfRequestedLength()
    {
        using var secret = SecretBytes.Random(32);

        Assert.AreEqual(32, secret.Length);
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="SecretBytes.Random" /> draws of a collision-safe size differ.
    /// </summary>
    [TestMethod]
    public void Random_WhenDrawnTwice_ShouldProduceDistinctContent()
    {
        using var first = SecretBytes.Random(32);
        using var second = SecretBytes.Random(32);

        Assert.IsFalse(first.FixedTimeEquals(second));
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes.Random" /> throws <see cref="ArgumentOutOfRangeException" /> for zero
    /// and negative lengths.
    /// </summary>
    /// <param name="length">The invalid length under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Random_WhenLengthIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            using var secret = SecretBytes.Random(length);
        });

        Assert.AreEqual("length", ex.ParamName);
    }
}
