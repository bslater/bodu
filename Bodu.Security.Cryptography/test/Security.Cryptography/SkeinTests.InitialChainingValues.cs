// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.InitialChainingValues.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SkeinTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Returns the Skein 1.3 Appendix B initial chaining value words for the unkeyed plain-hash configuration
    /// associated with <paramref name="variant" />, or <see langword="null" /> when the concrete test class does
    /// not publish an Appendix B IV for that variant (for example, the <c>Mac_</c> variants where the IV depends
    /// on the per-row key and there is no published constant).
    /// </summary>
    /// <param name="variant">The variant under test.</param>
    /// <returns>
    /// The IV as a sequence of 64-bit little-endian words exactly as published in Appendix B of the Skein 1.3
    /// specification, or <see langword="null" /> when no published IV applies to the variant.
    /// </returns>
    protected abstract IReadOnlyList<ulong>? GetExpectedAppendixBChainingValue(TVariant variant);

    /// <summary>
    /// Verifies that the cached initial chaining value computed by <see cref="Skein{T}" /> matches the constant
    /// published in Skein 1.3 Appendix B for every variant whose concrete test class declares one. Variants
    /// without a published IV (typically the <c>Mac_</c> profiles) are reported as inconclusive.
    /// </summary>
    /// <param name="variant">The variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(HashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(HashAlgorithmVariantDisplayName))]
    public void InitialChainingValue_WhenComputed_ShouldMatchAppendixB(TVariant variant)
    {
        IReadOnlyList<ulong>? expected = GetExpectedAppendixBChainingValue(variant);
        if (expected is null)
        {
            Assert.Inconclusive($"[{variant}] No Appendix B initial chaining value published for this variant.");
            return;
        }

        using TAlgorithm algorithm = CreateAlgorithm(variant);

        // For the canonical Appendix B IV we compare against the unkeyed configuration: the published constant
        // assumes no preliminary KEY UBI phase, so reset the key to the empty sentinel before reading the IV.
        algorithm.Key = [];

        var actual = algorithm.GetInitialChainingValueWords();

        Assert.AreEqual(
            expected.Count,
            actual.Length,
            $"[{variant}] IV word count mismatch (expected {expected.Count}, got {actual.Length}).");

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.AreEqual(
                expected[i],
                actual[i],
                $"[{variant}] IV word {i}: expected 0x{expected[i]:X16}, got 0x{actual[i]:X16}.");
        }
    }
}
