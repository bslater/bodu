// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonceTests.Random.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class NonceTests
{
    /// <summary>
    /// Verifies that <see cref="Nonce.Random" /> produces a value of the requested length.
    /// </summary>
    [TestMethod]
    public void Random_WhenLengthIsPositive_ShouldProduceValueOfRequestedLength()
    {
        var nonce = Nonce.Random(24);

        Assert.AreEqual(24, nonce.Length);
        Assert.IsFalse(nonce.IsEmpty);
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="Nonce.Random" /> draws of a collision-safe size differ.
    /// </summary>
    [TestMethod]
    public void Random_WhenDrawnTwice_ShouldProduceDistinctValues()
    {
        var first = Nonce.Random(24);
        var second = Nonce.Random(24);

        Assert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="Nonce.Random" /> throws <see cref="ArgumentOutOfRangeException" /> for zero and
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
            _ = Nonce.Random(length);
        });

        Assert.AreEqual("length", ex.ParamName);
    }
}
