// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the disposal behavior of <see cref="X25519" />.
/// </summary>
public partial class X25519Tests
{
    /// <summary>
    /// Verifies that members throw <see cref="ObjectDisposedException" /> after the instance has been disposed.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeSubsequentMemberAccessThrowObjectDisposedException()
    {
        var algorithm = new X25519();
        algorithm.GenerateKey();
        var peer = algorithm.ExportPublicKey();

        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.HasPrivateKey; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { algorithm.GenerateKey(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.ExportPrivateKey(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.ExportPublicKey(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.DeriveSharedSecret(peer); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { algorithm.ImportPrivateKey(new byte[X25519.KeySizeInBytes]); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { algorithm.ImportPublicKey(new byte[X25519.KeySizeInBytes]); });
    }

    /// <summary>
    /// Verifies that calling <see cref="IDisposable.Dispose" /> a second time is a harmless no-op.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new X25519();
        algorithm.GenerateKey();

        algorithm.Dispose();
        algorithm.Dispose();
    }
}
