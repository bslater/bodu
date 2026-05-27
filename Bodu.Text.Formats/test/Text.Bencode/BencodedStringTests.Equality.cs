// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedStringTests.Equality.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodedStringTests
{

    /// <summary>
    /// Verifies that <see cref="object.GetHashCode" /> is consistent with the equality contract: equal byte
    /// content yields equal hashes.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenSameBytes_ShouldReturnSameHash()
    {
        BencodedString a = new([0xDE, 0xAD]);
        BencodedString b = new([0xDE, 0xAD]);

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that two <see cref="BencodedString" /> instances with different byte content compare unequal.
    /// </summary>
    [TestMethod]
    public void TypedEquals_WhenDifferentBytes_ShouldReturnFalse()
    {
        BencodedString a = new([0xDE, 0xAD]);
        BencodedString b = new([0xBE, 0xEF]);

        Assert.IsFalse(a.Equals(b));
    }

    /// <summary>
    /// Verifies that comparing a <see cref="BencodedString" /> to a <see langword="null" /> reference via the
    /// typed equals overload returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TypedEquals_WhenOtherIsNull_ShouldReturnFalse()
    {
        BencodedString a = new([0xDE, 0xAD]);

        Assert.IsFalse(a.Equals((BencodedString?)null));
    }
    /// <summary>
    /// Verifies that two <see cref="BencodedString" /> instances with identical byte content compare equal via the
    /// typed <see cref="IEquatable{T}.Equals" /> overload.
    /// </summary>
    [TestMethod]
    public void TypedEquals_WhenSameBytes_ShouldReturnTrue()
    {
        BencodedString a = new([0xDE, 0xAD]);
        BencodedString b = new([0xDE, 0xAD]);

        Assert.IsTrue(a.Equals(b));
    }

}
