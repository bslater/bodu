// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SaltTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SaltTests
{
    /// <summary>
    /// Verifies that two salts created from identical bytes compare equal through
    /// <see cref="Salt.Equals(Salt)" />, the equality operators, and <see cref="Salt.GetHashCode" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesAreIdentical_ShouldBeEqualWithMatchingHashCodes()
    {
        var left = Salt.FromBytes([0x01, 0x02, 0x03]);
        var right = Salt.FromBytes([0x01, 0x02, 0x03]);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that salts with different bytes compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesDiffer_ShouldBeUnequal()
    {
        var left = Salt.FromBytes([0x01, 0x02, 0x03]);
        var right = Salt.FromBytes([0x01, 0x02, 0x04]);

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that the default instance compares equal to a salt created from an empty span.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDefaultAndEmptyFromBytes_ShouldBeEqual()
    {
        Assert.IsTrue(default(Salt) == Salt.FromBytes([]));
    }

    /// <summary>
    /// Verifies that salts of different lengths compare unequal, pinning that the constant-time comparison returns
    /// <see langword="false" /> on a length mismatch.
    /// </summary>
    [TestMethod]
    public void Equals_WhenLengthsDiffer_ShouldBeUnequal()
    {
        var left = Salt.FromBytes([0x01, 0x02, 0x03]);
        var right = Salt.FromBytes([0x01, 0x02, 0x03, 0x04]);

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that the default (empty) instance compares unequal to a non-empty salt.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDefaultAndNonEmpty_ShouldBeUnequal()
    {
        Assert.IsFalse(default(Salt) == Salt.FromBytes([0x01]));
        Assert.IsTrue(default(Salt) != Salt.FromBytes([0x01]));
    }
}
