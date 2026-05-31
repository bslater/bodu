// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests.TransformFinalBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        using TCryptoTransform transform = CreateAlgorithm();
        transform.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            transform.TransformFinalBlock([], 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws an
    /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_WhenInputBufferIsNull_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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
    public void TransformFinalBlock_WhenInputOffsetIsNegative_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        var inputBuffer = new byte[transform.InputBlockSize];

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
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
    public void TransformFinalBlock_WhenInputCountIsNegative_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        var inputBuffer = new byte[transform.InputBlockSize];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
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
    public void TransformFinalBlock_WhenInputRangeExceedsInputBuffer_ShouldThrowExactly()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        var inputBuffer = new byte[transform.InputBlockSize];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = transform.TransformFinalBlock(inputBuffer, 1, inputBuffer.Length);
        });
    }
}
