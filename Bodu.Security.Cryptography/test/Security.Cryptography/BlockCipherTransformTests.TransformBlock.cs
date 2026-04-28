// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.TransformBlock.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws
    /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
    /// Regression guard for transforms that previously threw <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputBufferIsNull_ShouldThrowArgumentNullException_fix()
    {
        using var transform = CreateAlgorithm();
        byte[] output = new byte[transform.OutputBlockSize];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = transform.TransformBlock(null!, 0, 0, output, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws
    /// <see cref="ArgumentNullException" /> when <c>outputBuffer</c> is <see langword="null" />.
    /// Regression guard for transforms that previously threw <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenOutputBufferIsNull_ShouldThrowArgumentNullException_fix()
    {
        using var transform = CreateAlgorithm();
        byte[] input = new byte[transform.InputBlockSize];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = transform.TransformBlock(input, 0, input.Length, null!, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.InputBlockSize" /> is greater than zero.
    /// </summary>
    [TestMethod]
    public void InputBlockSize_ShouldBeGreaterThanZero()
    {
        using var transform = CreateAlgorithm();
        Assert.IsTrue(transform.InputBlockSize > 0);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.InputBlockSize" /> equals
    /// <see cref="BlockCipherTransform.OutputBlockSize" />.
    /// </summary>
    [TestMethod]
    public void InputBlockSize_ShouldEqualOutputBlockSize()
    {
        using var transform = CreateAlgorithm();
        Assert.AreEqual(transform.InputBlockSize, transform.OutputBlockSize);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.CanReuseTransform" /> returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void CanReuseTransform_ShouldReturnFalse()
    {
        using var transform = CreateAlgorithm();
        Assert.IsFalse(transform.CanReuseTransform);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.CanTransformMultipleBlocks" /> returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void CanTransformMultipleBlocks_ShouldReturnTrue()
    {
        using var transform = CreateAlgorithm();
        Assert.IsTrue(transform.CanTransformMultipleBlocks);
    }
}
