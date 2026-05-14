// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfbModeTransformTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class OfbModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="OfbModeTransform.Dispose" /> can be invoked without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledOnce_ShouldNotThrow()
    {
        var cipher = new MonitoringBlockCipher(blockSize: 8);
        var iv = new byte[cipher.BlockSize / 8];
        var transform = new OfbModeTransform(cipher, iv);

        transform.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="OfbModeTransform.Dispose" /> is idempotent and the second call is a no-op.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var cipher = new MonitoringBlockCipher(blockSize: 8);
        var iv = new byte[cipher.BlockSize / 8];
        var transform = new OfbModeTransform(cipher, iv);

        transform.Dispose();
        transform.Dispose();
    }
}
