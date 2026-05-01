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
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws
    /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
    /// Regression guard for transforms that previously threw <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputBufferIsNull_ShouldThrowArgumentNullException_fix()
    {
        using var transform = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = transform.TransformFinalBlock(null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> rejects an input
    /// count that is not an exact multiple of <see cref="ICryptoTransform.InputBlockSize" /> for raw block
    /// cipher transforms.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputCountIsNotMultipleOfInputBlockSize_ShouldThrowCryptographicException()
    {
        using var transform = CreateAlgorithm();

        if (transform.InputBlockSize <= 1)
        {
            Assert.Inconclusive("The transform has a one-byte input block size and cannot produce a shorter partial block.");
            return;
        }

        byte[] inputBuffer = new byte[transform.InputBlockSize - 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = transform.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
        });
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
