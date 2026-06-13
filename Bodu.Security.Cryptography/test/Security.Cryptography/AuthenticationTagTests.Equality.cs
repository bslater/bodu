// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AuthenticationTagTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class AuthenticationTagTests
{
    /// <summary>
    /// Verifies that two tags created from identical bytes compare equal through
    /// <see cref="AuthenticationTag.Equals(AuthenticationTag)" />, the equality operators, and
    /// <see cref="AuthenticationTag.GetHashCode" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesAreIdentical_ShouldBeEqualWithMatchingHashCodes()
    {
        var left = AuthenticationTag.FromBytes([0x01, 0x02, 0x03]);
        var right = AuthenticationTag.FromBytes([0x01, 0x02, 0x03]);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that tags with different bytes compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesDiffer_ShouldBeUnequal()
    {
        var left = AuthenticationTag.FromBytes([0x01, 0x02, 0x03]);
        var right = AuthenticationTag.FromBytes([0x01, 0x02, 0x04]);

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that the default instance compares equal to a tag created from an empty span.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDefaultAndEmptyFromBytes_ShouldBeEqual()
    {
        Assert.IsTrue(default(AuthenticationTag) == AuthenticationTag.FromBytes([]));
    }
}
