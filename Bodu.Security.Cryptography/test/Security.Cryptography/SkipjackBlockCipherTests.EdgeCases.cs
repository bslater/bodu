// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="SkipjackBlockCipher" /> for unexpected exceptions when its public surface is
/// exercised outside of expected usage — wrong-sized key on construction, wrong-sized input/output
/// spans on encrypt and decrypt, and idempotent disposal. Complements the generic
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> base coverage.
/// </summary>
internal sealed partial class SkipjackBlockCipherTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="SkipjackBlockCipher" /> with a key whose length is not
    /// exactly <see cref="SkipjackBlockCipher.KeySize" /> throws <see cref="ArgumentException" /> rather
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

    /// <summary>
    /// Verifies that <see cref="SkipjackBlockCipher.Encrypt(ReadOnlySpan{byte}, Span{byte})" /> with
    /// an input span whose length is not exactly <see cref="SkipjackBlockCipher.BlockBytes" /> throws
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
    /// an output span whose length is not exactly <see cref="SkipjackBlockCipher.BlockBytes" /> throws
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

    /// <summary>
    /// Verifies that <see cref="SkipjackBlockCipher.Decrypt(ReadOnlySpan{byte}, Span{byte})" /> with
    /// an input span whose length is not exactly <see cref="SkipjackBlockCipher.BlockBytes" /> throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(16)]
    public void Decrypt_WhenInputIsWrongSize_ShouldThrowExactly(int inputLength)
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Decrypt(new byte[inputLength], new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SkipjackBlockCipher.Decrypt(ReadOnlySpan{byte}, Span{byte})" /> with
    /// an output span whose length is not exactly <see cref="SkipjackBlockCipher.BlockBytes" /> throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(16)]
    public void Decrypt_WhenOutputIsWrongSize_ShouldThrowExactly(int outputLength)
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            cipher.Decrypt(new byte[8], new byte[outputLength]);
        });
    }

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
