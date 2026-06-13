// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class AsymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that disposing an instance twice — and disposing one that was never touched — is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwiceOrOnUntouchedInstance_ShouldNotThrow()
    {
        TAlgorithm used = CreateAlgorithmWithGeneratedKey();
        used.Dispose();
        used.Dispose();

        TAlgorithm untouched = CreateAlgorithm();
        untouched.Dispose();
    }

    /// <summary>
    /// Verifies that the key-management members throw <see cref="ObjectDisposedException" /> after the instance has
    /// been disposed.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeKeyMembersThrowObjectDisposedException()
    {
        TAlgorithm algorithm = CreateAlgorithmWithGeneratedKey();
        var privateKey = ExportPrivateKey(algorithm);
        var publicKey = ExportPublicKey(algorithm);

        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = HasPrivateKey(algorithm); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = HasPublicKey(algorithm); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { GenerateKey(algorithm); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = ExportPrivateKey(algorithm); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = ExportPublicKey(algorithm); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { ImportPrivateKey(algorithm, privateKey); });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { ImportPublicKey(algorithm, publicKey); });
    }
}
