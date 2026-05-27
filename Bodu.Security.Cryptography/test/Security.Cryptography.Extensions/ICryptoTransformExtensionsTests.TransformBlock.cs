// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests.TransformBlock.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public partial class ICryptoTransformExtensionsTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // TransformBlock(byte[]) — in-place overload
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenTransformIsNull_ShouldThrowExactly()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            transform!.TransformBlock([1, 2, 3, 4]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenArrayIsNull_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            transform.TransformBlock(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> writes
    /// the transformed output in-place and returns the number of bytes written.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void TransformBlock_WhenArrayIsValid_ShouldTransformInPlaceAndReturnByteCount(KnownAnswerTest kat)
    {
        // Take a fresh copy since TransformBlock modifies the array in-place.
        var block = (byte[])kat.Input.Clone();

        using SimpleReversingCryptoTransform transform = CreateTransform(kat);
        var written = transform.TransformBlock(block);

        Assert.AreEqual(kat.Input.Length, written,
            $"[{kat.Name}] Written byte count must equal input length.");
        CollectionAssert.AreEqual(kat.ExpectedOutput, block,
            $"[{kat.Name}] In-place transform did not produce the expected output.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" />
    /// returns zero when given an empty input, matching the <see cref="ICryptoTransform" /> contract
    /// that a zero-byte <c>TransformBlock</c> is a no-op that writes no output and does not advance
    /// any chaining state.
    /// </summary>
    /// <remarks>
    /// Pre-#200 the underlying <c>BlockCipherTransform.TransformBlock</c> used
    /// <c>ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize)</c> with the default
    /// <c>throwIfZero == true</c>, so this call surfaced a
    /// <see cref="CryptographicException" />. PRs #200 and the consolidation in #202 switched both
    /// validators to <c>throwIfZero: false</c> to comply with the framework contract; this test
    /// pins the new behaviour.
    /// </remarks>
    [TestMethod]
    public void TransformBlock_WhenArrayIsEmpty_ShouldReturnZero()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        var written = transform.TransformBlock(Array.Empty<byte>());

        Assert.AreEqual(0, written,
            "TransformBlock with an empty input must report zero bytes written, not throw.");
    }

    /// <summary>
    /// Verifies that consecutive calls to
    /// <see cref="ICryptoTransformExtensions.TransformBlock(ICryptoTransform,byte[])" /> each independently
    /// transform the supplied block, confirming that ECB mode produces no cross-call state leakage —
    /// the same input always yields the same output regardless of previous calls.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void TransformBlock_WhenCalledRepeatedly_ShouldTransformEachBlockIndependently(KnownAnswerTest kat)
    {
        // Two independent copies of the same input — the transform must produce identical
        // output for each, confirming there is no state leakage between consecutive calls.
        var blockA = (byte[])kat.Input.Clone();
        var blockB = (byte[])kat.Input.Clone();

        using SimpleReversingCryptoTransform transform = CreateTransform(kat);
        transform.TransformBlock(blockA);
        transform.TransformBlock(blockB);

        CollectionAssert.AreEqual(kat.ExpectedOutput, blockA,
            $"[{kat.Name}] First TransformBlock call produced wrong output.");
        CollectionAssert.AreEqual(kat.ExpectedOutput, blockB,
            $"[{kat.Name}] Second TransformBlock call produced wrong output — possible state leakage from first call.");
    }
}
