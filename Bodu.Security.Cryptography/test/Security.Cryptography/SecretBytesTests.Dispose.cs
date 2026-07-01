// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecretBytesTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SecretBytesTests
{
    /// <summary>
    /// Verifies that <see cref="SecretBytes.Dispose" /> is idempotent.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var secret = SecretBytes.CopyFrom([0x01, 0x02]);

        secret.Dispose();
        secret.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes.AsSpan" /> throws <see cref="ObjectDisposedException" /> after disposal.
    /// </summary>
    [TestMethod]
    public void AsSpan_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var secret = SecretBytes.CopyFrom([0x01]);
        secret.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = secret.AsSpan().Length;
        });
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes.ToArray" /> throws <see cref="ObjectDisposedException" /> after disposal.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var secret = SecretBytes.CopyFrom([0x01]);
        secret.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = secret.ToArray();
        });
    }

    /// <summary>
    /// Verifies that both <see cref="SecretBytes.FixedTimeEquals(SecretBytes)" /> overloads throw
    /// <see cref="ObjectDisposedException" /> when either operand has been disposed.
    /// </summary>
    [TestMethod]
    public void FixedTimeEquals_WhenEitherOperandDisposed_ShouldThrowObjectDisposedException()
    {
        var disposed = SecretBytes.CopyFrom([0x01]);
        disposed.Dispose();
        using var live = SecretBytes.CopyFrom([0x01]);

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = disposed.FixedTimeEquals(live);
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = live.FixedTimeEquals(disposed);
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = disposed.FixedTimeEquals(new byte[] { 0x01 });
        });
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes.Clear" /> throws <see cref="ObjectDisposedException" /> after disposal.
    /// </summary>
    [TestMethod]
    public void Clear_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var secret = SecretBytes.CopyFrom([0x01]);
        secret.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(secret.Clear);
    }

    /// <summary>
    /// Verifies that <see cref="SecretBytes.Length" /> and <see cref="SecretBytes.ToString" /> remain usable after
    /// disposal, because the length of a secret is not itself secret.
    /// </summary>
    [TestMethod]
    public void LengthAndToString_WhenDisposed_ShouldRemainUsable()
    {
        var secret = SecretBytes.CopyFrom([0x01, 0x02, 0x03]);
        secret.Dispose();

        Assert.AreEqual(3, secret.Length);
        Assert.AreEqual("SecretBytes (Length = 3)", secret.ToString());
    }
}
