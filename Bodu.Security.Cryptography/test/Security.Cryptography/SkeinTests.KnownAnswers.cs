// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SkeinTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Yields a data row for every keyed known-answer vector declared by every Skein variant. Each row is shaped as
    /// <c>(variant, vector name, profile-or-empty, key-or-null, input bytes, expected bytes)</c> and feeds the
    /// <see cref="ComputeHash_WhenUsingKeyedKnownAnswer_ShouldMatchExpected" /> data-driven test.
    /// </summary>
    /// <returns>A sequence of test-case argument arrays for MSTest's <see cref="DynamicDataAttribute" />.</returns>
    /// <remarks>
    /// The Skein-MAC <c>random+MAC</c> entries published with the Skein 1.3 / NIST CD KAT carry a different MAC key
    /// for every vector (and frequently a key length that differs from the spec's nominal MAC size), which the
    /// shared <c>HashAlgorithmKnownAnswers</c> slot cannot represent. Skein declares its keyed corpus through this
    /// dedicated path so each per-row key reaches the algorithm before <see cref="HashAlgorithm.ComputeHash(byte[])" />.
    /// </remarks>
    public static IEnumerable<object[]> KeyedKnownAnswerTestData()
    {
        var instance = new TTest();
        foreach (TVariant variant in instance.GetHashAlgorithmVariants())
        {
            if (instance.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
                continue;

            foreach (KeyedHashAlgorithmKnownAnswer vector in specification.KeyedAnswers.Vectors)
            {
                yield return new object[]
                {
                    variant,
                    vector.Name,
                    vector.Profile ?? string.Empty,
                    (object?)vector.Key ?? string.Empty,
                    vector.Input,
                    Convert.FromHexString(vector.ExpectedHex),
                };
            }
        }
    }

    /// <summary>
    /// Verifies that hashing <paramref name="input" /> under the variant configured by <paramref name="variant" />
    /// (with <paramref name="key" /> overriding the variant default when supplied as a non-string array) reproduces
    /// <paramref name="expected" /> — the published Skein 1.3 / NIST CD known-answer digest for this row.
    /// </summary>
    /// <param name="variant">The (output size, mode) configuration to instantiate.</param>
    /// <param name="vectorName">A semantic name printed in the failure message to identify the row.</param>
    /// <param name="profile">A free-form provenance tag printed alongside the name (for example <c>"NIST CD KAT"</c>).</param>
    /// <param name="keyOrSentinel">
    /// The per-row key as a <c>byte[]</c> when supplied; an empty string sentinel signals "use the variant default".
    /// MSTest <c>[DynamicData]</c> cannot round-trip a real <see langword="null" /> through the row tuple, so the
    /// empty-string sentinel substitutes for the absence of a per-row key.
    /// </param>
    /// <param name="input">The message bytes to hash.</param>
    /// <param name="expected">The expected digest bytes.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KeyedKnownAnswerTestData))]
    public void ComputeHash_WhenUsingKeyedKnownAnswer_ShouldMatchExpected(
        TVariant variant,
        string vectorName,
        string profile,
        object keyOrSentinel,
        byte[] input,
        byte[] expected)
    {
        using TAlgorithm algorithm = CreateAlgorithm(variant);

        if (keyOrSentinel is byte[] perRowKey)
            algorithm.Key = perRowKey;

        var actual = algorithm.ComputeHash(input);

        var label = string.IsNullOrEmpty(profile) ? vectorName : $"{vectorName} [{profile}]";
        CollectionAssert.AreEqual(
            expected,
            actual,
            $"Skein KAT mismatch for variant '{variant}', vector '{label}'.");
    }
}
