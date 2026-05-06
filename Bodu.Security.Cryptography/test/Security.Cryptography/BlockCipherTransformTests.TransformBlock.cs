// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.TransformBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that the transform produced by <see cref="CreateTransformForKnownAnswer" /> for encryption
    /// returns the expected number of bytes written when processing <see cref="BlockCipherKnownAnswer.Plaintext" />
    /// through <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" />.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenEncryptingKnownAnswer_ShouldReturnCiphertextByteCount(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);
        byte[] outputBuffer = new byte[answer.Ciphertext.Length];

        int written = transform.TransformBlock(
            answer.Plaintext,
            0,
            answer.Plaintext.Length,
            outputBuffer,
            0);

        Assert.AreEqual(answer.Ciphertext.Length, written);
    }

    /// <summary>
    /// Verifies that the transform produced by <see cref="CreateTransformForKnownAnswer" /> for encryption
    /// writes <see cref="BlockCipherKnownAnswer.Ciphertext" /> to the caller-supplied output buffer when
    /// processing <see cref="BlockCipherKnownAnswer.Plaintext" /> through
    /// <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" />.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenEncryptingKnownAnswer_ShouldWriteExpectedCiphertext(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);
        byte[] outputBuffer = new byte[answer.Ciphertext.Length];

        _ = transform.TransformBlock(
            answer.Plaintext,
            0,
            answer.Plaintext.Length,
            outputBuffer,
            0);

        CollectionAssert.AreEqual(
            answer.Ciphertext,
            outputBuffer,
            $"TransformBlock encrypt KAT mismatch for vector '{answer.Name}'.");
    }

    /// <summary>
    /// Verifies that the transform produced by <see cref="CreateTransformForKnownAnswer" /> for decryption
    /// returns the expected number of bytes written when processing <see cref="BlockCipherKnownAnswer.Ciphertext" />
    /// through <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" />.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenDecryptingKnownAnswer_ShouldReturnPlaintextByteCount(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: false);
        byte[] outputBuffer = new byte[answer.Plaintext.Length];

        int written = transform.TransformBlock(
            answer.Ciphertext,
            0,
            answer.Ciphertext.Length,
            outputBuffer,
            0);

        Assert.AreEqual(answer.Plaintext.Length, written);
    }

    /// <summary>
    /// Verifies that the transform produced by <see cref="CreateTransformForKnownAnswer" /> for decryption
    /// writes <see cref="BlockCipherKnownAnswer.Plaintext" /> to the caller-supplied output buffer when
    /// processing <see cref="BlockCipherKnownAnswer.Ciphertext" /> through
    /// <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" />.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenDecryptingKnownAnswer_ShouldWriteExpectedPlaintext(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: false);
        byte[] outputBuffer = new byte[answer.Plaintext.Length];

        _ = transform.TransformBlock(
            answer.Ciphertext,
            0,
            answer.Ciphertext.Length,
            outputBuffer,
            0);

        CollectionAssert.AreEqual(
            answer.Plaintext,
            outputBuffer,
            $"TransformBlock decrypt KAT mismatch for vector '{answer.Name}'.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> returns the
    /// expected number of bytes written when encryption writes to a non-zero output offset.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldReturnCiphertextByteCount(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        const int outputOffset = 3;
        byte[] outputBuffer = new byte[outputOffset + answer.Ciphertext.Length + 3];

        int written = transform.TransformBlock(
            answer.Plaintext,
            0,
            answer.Plaintext.Length,
            outputBuffer,
            outputOffset);

        Assert.AreEqual(answer.Ciphertext.Length, written);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> writes the
    /// expected ciphertext at the requested non-zero output offset.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldWriteExpectedCiphertextAtOffset(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        const int outputOffset = 3;
        byte[] outputBuffer = new byte[outputOffset + answer.Ciphertext.Length + 3];

        _ = transform.TransformBlock(
            answer.Plaintext,
            0,
            answer.Plaintext.Length,
            outputBuffer,
            outputOffset);

        byte[] actual = Slice(outputBuffer, outputOffset, answer.Ciphertext.Length);

        CollectionAssert.AreEqual(
            answer.Ciphertext,
            actual,
            $"TransformBlock did not write the expected ciphertext at output offset {outputOffset} for vector '{answer.Name}'.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> does not
    /// modify bytes before the requested non-zero output offset.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldNotModifyBytesBeforeOutputOffset(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        const int outputOffset = 3;
        const byte sentinel = 0xA5;

        byte[] outputBuffer = new byte[outputOffset + answer.Ciphertext.Length + 3];
        Array.Fill(outputBuffer, sentinel);

        _ = transform.TransformBlock(
            answer.Plaintext,
            0,
            answer.Plaintext.Length,
            outputBuffer,
            outputOffset);

        byte[] expected = Filled(outputOffset, sentinel);
        byte[] actual = Slice(outputBuffer, 0, outputOffset);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> does not
    /// modify bytes after the transformed block written at a non-zero output offset.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldNotModifyBytesAfterWrittenBlock(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        const int outputOffset = 3;
        const int suffixLength = 3;
        const byte sentinel = 0xA5;

        byte[] outputBuffer = new byte[outputOffset + answer.Ciphertext.Length + suffixLength];
        Array.Fill(outputBuffer, sentinel);

        _ = transform.TransformBlock(
            answer.Plaintext,
            0,
            answer.Plaintext.Length,
            outputBuffer,
            outputOffset);

        byte[] expected = Filled(suffixLength, sentinel);
        byte[] actual = Slice(outputBuffer, outputOffset + answer.Ciphertext.Length, suffixLength);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a transform which reports support for multiple blocks returns the expected number of bytes
    /// written when encrypting repeated known-answer input blocks in a single
    /// <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> call.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenEncryptingMultipleKnownAnswerBlocks_ShouldReturnCiphertextByteCount(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        byte[] inputBuffer = RepeatBlock(answer.Plaintext, 2);
        byte[] expected = RepeatBlock(answer.Ciphertext, 2);
        byte[] outputBuffer = new byte[expected.Length];

        int written = transform.TransformBlock(
            inputBuffer,
            0,
            inputBuffer.Length,
            outputBuffer,
            0);

        Assert.AreEqual(expected.Length, written);
    }

    /// <summary>
    /// Verifies that a transform which reports support for multiple blocks produces the same ciphertext when
    /// encrypting repeated known-answer input blocks in a single
    /// <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> call as it does when
    /// encrypting the same input as sequential single-block calls.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenEncryptingMultipleKnownAnswerBlocks_ShouldMatchSequentialSingleBlockCalls(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        byte[] inputBuffer = RepeatBlock(answer.Plaintext, 2);
        byte[] batchedOutput = new byte[answer.Ciphertext.Length * 2];
        byte[] sequentialOutput = new byte[answer.Ciphertext.Length * 2];

        using (TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true))
        {
            _ = transform.TransformBlock(
                inputBuffer,
                0,
                inputBuffer.Length,
                batchedOutput,
                0);
        }

        using (TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true))
        {
            _ = transform.TransformBlock(
                inputBuffer,
                0,
                answer.Plaintext.Length,
                sequentialOutput,
                0);

            _ = transform.TransformBlock(
                inputBuffer,
                answer.Plaintext.Length,
                answer.Plaintext.Length,
                sequentialOutput,
                answer.Ciphertext.Length);
        }

        CollectionAssert.AreEqual(
            sequentialOutput,
            batchedOutput,
            $"TransformBlock multi-block encryption did not match sequential single-block calls for vector '{answer.Name}'.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> rejects
    /// an input count that is not an exact multiple of <see cref="ICryptoTransform.InputBlockSize" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputCountIsNotMultipleOfInputBlockSize_ShouldThrowCryptographicException()
    {
        using var transform = CreateAlgorithm();

        if (transform.InputBlockSize <= 1)
        {
            Assert.Inconclusive("The transform has a one-byte input block size and cannot produce a shorter partial block.");
            return;
        }

        byte[] inputBuffer = new byte[transform.InputBlockSize - 1];
        byte[] outputBuffer = new byte[transform.OutputBlockSize];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            transform.TransformBlock(inputBuffer, 0, inputBuffer.Length, outputBuffer, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> reads only
    /// the selected input range when <c>inputOffset</c> is non-zero.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenInputOffsetIsNonZero_ShouldReadOnlySelectedInputRange(BlockCipherKnownAnswer? answer)
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

        byte[] outputBuffer = new byte[answer.Ciphertext.Length];

        _ = transform.TransformBlock(
            inputBuffer,
            inputOffset,
            answer.Plaintext.Length,
            outputBuffer,
            0);

        CollectionAssert.AreEqual(
            answer.Ciphertext,
            outputBuffer,
            $"TransformBlock did not read the expected input range for vector '{answer.Name}'.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> succeeds when
    /// the output buffer has exactly enough remaining space from the requested <c>outputOffset</c>.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenOutputOffsetLeavesExactlyEnoughSpace_ShouldSucceed(BlockCipherKnownAnswer? answer)
    {
        if (answer is null)
        {
            Assert.Inconclusive($"{typeof(TTest).Name} declares no Transform-layer KAT vectors via {nameof(GetKnownAnswers)}.");
            return;
        }

        using TCryptoTransform transform = CreateTransformForKnownAnswer(answer, forEncryption: true);

        const int outputOffset = 3;
        byte[] outputBuffer = new byte[outputOffset + answer.Ciphertext.Length];

        int written = transform.TransformBlock(
            answer.Plaintext,
            0,
            answer.Plaintext.Length,
            outputBuffer,
            outputOffset);

        Assert.AreEqual(answer.Ciphertext.Length, written);
    }

    /// <summary>
    /// Verifies that a transform which reports <see cref="ICryptoTransform.CanReuseTransform" /> as
    /// <see langword="false" /> rejects <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" />
    /// after <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> has completed.
    /// </summary>
    /// <param name="answer">The vector under test, or <see langword="null" /> when the subclass declares no
    /// Transform-layer KATs.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerDisplayName))]
    public void TransformBlock_WhenCalledAfterTransformFinalBlockAndCanReuseTransformIsFalse_ShouldThrowInvalidOperationException(BlockCipherKnownAnswer? answer)
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

        byte[] outputBuffer = new byte[answer.Ciphertext.Length];

        _ = transform.TransformFinalBlock(answer.Plaintext, 0, answer.Plaintext.Length);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            transform.TransformBlock(answer.Plaintext, 0, answer.Plaintext.Length, outputBuffer, 0);
        });
    }

    private static byte[] RepeatBlock(byte[] block, int count)
    {
        byte[] result = new byte[block.Length * count];

        for (int i = 0; i < count; i++)
        {
            Buffer.BlockCopy(block, 0, result, i * block.Length, block.Length);
        }

        return result;
    }

    private static byte[] Slice(byte[] buffer, int offset, int count)
    {
        byte[] result = new byte[count];
        Buffer.BlockCopy(buffer, offset, result, 0, count);
        return result;
    }

    private static byte[] Filled(int count, byte value)
    {
        byte[] result = new byte[count];
        Array.Fill(result, value);
        return result;
    }
}