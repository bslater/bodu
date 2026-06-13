// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the disposal behavior of <see cref="Ed25519" />.
/// </summary>
public partial class Ed25519Tests
{
    /// <summary>
    /// Verifies that members throw <see cref="ObjectDisposedException" /> after the instance has been disposed.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeSubsequentMemberAccessThrowObjectDisposedException()
    {
        var algorithm = new Ed25519();
        algorithm.GenerateKey();
        var message = new byte[] { 1 };
        var signature = algorithm.SignData(message);

        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.HasPrivateKey; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { algorithm.GenerateKey(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.ExportPrivateKey(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.ExportPublicKey(); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.SignData(message); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = algorithm.VerifyData(message, signature); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { algorithm.ImportPrivateKey(new byte[Ed25519.PrivateKeySizeInBytes]); });
    }

    /// <summary>
    /// Verifies that calling <see cref="IDisposable.Dispose" /> a second time is a harmless no-op.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new Ed25519();
        algorithm.GenerateKey();

        algorithm.Dispose();
        algorithm.Dispose();
    }
}
