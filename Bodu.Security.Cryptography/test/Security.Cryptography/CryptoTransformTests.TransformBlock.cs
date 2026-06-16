// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests.TransformBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class CryptoTransformTests<TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an <see cref="ObjectDisposedException" /> after the
    /// transform is disposed.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenDisposed_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        byte[] buffer = new byte[transform.InputBlockSize];

        transform.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            transform.TransformBlock(buffer, 0, buffer.Length, buffer, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an
    /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputBufferIsNull_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        byte[] outputBuffer = new byte[transform.OutputBlockSize];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            transform.TransformBlock(null!, 0, transform.InputBlockSize, outputBuffer, 0);
        });

        Assert.AreEqual("inputBuffer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an
    /// <see cref="ArgumentNullException" /> when <c>outputBuffer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputBufferIsNull_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();

        if (transform is HashAlgorithm)
        {
            Assert.Inconclusive("HashAlgorithm transforms do not require an output buffer for TransformBlock.");
            return;
        }

        byte[] inputBuffer = new byte[transform.InputBlockSize];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            transform.TransformBlock(inputBuffer, 0, inputBuffer.Length, null!, 0);
        });

        Assert.AreEqual("outputBuffer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an
    /// <see cref="ArgumentOutOfRangeException" /> when <c>inputOffset</c> is negative.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputOffsetIsNegative_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        byte[] inputBuffer = new byte[transform.InputBlockSize];
        byte[] outputBuffer = new byte[transform.OutputBlockSize];

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            transform.TransformBlock(inputBuffer, -1, inputBuffer.Length, outputBuffer, 0);
        });

        Assert.AreEqual("inputOffset", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an
    /// <see cref="ArgumentOutOfRangeException" /> when <c>inputCount</c> is negative.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputCountIsNegative_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        byte[] inputBuffer = new byte[transform.InputBlockSize];
        byte[] outputBuffer = new byte[transform.OutputBlockSize];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            transform.TransformBlock(inputBuffer, 0, -1, outputBuffer, 0);
        });

        if (ex.ParamName != null)
            Assert.AreEqual("inputCount", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an
    /// <see cref="ArgumentException" /> when the requested input range extends past the end of
    /// <c>inputBuffer</c>.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputRangeExceedsInputBuffer_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        byte[] inputBuffer = new byte[transform.InputBlockSize];
        byte[] outputBuffer = new byte[transform.OutputBlockSize * 2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            transform.TransformBlock(inputBuffer, 1, inputBuffer.Length, outputBuffer, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an
    /// <see cref="ArgumentOutOfRangeException" /> when <c>outputOffset</c> is negative.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputOffsetIsNegative_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();

        if (transform is HashAlgorithm)
        {
            Assert.Inconclusive("HashAlgorithm transforms do not require an output buffer for TransformBlock.");
            return;
        }

        byte[] inputBuffer = new byte[transform.InputBlockSize];
        byte[] outputBuffer = new byte[transform.OutputBlockSize];

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            transform.TransformBlock(inputBuffer, 0, inputBuffer.Length, outputBuffer, -1);
        });

        Assert.AreEqual("outputOffset", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an
    /// <see cref="ArgumentException" /> when the output buffer cannot contain the transformed block at the
    /// requested <c>outputOffset</c>.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputRangeExceedsOutputBuffer_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();

        if (transform is HashAlgorithm)
        {
            Assert.Inconclusive("HashAlgorithm transforms do not require an output buffer for TransformBlock.");
            return;
        }

        byte[] inputBuffer = new byte[transform.InputBlockSize];
        byte[] outputBuffer = new byte[transform.OutputBlockSize];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            transform.TransformBlock(inputBuffer, 0, inputBuffer.Length, outputBuffer, 1);
        });
    }
}
