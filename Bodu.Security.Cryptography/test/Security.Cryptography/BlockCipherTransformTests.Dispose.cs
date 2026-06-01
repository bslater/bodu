// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.Dispose.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that disposing the encryptor twice does not throw, per the standard
    /// <see cref="System.IDisposable" /> idempotence contract.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        TCryptoTransform encryptor = CreateEncryptor();

        encryptor.Dispose();
        encryptor.Dispose();
    }
}
