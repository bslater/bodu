// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests{T,T,T}.Append.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{

    /// <summary>
    /// Verifies that splitting an input across multiple
    /// <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" /> calls produces the same digest as
    /// hashing it all at once.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void Append_WhenCalledInChunks_ShouldMatchSingleAppend(TVariant variant)
    {
        byte[] data = NonCryptographicHashSharedInputs.Sequential0To255;

        NonCryptographicHashAlgorithm whole = CreateAlgorithm(variant);
        whole.Append(data);

        NonCryptographicHashAlgorithm chunked = CreateAlgorithm(variant);
        int third = data.Length / 3;
        chunked.Append(data.AsSpan(0, third));
        chunked.Append(data.AsSpan(third, third));
        chunked.Append(data.AsSpan(third * 2, data.Length - (third * 2)));

        CollectionAssert.AreEqual(whole.GetCurrentHash(), chunked.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that appending one byte at a time yields the same digest as appending the full sequence in a
    /// single call, across a block-straddling input.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void Append_WhenCalledOneByteAtATime_ShouldMatchSingleAppend(TVariant variant)
    {
        byte[] data = NonCryptographicHashSharedInputs.QuickBrownFox;

        NonCryptographicHashAlgorithm whole = CreateAlgorithm(variant);
        whole.Append(data);

        NonCryptographicHashAlgorithm perByte = CreateAlgorithm(variant);
        foreach (byte b in data)
            perByte.Append([b]);

        CollectionAssert.AreEqual(whole.GetCurrentHash(), perByte.GetCurrentHash());
    }
    /// <summary>
    /// Verifies that appending an empty span leaves <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" />
    /// unchanged — equal to the hash of a freshly constructed instance.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void Append_WhenInputIsEmpty_ShouldLeaveHashUnchanged(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        NonCryptographicHashAlgorithm baseline = CreateAlgorithm(variant);

        algorithm.Append([]);

        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that hashing a named input via a single
    /// <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" /> call reproduces the expected
    /// known-answer digest for the variant.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    /// <param name="testName">The semantic name of the input vector.</param>
    /// <param name="input">The input bytes being hashed.</param>
    /// <param name="expected">The expected hash digest.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerTestData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void Append_WhenUsingNamedInput_ShouldProduceExpectedHash(TVariant variant, string testName, byte[] input, byte[] expected)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(input);

        byte[] actual = algorithm.GetCurrentHash();
        Bodu.Test.TestHelpers.TraceWriteIfNotEqual(expected, actual, $"Hash mismatch for '{testName}' using variant '{variant}'.");

        CollectionAssert.AreEqual(expected, actual, $"Hash mismatch for '{testName}' using variant '{variant}'.");
    }

}
