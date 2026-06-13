// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SaltTests.Random.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SaltTests
{
    /// <summary>
    /// Verifies that <see cref="Salt.Random" /> produces a value of the requested length.
    /// </summary>
    [TestMethod]
    public void Random_WhenLengthIsPositive_ShouldProduceValueOfRequestedLength()
    {
        var salt = Salt.Random(16);

        Assert.AreEqual(16, salt.Length);
        Assert.IsFalse(salt.IsEmpty);
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="Salt.Random" /> draws of a collision-safe size differ.
    /// </summary>
    [TestMethod]
    public void Random_WhenDrawnTwice_ShouldProduceDistinctValues()
    {
        var first = Salt.Random(16);
        var second = Salt.Random(16);

        Assert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="Salt.Random" /> throws <see cref="ArgumentOutOfRangeException" /> for zero and
    /// negative lengths.
    /// </summary>
    /// <param name="length">The invalid length under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Random_WhenLengthIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Salt.Random(length);
        });

        Assert.AreEqual("length", ex.ParamName);
    }
}
