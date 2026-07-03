// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonceTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class NonceTests
{
    /// <summary>
    /// Verifies that two nonces created from identical bytes compare equal through
    /// <see cref="Nonce.Equals(Nonce)" />, the equality operators, and <see cref="Nonce.GetHashCode" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesAreIdentical_ShouldBeEqualWithMatchingHashCodes()
    {
        var left = Nonce.FromBytes([0x01, 0x02, 0x03]);
        var right = Nonce.FromBytes([0x01, 0x02, 0x03]);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that nonces with different bytes compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesDiffer_ShouldBeUnequal()
    {
        var left = Nonce.FromBytes([0x01, 0x02, 0x03]);
        var right = Nonce.FromBytes([0x01, 0x02, 0x04]);

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that the default instance compares equal to a nonce created from an empty span.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDefaultAndEmptyFromBytes_ShouldBeEqual()
    {
        Assert.IsTrue(default(Nonce) == Nonce.FromBytes([]));
    }

    /// <summary>
    /// Verifies that nonces of different lengths compare unequal, pinning that the constant-time comparison returns
    /// <see langword="false" /> on a length mismatch.
    /// </summary>
    [TestMethod]
    public void Equals_WhenLengthsDiffer_ShouldBeUnequal()
    {
        var left = Nonce.FromBytes([0x01, 0x02, 0x03]);
        var right = Nonce.FromBytes([0x01, 0x02, 0x03, 0x04]);

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that the default (empty) instance compares unequal to a non-empty nonce.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDefaultAndNonEmpty_ShouldBeUnequal()
    {
        Assert.IsFalse(default(Nonce) == Nonce.FromBytes([0x01]));
        Assert.IsTrue(default(Nonce) != Nonce.FromBytes([0x01]));
    }
}
