// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.TransformBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> returns
    /// the configured <see cref="ICryptoTransform.OutputBlockSize" /> when encrypting a single full block.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenEncryptingSingleBlock_ShouldReturnOutputBlockSize()
    {
        using TCryptoTransform encryptor = CreateEncryptor();
        byte[] plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize);
        byte[] outputBuffer = new byte[encryptor.OutputBlockSize];

        int written = encryptor.TransformBlock(plaintext, 0, plaintext.Length, outputBuffer, 0);

        Assert.AreEqual(encryptor.OutputBlockSize, written);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> returns
    /// the expected number of bytes written when encryption writes to a non-zero output offset.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldReturnOutputBlockSize()
    {
        using TCryptoTransform encryptor = CreateEncryptor();
        byte[] plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize);

        const int outputOffset = 3;
        byte[] outputBuffer = new byte[outputOffset + encryptor.OutputBlockSize + 3];

        int written = encryptor.TransformBlock(plaintext, 0, plaintext.Length, outputBuffer, outputOffset);

        Assert.AreEqual(encryptor.OutputBlockSize, written);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> produces
    /// identical ciphertext irrespective of the output offset — encrypting the same plaintext into offset 0
    /// of one buffer and offset <em>N</em> of another yields the same block of bytes at the respective
    /// output positions.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldWriteSameCiphertextAsOffsetZero()
    {
        byte[] expected;
        using (TCryptoTransform reference = CreateEncryptor())
        {
            byte[] plaintext = BuildIncrementingPlaintext(reference.InputBlockSize);
            expected = new byte[reference.OutputBlockSize];
            reference.TransformBlock(plaintext, 0, plaintext.Length, expected, 0);
        }

        using TCryptoTransform encryptor = CreateEncryptor();
        const int outputOffset = 3;
        byte[] plaintextOffset = BuildIncrementingPlaintext(encryptor.InputBlockSize);
        byte[] outputBuffer = new byte[outputOffset + encryptor.OutputBlockSize + 3];

        encryptor.TransformBlock(plaintextOffset, 0, plaintextOffset.Length, outputBuffer, outputOffset);

        byte[] actual = new byte[expected.Length];
        Buffer.BlockCopy(outputBuffer, outputOffset, actual, 0, expected.Length);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> does not
    /// modify bytes before the requested non-zero output offset.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldNotModifyBytesBeforeOutputOffset()
    {
        using TCryptoTransform encryptor = CreateEncryptor();
        byte[] plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize);

        const int outputOffset = 3;
        const byte sentinel = 0xA5;
        byte[] outputBuffer = new byte[outputOffset + encryptor.OutputBlockSize + 3];
        Array.Fill(outputBuffer, sentinel);

        encryptor.TransformBlock(plaintext, 0, plaintext.Length, outputBuffer, outputOffset);

        for (int i = 0; i < outputOffset; i++)
            Assert.AreEqual(sentinel, outputBuffer[i], $"Byte at index {i} was modified.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> does not
    /// modify bytes after the transformed block written at a non-zero output offset.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputOffsetIsNonZero_ShouldNotModifyBytesAfterWrittenBlock()
    {
        using TCryptoTransform encryptor = CreateEncryptor();
        byte[] plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize);

        const int outputOffset = 3;
        const int suffixLength = 3;
        const byte sentinel = 0xA5;
        byte[] outputBuffer = new byte[outputOffset + encryptor.OutputBlockSize + suffixLength];
        Array.Fill(outputBuffer, sentinel);

        encryptor.TransformBlock(plaintext, 0, plaintext.Length, outputBuffer, outputOffset);

        for (int i = outputOffset + encryptor.OutputBlockSize; i < outputBuffer.Length; i++)
            Assert.AreEqual(sentinel, outputBuffer[i], $"Byte at index {i} was modified.");
    }

    /// <summary>
    /// Verifies that a transform which reports support for multiple blocks returns the total bytes written
    /// when encrypting multiple input blocks in a single
    /// <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> call.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenEncryptingMultipleBlocks_ShouldReturnTotalByteCount()
    {
        using TCryptoTransform encryptor = CreateEncryptor();

        if (!encryptor.CanTransformMultipleBlocks)
        {
            Assert.Inconclusive("Transform does not support multi-block input in a single call.");
            return;
        }

        byte[] plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize * 2);
        byte[] outputBuffer = new byte[encryptor.OutputBlockSize * 2];

        int written = encryptor.TransformBlock(plaintext, 0, plaintext.Length, outputBuffer, 0);

        Assert.AreEqual(encryptor.OutputBlockSize * 2, written);
    }

    /// <summary>
    /// Verifies that a multi-block encryption performed in a single
    /// <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> call produces the same
    /// ciphertext as the equivalent sequence of single-block calls on a fresh paired encryptor.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenEncryptingMultipleBlocks_ShouldMatchSequentialSingleBlockCalls()
    {
        int blockSize;
        byte[] plaintext;
        using (TCryptoTransform probe = CreateEncryptor())
        {
            if (!probe.CanTransformMultipleBlocks)
            {
                Assert.Inconclusive("Transform does not support multi-block input in a single call.");
                return;
            }

            blockSize = probe.InputBlockSize;
            plaintext = BuildIncrementingPlaintext(blockSize * 2);
        }

        byte[] batchedOutput = new byte[blockSize * 2];
        using (TCryptoTransform batched = CreateEncryptor())
        {
            batched.TransformBlock(plaintext, 0, plaintext.Length, batchedOutput, 0);
        }

        byte[] sequentialOutput = new byte[blockSize * 2];
        using (TCryptoTransform sequential = CreateEncryptor())
        {
            sequential.TransformBlock(plaintext, 0, blockSize, sequentialOutput, 0);
            sequential.TransformBlock(plaintext, blockSize, blockSize, sequentialOutput, blockSize);
        }

        CollectionAssert.AreEqual(sequentialOutput, batchedOutput);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> rejects
    /// an input count that is not an exact multiple of <see cref="ICryptoTransform.InputBlockSize" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputCountIsNotMultipleOfInputBlockSize_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();

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
    /// the selected input range when <c>inputOffset</c> is non-zero — the produced ciphertext matches the
    /// result of encrypting the same payload at <c>inputOffset</c> 0.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputOffsetIsNonZero_ShouldReadOnlySelectedInputRange()
    {
        byte[] expected;
        byte[] payload;
        int blockSize;
        using (TCryptoTransform reference = CreateEncryptor())
        {
            blockSize = reference.InputBlockSize;
            payload = BuildIncrementingPlaintext(blockSize);
            expected = new byte[reference.OutputBlockSize];
            reference.TransformBlock(payload, 0, payload.Length, expected, 0);
        }

        using TCryptoTransform encryptor = CreateEncryptor();
        const int inputOffset = 5;
        byte[] inputBuffer = new byte[inputOffset + blockSize + 5];
        Buffer.BlockCopy(payload, 0, inputBuffer, inputOffset, blockSize);

        byte[] outputBuffer = new byte[encryptor.OutputBlockSize];
        encryptor.TransformBlock(inputBuffer, inputOffset, blockSize, outputBuffer, 0);

        CollectionAssert.AreEqual(expected, outputBuffer);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> succeeds
    /// when the output buffer has exactly enough remaining space from the requested <c>outputOffset</c>.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputOffsetLeavesExactlyEnoughSpace_ShouldSucceed()
    {
        using TCryptoTransform encryptor = CreateEncryptor();
        byte[] plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize);

        const int outputOffset = 3;
        byte[] outputBuffer = new byte[outputOffset + encryptor.OutputBlockSize];

        int written = encryptor.TransformBlock(plaintext, 0, plaintext.Length, outputBuffer, outputOffset);

        Assert.AreEqual(encryptor.OutputBlockSize, written);
    }

    /// <summary>
    /// Verifies that a transform which reports <see cref="ICryptoTransform.CanReuseTransform" /> as
    /// <see langword="false" /> rejects
    /// <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" /> after
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> has completed.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenCalledAfterTransformFinalBlockAndCanReuseTransformIsFalse_ShouldThrowExactly()
    {
        using TCryptoTransform encryptor = CreateEncryptor();

        if (encryptor.CanReuseTransform)
        {
            Assert.Inconclusive("This test only applies to transforms that cannot be reused after TransformFinalBlock.");
            return;
        }

        byte[] plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize);
        byte[] outputBuffer = new byte[encryptor.OutputBlockSize];

        _ = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            encryptor.TransformBlock(plaintext, 0, plaintext.Length, outputBuffer, 0);
        });
    }
}
