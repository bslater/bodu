// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecretBytesTests.CopyFrom.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SecretBytesTests
{
    /// <summary>
    /// Verifies that <see cref="SecretBytes.CopyFrom" /> copies the input so later mutation of the source buffer
    /// does not change the secret.
    /// </summary>
    [TestMethod]
    public void CopyFrom_WhenSourceMutatedAfterwards_ShouldNotAffectSecret()
    {
        var source = new byte[] { 0x01, 0x02, 0x03 };
        using var secret = SecretBytes.CopyFrom(source);

        source[0] = 0xFF;

        Assert.IsTrue(secret.FixedTimeEquals(new byte[] { 0x01, 0x02, 0x03 }));
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes.CopyFrom" /> over an empty span produces an empty instance.
    /// </summary>
    [TestMethod]
    public void CopyFrom_WhenInputIsEmpty_ShouldProduceEmptyInstance()
    {
        using var secret = SecretBytes.CopyFrom([]);

        Assert.AreEqual(0, secret.Length);
        Assert.IsTrue(secret.AsSpan().IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes.AsSpan" /> and <see cref="SecretBytes.ToArray" /> expose the copied
    /// bytes, and that mutating the array returned by <see cref="SecretBytes.ToArray" /> does not affect the secret.
    /// </summary>
    [TestMethod]
    public void CopyFrom_WhenInputIsNonEmpty_ShouldExposeCopiedBytes()
    {
        var source = new byte[] { 0x0A, 0x0B, 0x0C };
        using var secret = SecretBytes.CopyFrom(source);

        Assert.IsTrue(secret.AsSpan().SequenceEqual(source));

        var copy = secret.ToArray();
        CollectionAssert.AreEqual(source, copy);

        copy[0] = 0xFF;
        Assert.IsTrue(secret.FixedTimeEquals(source));
    }
}
