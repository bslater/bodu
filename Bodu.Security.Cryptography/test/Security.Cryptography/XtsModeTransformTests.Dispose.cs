// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class XtsModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="XtsModeTransform.Dispose" /> can be invoked without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledOnce_ShouldNotThrow()
    {
        var dataCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize);
        var tweakCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize, xorMask: 0x55);
        byte[] tweak = new byte[ExpectedBlockSize];
        var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);

        transform.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="XtsModeTransform.Dispose" /> is idempotent and the second call is a no-op.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var dataCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize);
        var tweakCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize, xorMask: 0x55);
        byte[] tweak = new byte[ExpectedBlockSize];
        var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);

        transform.Dispose();
        transform.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="XtsModeTransform.Transform" /> throws <see cref="ObjectDisposedException" /> after
    /// disposal, so a disposed transform cannot silently continue from its zeroed tweak state.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var dataCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize);
        var tweakCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize, xorMask: 0x55);
        byte[] tweak = new byte[ExpectedBlockSize];
        var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        transform.Dispose();

        byte[] input = new byte[ExpectedBlockSize];
        byte[] output = new byte[ExpectedBlockSize];

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            transform.Transform(input, output, encrypt: true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="XtsModeTransform" /> constructor throws <see cref="ArgumentException" /> when the
    /// cipher block size is not 128 bits, since the GF(2^128) tweak reduction is defined only for 128-bit blocks.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlockSizeIsNot128_ShouldThrowExactly()
    {
        // MonitoringBlockCipher(8) has a 64-bit block.
        var dataCipher = new MonitoringBlockCipher(blockSize: 8);
        var tweakCipher = new MonitoringBlockCipher(blockSize: 8, xorMask: 0x55);
        byte[] tweak = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="XtsModeTransform" /> constructor throws <see cref="ArgumentException" /> when the
    /// tweak cipher block size differs from the data cipher block size.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTweakCipherBlockSizeDiffers_ShouldThrowExactly()
    {
        var dataCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize);
        var tweakCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize + 8, xorMask: 0x55);
        byte[] tweak = new byte[ExpectedBlockSize];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="XtsModeTransform" /> constructor throws <see cref="ArgumentException" /> when the
    /// tweak length differs from the cipher block size.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTweakLengthMismatchesBlockSize_ShouldThrowExactly()
    {
        var dataCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize);
        var tweakCipher = new MonitoringBlockCipher(blockSize: ExpectedBlockSize, xorMask: 0x55);
        byte[] tweak = new byte[ExpectedBlockSize - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        });
    }
}
