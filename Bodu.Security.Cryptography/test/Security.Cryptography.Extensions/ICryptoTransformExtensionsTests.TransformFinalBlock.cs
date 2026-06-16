// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests.TransformFinalBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public partial class ICryptoTransformExtensionsTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // TransformFinalBlock() — no-input overload
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform)" /> throws
    /// <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_NoInput_WhenTransformIsNull_ShouldThrowExactly()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform!.TransformFinalBlock());
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform)" /> finalises
    /// the transform without processing any data and returns a non-null byte array.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_NoInput_WhenTransformIsValid_ShouldReturnByteArray()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        byte[] result = transform.TransformFinalBlock();

        Assert.IsNotNull(result);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // TransformFinalBlock(byte[]) — full-array overload
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[])" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_ByteArray_WhenTransformIsNull_ShouldThrowExactly()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform!.TransformFinalBlock([1, 2, 3, 4]));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[])" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_ByteArray_WhenArrayIsNull_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform.TransformFinalBlock(null!));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[])" />
    /// transforms the entire array and returns the expected output.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void TransformFinalBlock_ByteArray_WhenArrayIsValid_ShouldReturnTransformedOutput(KnownAnswerTest kat)
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);

        byte[] result = transform.TransformFinalBlock(kat.Input);

        CollectionAssert.AreEqual(kat.ExpectedOutput, result,
            $"[{kat.Name}] TransformFinalBlock did not produce the expected output.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[])" />
    /// returns a non-null result when given an empty array.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_ByteArray_WhenArrayIsEmpty_ShouldReturnNonNullResult()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        byte[] result = transform.TransformFinalBlock([]);

        Assert.IsNotNull(result);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // TransformFinalBlock(byte[], int) — from-offset overload
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_ByteArrayOffset_WhenTransformIsNull_ShouldThrowExactly()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform!.TransformFinalBlock([1, 2, 3, 4], 0));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="array" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_ByteArrayOffset_WhenArrayIsNull_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform.TransformFinalBlock(null!, 0));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
    /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> is negative.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_ByteArrayOffset_WhenOffsetIsNegative_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            transform.TransformFinalBlock([1, 2, 3, 4], -1));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
    /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="offset" /> exceeds the array bounds.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_ByteArrayOffset_WhenOffsetExceedsBounds_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            transform.TransformFinalBlock([1, 2, 3, 4], 5));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,byte[],int)" />
    /// with offset zero transforms the entire array and returns the same output as the no-offset overload.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void TransformFinalBlock_ByteArrayOffset_WhenOffsetIsZero_ShouldTransformEntireArray(KnownAnswerTest kat)
    {
        byte[] fromOffset;
        using (SimpleReversingCryptoTransform transformA = CreateTransform(kat))
            fromOffset = transformA.TransformFinalBlock(kat.Input, 0);

        byte[] fromArray;
        using (SimpleReversingCryptoTransform transformB = CreateTransform(kat))
            fromArray = transformB.TransformFinalBlock(kat.Input);

        CollectionAssert.AreEqual(kat.ExpectedOutput, fromOffset,
            $"[{kat.Name}] Offset=0 result does not match expected output.");
        CollectionAssert.AreEqual(fromArray, fromOffset,
            $"[{kat.Name}] Offset=0 result does not match the no-offset overload.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // TransformFinalBlock(ReadOnlySpan<byte>)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_Span_WhenTransformIsNull_ShouldThrowExactly()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform!.TransformFinalBlock((ReadOnlySpan<byte>)[1, 2, 3, 4]));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlySpan{byte})" />
    /// correctly transforms a valid input span and returns the expected output.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void TransformFinalBlock_Span_WhenSpanIsValid_ShouldReturnTransformedOutput(KnownAnswerTest kat)
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);

        byte[] result = transform.TransformFinalBlock((ReadOnlySpan<byte>)kat.Input);

        CollectionAssert.AreEqual(kat.ExpectedOutput, result,
            $"[{kat.Name}] Span overload did not produce the expected output.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlySpan{byte})" />
    /// returns a non-null result when given an empty span.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_Span_WhenSpanIsEmpty_ShouldReturnNonNullResult()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        byte[] result = transform.TransformFinalBlock([]);

        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Verifies that the span overload produces output identical to the byte-array overload for the same input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void TransformFinalBlock_Span_WhenComparedToByteArrayOverload_ShouldProduceIdenticalOutput(
        KnownAnswerTest kat)
    {
        byte[] fromArray;
        using (SimpleReversingCryptoTransform transformA = CreateTransform(kat))
            fromArray = transformA.TransformFinalBlock(kat.Input);

        byte[] fromSpan;
        using (SimpleReversingCryptoTransform transformB = CreateTransform(kat))
            fromSpan = transformB.TransformFinalBlock((ReadOnlySpan<byte>)kat.Input);

        CollectionAssert.AreEqual(fromArray, fromSpan,
            $"[{kat.Name}] Span and byte-array overloads produced different output.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // TransformFinalBlock(ReadOnlyMemory<byte>)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlyMemory{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="cryptoTransform" /> is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TransformFinalBlock_Memory_WhenTransformIsNull_ShouldThrowExactly()
    {
        ICryptoTransform? transform = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            transform!.TransformFinalBlock(new ReadOnlyMemory<byte>([1, 2, 3, 4])));
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformFinalBlock(ICryptoTransform,ReadOnlyMemory{byte})" />
    /// transforms the memory region and returns the expected output.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void TransformFinalBlock_Memory_WhenMemoryIsValid_ShouldReturnTransformedOutput(KnownAnswerTest kat)
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);

        byte[] result = transform.TransformFinalBlock(new ReadOnlyMemory<byte>(kat.Input));

        CollectionAssert.AreEqual(kat.ExpectedOutput, result,
            $"[{kat.Name}] Memory overload did not produce the expected output.");
    }

    /// <summary>
    /// Verifies that the memory overload produces output identical to the span overload for the same input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public void TransformFinalBlock_Memory_WhenComparedToSpanOverload_ShouldProduceIdenticalOutput(
        KnownAnswerTest kat)
    {
        byte[] fromMemory;
        using (SimpleReversingCryptoTransform transformA = CreateTransform(kat))
            fromMemory = transformA.TransformFinalBlock(new ReadOnlyMemory<byte>(kat.Input));

        byte[] fromSpan;
        using (SimpleReversingCryptoTransform transformB = CreateTransform(kat))
            fromSpan = transformB.TransformFinalBlock((ReadOnlySpan<byte>)kat.Input);

        CollectionAssert.AreEqual(fromMemory, fromSpan,
            $"[{kat.Name}] Memory and span overloads produced different output.");
    }
}
