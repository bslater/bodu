// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.BlockSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class SkipjackBlockCipherTests
{
    /// <summary>
    /// Verifies that <see cref="SkipjackBlockCipher.BlockSize" /> remains queryable after disposal
    /// since it returns the compile-time constant <see cref="SkipjackBlockCipher.BlockBytes" />
    /// rather than any state that gets cleared on disposal.
    /// </summary>
    [TestMethod]
    public void BlockSize_WhenAccessedAfterDispose_ShouldReturnConstant()
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);
        cipher.Dispose();

        Assert.AreEqual(SkipjackBlockCipher.BlockBytes, cipher.BlockSize);
    }
}
