// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests.TransformFinalBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class CryptoTransformTests<TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws an <see cref="ObjectDisposedException" /> after the
    /// transform has been disposed.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenDisposed_ShouldThrowExactly()
    {
        using var transform = CreateAlgorithm();
        transform.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws an
    /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputBufferIsNull_ShouldThrowArgumentNullException()
    {
        using var transform = CreateAlgorithm();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = transform.TransformFinalBlock(null!, 0, 0);
        });

        Assert.AreEqual("inputBuffer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws an
    /// <see cref="ArgumentOutOfRangeException" /> when <c>inputOffset</c> is negative.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputOffsetIsNegative_ShouldThrowArgumentException()
    {
        using var transform = CreateAlgorithm();
        byte[] inputBuffer = new byte[transform.InputBlockSize];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = transform.TransformFinalBlock(inputBuffer, -1, inputBuffer.Length);
        });

        Assert.AreEqual("inputOffset", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws an
    /// <see cref="ArgumentOutOfRangeException" /> when <c>inputCount</c> is negative.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputCountIsNegative_ShouldThrowArgumentException()
    {
        using var transform = CreateAlgorithm();
        byte[] inputBuffer = new byte[transform.InputBlockSize];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = transform.TransformFinalBlock(inputBuffer, 0, -1);
        });

        if (ex.ParamName != null)
            Assert.AreEqual("inputCount", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws an
    /// <see cref="ArgumentException" /> when the requested input range extends past the end of
    /// <c>inputBuffer</c>.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputRangeExceedsInputBuffer_ShouldThrowArgumentException()
    {
        using var transform = CreateAlgorithm();
        byte[] inputBuffer = new byte[transform.InputBlockSize];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = transform.TransformFinalBlock(inputBuffer, 1, inputBuffer.Length);
        });
    }
}
