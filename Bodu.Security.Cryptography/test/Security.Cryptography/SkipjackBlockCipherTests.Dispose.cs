// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class SkipjackBlockCipherTests
{
    /// <summary>
    /// Verifies that calling <see cref="SkipjackBlockCipher.Dispose" /> twice is idempotent and
    /// does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);
        cipher.Dispose();

        try
        {
            cipher.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on SkipjackBlockCipher threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
