// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipherTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class TwofishBlockCipherTests
{
    /// <summary>
    /// Verifies that constructing <see cref="TwofishBlockCipher" /> with a key length that is not 16, 24, or
    /// 32 bytes throws <see cref="ArgumentException" />. The exception type is Bodu-specific and varies across
    /// cipher families (AES throws <see cref="System.Security.Cryptography.CryptographicException" />), so this
    /// negative-path coverage stays on the concrete test class rather than the shared base.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(23)]
    [DataRow(25)]
    [DataRow(31)]
    [DataRow(33)]
    public void Ctor_WhenKeyLengthIsInvalid_ShouldThrowExactly(int keyLength)
    {
        var key = new byte[keyLength];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using var _ = new TwofishBlockCipher(key);
        });
    }
}
