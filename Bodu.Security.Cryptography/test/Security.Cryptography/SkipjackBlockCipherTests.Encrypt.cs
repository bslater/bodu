// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class SkipjackBlockCipherTests
{
    /// <summary>
    /// Verifies that <see cref="SkipjackBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> with
    /// an input span whose length is not exactly <see cref="SkipjackBlockCipher.BlockSize" /> throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(16)]
    public void Encrypt_WhenInputIsWrongSize_ShouldThrowExactly(int inputLength)
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Encrypt(new byte[inputLength], new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SkipjackBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> with
    /// an output span whose length is not exactly <see cref="SkipjackBlockCipher.BlockSize" /> throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(16)]
    public void Encrypt_WhenOutputIsWrongSize_ShouldThrowExactly(int outputLength)
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Encrypt(new byte[8], new byte[outputLength]);
        });
    }
}
