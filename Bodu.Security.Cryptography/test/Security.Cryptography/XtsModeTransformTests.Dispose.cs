// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        var tweak = new byte[ExpectedBlockSize];
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
        var tweak = new byte[ExpectedBlockSize];
        var transform = new XtsModeTransform(dataCipher, tweakCipher, tweak);

        transform.Dispose();
        transform.Dispose();
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
        var tweak = new byte[ExpectedBlockSize];

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
        var tweak = new byte[ExpectedBlockSize - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XtsModeTransform(dataCipher, tweakCipher, tweak);
        });
    }
}
