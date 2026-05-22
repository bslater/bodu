// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── Constructor validation ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="AsconAead128(byte[], byte[])" /> constructor throws
    /// <see cref="ArgumentNullException" /> when the key array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new AsconAead128(null!, ValidNonce);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="AsconAead128(byte[], byte[])" /> constructor throws
    /// <see cref="ArgumentNullException" /> when the nonce array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new AsconAead128(ValidKey, null!);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="AsconAead128(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// constructor throws <see cref="ArgumentException" /> when the key is the wrong size.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsWrongSize_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AsconAead128(new byte[15], ValidNonce);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="AsconAead128(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// constructor throws <see cref="ArgumentException" /> when the nonce is the wrong size.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonceIsWrongSize_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new AsconAead128(ValidKey, new byte[15]);
        });
    }
}
