// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.TransformFinalBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" />
    /// still rejects a <see langword="null" /> input buffer with <see cref="ArgumentNullException" />,
    /// distinct from a zero-length input which is permitted (see
    /// <see cref="TransformFinalBlock_WhenEncryptingEmptyInput_ShouldNotThrow" />).
    /// Regression guard for transforms that previously threw <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
    /// </summary>
    /// <param name="answer">
    /// The vector under test, or <see langword="null" /> when the subclass declares no Transform-layer KATs.
    /// </param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformFinalBlock_WhenInputBufferIsNull_ShouldThrowArgumentNullException(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = transform.TransformFinalBlock(null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> accepts
    /// an empty final input span when encrypting. The contract distinguishes between a zero-length
    /// final block (valid — the padding layer emits whatever the scheme requires) and a non-zero
    /// partial block (still invalid).
    /// </summary>
    /// <param name="answer">
    /// The vector under test, or <see langword="null" /> when the subclass declares no Transform-layer KATs.
    /// </param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformFinalBlock_WhenEncryptingEmptyInput_ShouldNotThrow(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        _ = transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> on an
    /// encryptor with the default <see cref="PaddingMode.PKCS7" /> accepts a partial-block input
    /// and pads it out to exactly one full block of ciphertext.
    /// </summary>
    /// <remarks>
    /// Pre-#208 <see cref="BlockCipherTransform.TransformFinalBlock" /> ran an
    /// alignment-multiple validator on the raw input <strong>before</strong> the padding step,
    /// so this call surfaced a <see cref="CryptographicException" /> with a
    /// "block length must be a positive multiple" message — incompatible with the
    /// <see cref="System.Security.Cryptography.CryptoStream.FlushFinalBlock" /> contract that
    /// hands off whatever residual sits in its buffer (typically <c>1..blockSize-1</c> bytes
    /// after the last aligned chunk has flowed through <c>TransformBlock</c>). #208 moved the
    /// alignment check off the encrypt path entirely; this test pins the corrected behaviour.
    /// </remarks>
    [TestMethod]
    public void TransformFinalBlock_WhenInputCountIsNotMultipleOfInputBlockSize_ShouldPadAndEncrypt_fix()
    {
        using var transform = CreateAlgorithm();

        if (transform.InputBlockSize <= 1)
        {
            Assert.Inconclusive("The transform has a one-byte input block size and cannot produce a shorter partial block.");
            return;
        }

        byte[] inputBuffer = new byte[transform.InputBlockSize - 1];

        byte[] cipherText = transform.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);

        Assert.AreEqual(transform.InputBlockSize, cipherText.Length,
            "PKCS7-padded partial-block input must produce exactly one block of ciphertext.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> reads only the
    /// selected input range when <c>inputOffset</c> is non-zero.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformFinalBlock_WhenInputOffsetIsNonZero_ShouldReadOnlySelectedInputRange(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        const int inputOffset = 5;
        byte[] inputBuffer = new byte[inputOffset + answer.Plaintext.Length + 5];
        Buffer.BlockCopy(answer.Plaintext, 0, inputBuffer, inputOffset, answer.Plaintext.Length);

        byte[] actual = transform.TransformFinalBlock(
            inputBuffer,
            inputOffset,
            answer.Plaintext.Length);

        CollectionAssert.AreEqual(
            answer.Ciphertext,
            actual,
            $"TransformFinalBlock did not read the expected input range for vector '{answer.Name}'.");
    }

    /// <summary>
    /// Verifies that a transform which reports <see cref="ICryptoTransform.CanReuseTransform" /> as
    /// <see langword="false" /> rejects a second
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> call after finalisation has completed.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformFinalBlock_WhenCalledAfterTransformFinalBlockAndCanReuseTransformIsFalse_ShouldThrowInvalidOperationException(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        if (transform.CanReuseTransform)
        {
            Assert.Inconclusive("This test only applies to transforms that cannot be reused after TransformFinalBlock.");
            return;
        }

        _ = transform.TransformFinalBlock(answer.Plaintext, 0, answer.Plaintext.Length);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = transform.TransformFinalBlock(answer.Plaintext, 0, answer.Plaintext.Length);
        });
    }
}
