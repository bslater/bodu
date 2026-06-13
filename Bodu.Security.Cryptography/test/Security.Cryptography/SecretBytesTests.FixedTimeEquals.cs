// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecretBytesTests.FixedTimeEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SecretBytesTests
{
    /// <summary>
    /// Verifies that <see cref="SecretBytes.FixedTimeEquals(SecretBytes)" /> returns <see langword="true" /> for
    /// identical content and <see langword="false" /> when content or length differs.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenComparingInstances_ShouldCompareContent()
    {
        using var secret = SecretBytes.CopyFrom([0x01, 0x02, 0x03]);
        using var same = SecretBytes.CopyFrom([0x01, 0x02, 0x03]);
        using var different = SecretBytes.CopyFrom([0x01, 0x02, 0x04]);
        using var shorter = SecretBytes.CopyFrom([0x01, 0x02]);

        Assert.IsTrue(secret.FixedTimeEquals(same));
        Assert.IsFalse(secret.FixedTimeEquals(different));
        Assert.IsFalse(secret.FixedTimeEquals(shorter));
    }

    /// <summary>
    /// Verifies that the span overload of <see cref="SecretBytes.FixedTimeEquals(ReadOnlySpan{byte})" /> compares
    /// the secret against raw bytes.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_ForSpanOverload_ShouldCompareAgainstRawBytes()
    {
        using var secret = SecretBytes.CopyFrom([0x10, 0x20, 0x30]);

        Assert.IsTrue(secret.FixedTimeEquals(new byte[] { 0x10, 0x20, 0x30 }));
        Assert.IsFalse(secret.FixedTimeEquals(new byte[] { 0x10, 0x20, 0x31 }));
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes.FixedTimeEquals(SecretBytes)" /> throws
    /// <see cref="ArgumentNullException" /> when the other instance is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        using var secret = SecretBytes.CopyFrom([0x01]);

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = secret.FixedTimeEquals((SecretBytes)null!);
        });

        Assert.AreEqual("other", ex.ParamName);
    }
}
