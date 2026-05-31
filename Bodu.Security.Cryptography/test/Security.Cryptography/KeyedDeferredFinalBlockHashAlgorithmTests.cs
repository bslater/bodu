// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedDeferredFinalBlockHashAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a reusable base class for verifying the correctness of
/// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">
/// The keyed deferred-final-block hash algorithm under test. Must derive from
/// <see cref="KeyedDeferredFinalBlockHashAlgorithm{TAlgorithm}" /> and expose a public parameterless constructor.
/// </typeparam>
/// <typeparam name="TVariant">The enumeration type used to represent algorithm configuration variants.</typeparam>
/// <remarks>
/// Extends <see cref="HashAlgorithmTests{TTest, TAlgorithm, TVariant}" /> with test logic specific to keyed
/// algorithms that follow the BLAKE-family deferred-finalisation shape — key retention, defensive copying,
/// legal key length boundaries, keyed-vs-unkeyed digest divergence, and disposal semantics.
/// </remarks>
public abstract partial class KeyedDeferredFinalBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    : HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    where TTest : KeyedDeferredFinalBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : KeyedDeferredFinalBlockHashAlgorithm<TAlgorithm>, new()
    where TVariant : struct, Enum
{
    /// <summary>
    /// Returns a sequence of keyed known-answer test vectors for the specified <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The algorithm variant to retrieve keyed vectors for.</param>
    /// <returns>
    /// An enumerable of <see cref="KnownAnswerTest" /> instances where
    /// <see cref="KnownAnswerTest.Parameters" /> contains a <c>"Key"</c> entry of type <see cref="byte[]" />.
    /// </returns>
    /// <remarks>
    /// The default implementation returns an empty sequence. Override in a concrete test class to supply
    /// implementation-specific keyed KAT vectors (for example, from the official BLAKE2 test vector files).
    /// </remarks>
    protected virtual IEnumerable<KnownAnswerTest> GetKeyedTestVectors(TVariant variant) => [];

    /// <summary>
    /// Returns test data that combines algorithm variants with keyed known-answer test vectors.
    /// </summary>
    /// <returns>
    /// A sequence of test case arguments: variant, vector name, input bytes, key bytes, and expected output.
    /// </returns>
    public static IEnumerable<object[]> KeyedKnownAnswerTestData()
    {
        TTest instance = new();
        foreach (TVariant variant in instance.GetHashAlgorithmVariants())
        {
            foreach (KnownAnswerTest kat in instance.GetKeyedTestVectors(variant))
            {
                if (kat.TryGet<byte[]>("Key", out var key) && key is not null)
                    yield return new object[] { variant, kat.Name, kat.Input, key, kat.ExpectedOutput };
            }
        }
    }
}
