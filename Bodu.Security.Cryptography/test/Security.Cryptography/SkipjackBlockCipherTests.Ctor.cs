// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class SkipjackBlockCipherTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="SkipjackBlockCipher" /> with a key whose length is not
    /// exactly <see cref="SkipjackBlockCipher.KeySize" /> / 8 bytes throws <see cref="ArgumentException" /> rather
    /// than <see cref="IndexOutOfRangeException" /> from the key-schedule loop.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(9)]
    [DataRow(11)]
    [DataRow(16)]
    [DataRow(32)]
    [DataRow(64)]
    public void Ctor_WhenKeyLengthIsInvalid_ShouldThrowExactly(int keyLength)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new SkipjackBlockCipher(new byte[keyLength]);
        });
    }
}
