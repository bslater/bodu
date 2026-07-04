// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class HashValueTests
{
    /// <summary>
    /// Verifies that two hash values created from identical bytes compare equal through
    /// <see cref="HashValue.Equals(HashValue)" />, the equality operators, and <see cref="HashValue.GetHashCode" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesAreIdentical_ShouldBeEqualWithMatchingHashCodes()
    {
        var left = HashValue.FromBytes([0x01, 0x02, 0x03]);
        var right = HashValue.FromBytes([0x01, 0x02, 0x03]);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that hash values with different bytes compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesDiffer_ShouldBeUnequal()
    {
        var left = HashValue.FromBytes([0x01, 0x02, 0x03]);
        var right = HashValue.FromBytes([0x01, 0x02, 0x04]);

        Assert.IsFalse(left.Equals(right));
        Assert.IsFalse(left == right);
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that the default instance compares equal to a value created from an empty span.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDefaultAndEmptyFromBytes_ShouldBeEqual()
    {
        Assert.IsTrue(default(HashValue) == HashValue.FromBytes([]));
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.Equals(object)" /> returns <see langword="false" /> for
    /// <see langword="null" /> and for objects of a different type.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedToNullOrDifferentType_ShouldReturnFalse()
    {
        var hash = HashValue.FromBytes([0x01]);

        Assert.IsFalse(hash.Equals(null));
        Assert.IsFalse(hash.Equals("01"));
    }

    /// <summary>
    /// Verifies that hash values of different lengths compare unequal, pinning that the constant-time comparison
    /// returns <see langword="false" /> on a length mismatch (the length is not concealed).
    /// </summary>
    [TestMethod]
    public void Equals_WhenLengthsDiffer_ShouldBeUnequal()
    {
        var left = HashValue.FromBytes([0x01, 0x02, 0x03]);
        var right = HashValue.FromBytes([0x01, 0x02, 0x03, 0x04]);

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that the default (empty) instance compares unequal to a non-empty hash value.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDefaultAndNonEmpty_ShouldBeUnequal()
    {
        Assert.IsFalse(default(HashValue) == HashValue.FromBytes([0x01]));
        Assert.IsTrue(default(HashValue) != HashValue.FromBytes([0x01]));
    }
}
