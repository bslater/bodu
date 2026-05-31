// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.TransformFinalBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> accepts an empty
    /// final input span when encrypting — the contract distinguishes between a zero-length final block
    /// (valid; the padding layer emits whatever the scheme requires) and a non-zero partial block (invalid
    /// only when padding is disabled).
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenEncryptingEmptyInput_ShouldNotThrow()
    {
        using TCryptoTransform encryptor = CreateEncryptor();

        _ = encryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> on an encryptor
    /// with the default <see cref="PaddingMode.PKCS7" /> accepts a partial-block input and pads it out to
    /// exactly one full block of ciphertext.
    /// </summary>
    /// <remarks>
    /// Pre-#208 <see cref="BlockCipherTransform.TransformFinalBlock" /> ran an alignment-multiple validator
    /// on the raw input <strong>before</strong> the padding step, so this call surfaced a
    /// <see cref="CryptographicException" /> with a "block length must be a positive multiple" message —
    /// incompatible with the <see cref="System.Security.Cryptography.CryptoStream.FlushFinalBlock" />
    /// contract that hands off whatever residual sits in its buffer (typically <c>1..blockSize-1</c> bytes
    /// after the last aligned chunk has flowed through <c>TransformBlock</c>). #208 moved the alignment
    /// check off the encrypt path entirely; this test pins the corrected behaviour.
    /// </remarks>
    [TestMethod]
    public void TransformFinalBlock_WhenInputCountIsNotMultipleOfInputBlockSize_ShouldPadAndEncrypt()
    {
        using TCryptoTransform transform = CreateAlgorithm();

        if (transform.InputBlockSize <= 1)
        {
            Assert.Inconclusive("The transform has a one-byte input block size and cannot produce a shorter partial block.");
            return;
        }

        var inputBuffer = new byte[transform.InputBlockSize - 1];

        var cipherText = transform.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);

        Assert.AreEqual(transform.InputBlockSize, cipherText.Length,
            "PKCS7-padded partial-block input must produce exactly one block of ciphertext.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> reads only the
    /// selected input range when <c>inputOffset</c> is non-zero — the produced ciphertext matches the result
    /// of finalising the same payload at <c>inputOffset</c> 0.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputOffsetIsNonZero_ShouldReadOnlySelectedInputRange()
    {
        byte[] expected;
        byte[] payload;
        int blockSize;
        using (TCryptoTransform reference = CreateEncryptor())
        {
            blockSize = reference.InputBlockSize;
            payload = BuildIncrementingPlaintext(blockSize);
            expected = reference.TransformFinalBlock(payload, 0, payload.Length);
        }

        using TCryptoTransform encryptor = CreateEncryptor();
        const int inputOffset = 5;
        var inputBuffer = new byte[inputOffset + blockSize + 5];
        Buffer.BlockCopy(payload, 0, inputBuffer, inputOffset, blockSize);

        var actual = encryptor.TransformFinalBlock(inputBuffer, inputOffset, blockSize);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a transform which reports <see cref="ICryptoTransform.CanReuseTransform" /> as
    /// <see langword="false" /> rejects a second
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> call after finalisation has
    /// completed.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenCalledAfterTransformFinalBlockAndCanReuseTransformIsFalse_ShouldThrowExactly()
    {
        using TCryptoTransform encryptor = CreateEncryptor();

        if (encryptor.CanReuseTransform)
        {
            Assert.Inconclusive("This test only applies to transforms that cannot be reused after TransformFinalBlock.");
            return;
        }

        var plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize);
        _ = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        });
    }
}
