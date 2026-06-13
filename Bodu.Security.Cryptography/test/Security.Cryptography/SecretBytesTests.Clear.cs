// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecretBytesTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SecretBytesTests
{
    /// <summary>
    /// Verifies that <see cref="SecretBytes.Clear" /> zeroes the content while leaving the instance usable at its
    /// original length.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalled_ShouldZeroContentAndRemainUsable()
    {
        using var secret = SecretBytes.CopyFrom([0x01, 0x02, 0x03]);

        secret.Clear();

        Assert.AreEqual(3, secret.Length);
        Assert.IsTrue(secret.FixedTimeEquals(new byte[] { 0x00, 0x00, 0x00 }));
    }

    /// <summary>
    /// Verifies that a span obtained from <see cref="SecretBytes.AsSpan" /> observes the zeroing performed by
    /// <see cref="SecretBytes.Clear" />, because it aliases the live internal buffer.
    /// </summary>
    [TestMethod]
    public void Clear_WhenSpanObtainedBeforehand_ShouldBeObservedThroughSpan()
    {
        using var secret = SecretBytes.CopyFrom([0xAA, 0xBB]);
        var span = secret.AsSpan();

        secret.Clear();

        Assert.AreEqual(0x00, span[0]);
        Assert.AreEqual(0x00, span[1]);
    }
}
