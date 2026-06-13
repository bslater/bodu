// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureValueTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SignatureValueTests
{
    /// <summary>
    /// Verifies that two signatures with the same format and identical bytes compare equal through
    /// <see cref="SignatureValue.Equals(SignatureValue)" />, the equality operators, and
    /// <see cref="SignatureValue.GetHashCode" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenFormatAndBytesAreIdentical_ShouldBeEqualWithMatchingHashCodes()
    {
        var left = SignatureValue.FromBytes([0x01, 0x02, 0x03], SignatureFormat.Der);
        var right = SignatureValue.FromBytes([0x01, 0x02, 0x03], SignatureFormat.Der);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that signatures with identical bytes but different recorded formats compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenFormatsDiffer_ShouldBeUnequal()
    {
        var left = SignatureValue.FromBytes([0x01, 0x02, 0x03], SignatureFormat.Der);
        var right = SignatureValue.FromBytes([0x01, 0x02, 0x03], SignatureFormat.P1363);

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that signatures with the same format but different bytes compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBytesDiffer_ShouldBeUnequal()
    {
        var left = SignatureValue.FromBytes([0x01, 0x02, 0x03], SignatureFormat.Raw);
        var right = SignatureValue.FromBytes([0x01, 0x02, 0x04], SignatureFormat.Raw);

        Assert.IsFalse(left.Equals(right));
    }
}
