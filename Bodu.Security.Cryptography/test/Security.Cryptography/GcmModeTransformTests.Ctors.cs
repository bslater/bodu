// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class GcmModeTransformTests
{
    /// <summary>
    /// Verifies that a nonce longer than 96 bits is rejected.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonceIsTooLong_ShouldThrowExactly()
    {
        using IBlockCipher cipher = new MonitoringBlockCipher(ExpectedBlockSize);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new GcmModeTransform(cipher, new byte[NonceSizeBytes + 1]);
        });

        Assert.AreEqual("nonce", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a block-sized 128-bit value is rejected by the public GCM nonce constructor.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonceIsBlockSized_ShouldThrowExactly()
    {
        using IBlockCipher cipher = new MonitoringBlockCipher(ExpectedBlockSize);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new GcmModeTransform(cipher, new byte[ExpectedBlockSize]);
        });

        Assert.AreEqual("nonce", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the span-based constructor succeeds when supplied with a 96-bit GCM nonce.
    /// </summary>
    [TestMethod]
    public void Ctor_WithNonceSpan_WhenArgumentsAreValid_ShouldSucceed()
    {
        using IBlockCipher cipher = new MonitoringBlockCipher(ExpectedBlockSize);

        using var transform = new GcmModeTransform(cipher, new byte[NonceSizeBytes].AsSpan());

        Assert.AreEqual(128, transform.TagSize);
    }

    /// <summary>
    /// Verifies that the span-based constructor rejects a non-96-bit nonce.
    /// </summary>
    [TestMethod]
    public void Ctor_WithNonceSpan_WhenNonceLengthIsInvalid_ShouldThrowExactly()
    {
        using IBlockCipher cipher = new MonitoringBlockCipher(ExpectedBlockSize);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new GcmModeTransform(cipher, new byte[ExpectedBlockSize].AsSpan());
        });

        Assert.AreEqual("nonce", ex.ParamName);
    }
}