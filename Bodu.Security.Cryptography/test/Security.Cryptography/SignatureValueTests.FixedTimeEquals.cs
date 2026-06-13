// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureValueTests.FixedTimeEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SignatureValueTests
{
    /// <summary>
    /// Verifies that <see cref="SignatureValue.FixedTimeEquals" /> returns <see langword="true" /> for identical
    /// bytes even when the recorded formats differ.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenBytesMatchAndFormatsDiffer_ShouldReturnTrue()
    {
        var left = SignatureValue.FromBytes([0x01, 0x02], SignatureFormat.Der);
        var right = SignatureValue.FromBytes([0x01, 0x02], SignatureFormat.P1363);

        Assert.IsTrue(left.FixedTimeEquals(right));
    }

    /// <summary>
    /// Verifies that <see cref="SignatureValue.FixedTimeEquals" /> returns <see langword="false" /> when any byte
    /// differs.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenBytesDiffer_ShouldReturnFalse()
    {
        var left = SignatureValue.FromBytes([0x01, 0x02], SignatureFormat.Raw);
        var right = SignatureValue.FromBytes([0x01, 0x03], SignatureFormat.Raw);

        Assert.IsFalse(left.FixedTimeEquals(right));
    }

    /// <summary>
    /// Verifies that <see cref="SignatureValue.FixedTimeEquals" /> returns <see langword="false" /> when the
    /// lengths differ.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenLengthsDiffer_ShouldReturnFalse()
    {
        var left = SignatureValue.FromBytes([0x01, 0x02], SignatureFormat.Raw);
        var right = SignatureValue.FromBytes([0x01, 0x02, 0x03], SignatureFormat.Raw);

        Assert.IsFalse(left.FixedTimeEquals(right));
    }
}
