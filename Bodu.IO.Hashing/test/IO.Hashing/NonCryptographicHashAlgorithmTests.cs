// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Reflection;

namespace Bodu.IO.Hashing;

/// <summary>
/// Provides a reusable base class for verifying correctness and consistency of
/// <see cref="NonCryptographicHashAlgorithm" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">
/// The hash algorithm under test, which must derive from <see cref="NonCryptographicHashAlgorithm" /> and expose
/// a public parameterless constructor.
/// </typeparam>
/// <typeparam name="TVariant">The enumeration type used to represent algorithm configuration variants.</typeparam>
/// <remarks>
/// This class supplies a standardised infrastructure for testing non-cryptographic hash algorithms across one or
/// more configurations — variant differentiation, incremental-append parity, reset semantics, and data-driven
/// known-answer evaluation via the typed <see cref="NonCryptographicHashKnownAnswers" /> record.
/// </remarks>
public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    where TTest : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : NonCryptographicHashAlgorithm, new()
    where TVariant : struct, Enum
{
    /// <summary>
    /// Returns the <see cref="NonCryptographicHashAlgorithmSpecification" /> describing the expected properties
    /// of <typeparamref name="TAlgorithm" /> — including its known-answer test vectors — when constructed for
    /// the given <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The variant under test.</param>
    /// <returns>A specification describing expected output width, block size, distribution parameters, and KATs.</returns>
    protected abstract NonCryptographicHashAlgorithmSpecification GetSpecification(TVariant variant);

    /// <summary>
    /// Gets the default variant to use in non-parameterised test scenarios.
    /// </summary>
    /// <remarks>
    /// The default variant represents the canonical or most common configuration of the algorithm under test.
    /// It is used for tests that do not require variant-specific logic.
    /// </remarks>
    protected virtual TVariant DefaultVariant => GetNonCryptographicHashAlgorithmVariants().First();

    /// <summary>
    /// Gets the expected hash result for an empty input using <see cref="DefaultVariant" />.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the default variant's <see cref="NonCryptographicHashKnownAnswers.Empty" /> slot is unset.
    /// </exception>
    /// <remarks>
    /// Override in a derived class to supply an alternative computation when the algorithm does not declare a
    /// published empty-input KAT.
    /// </remarks>
    protected virtual byte[] ExpectedEmptyInputHash
    {
        get
        {
            var knownAnswers = GetSpecification(DefaultVariant).KnownAnswers;
            if (knownAnswers.Empty is { } hex)
                return Convert.FromHexString(hex);

            throw new InvalidOperationException(
                $"Expected hash for the empty input is not defined for variant '{DefaultVariant}'.");
        }
    }

    /// <summary>
    /// Returns test case parameters for each defined algorithm variant.
    /// </summary>
    /// <returns>An enumerable of <typeparamref name="TVariant" /> values wrapped in object arrays.</returns>
    public static IEnumerable<object[]> NonCryptographicHashAlgorithmVariants() =>
        new TTest().GetNonCryptographicHashAlgorithmVariants().Select(variant => new object[] { variant });

    /// <summary>
    /// Returns all supported algorithm variants to be tested for the current implementation.
    /// </summary>
    /// <returns>
    /// A sequence of <typeparamref name="TVariant" /> values representing valid configuration variants for the
    /// algorithm under test.
    /// </returns>
    /// <remarks>
    /// This method drives variant-specific tests. Each variant may represent a change in output size, internal
    /// configuration, or other algorithm-specific mode flags.
    /// </remarks>
    public virtual IEnumerable<TVariant> GetNonCryptographicHashAlgorithmVariants() => Enum.GetValues<TVariant>();

    /// <summary>
    /// Creates a new instance of the algorithm under test using <see cref="DefaultVariant" />.
    /// </summary>
    /// <returns>A fully initialised instance of <typeparamref name="TAlgorithm" />.</returns>
    protected TAlgorithm CreateAlgorithm() => CreateAlgorithm(DefaultVariant);

    /// <summary>
    /// Creates a new instance of the algorithm for the specified <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The variant to instantiate.</param>
    /// <returns>A new instance of <typeparamref name="TAlgorithm" /> configured for the given variant.</returns>
    protected abstract TAlgorithm CreateAlgorithm(TVariant variant);

    /// <summary>
    /// Returns the expected hash values after progressively appending a single byte at a time from the sequence
    /// <c>0x00, 0x01, 0x02, …</c>.
    /// </summary>
    /// <param name="variant">The variant under test.</param>
    /// <returns>
    /// A sequence of expected hex-encoded hash values. The entry at index <c>i</c> is the hash of the first
    /// <c>i</c> bytes of the incremental sequence (so index 0 is the empty-input hash). An empty sequence
    /// causes the incremental test to be marked inconclusive.
    /// </returns>
    /// <remarks>
    /// The incremental test iterates once per entry, appending one further byte at each step and comparing the
    /// current hash to the corresponding entry. This lets derived classes validate streaming semantics across
    /// residual-buffer and block-alignment boundaries.
    /// </remarks>
    protected abstract IEnumerable<string> GetIncrementalHashValue(TVariant variant);

    /// <summary>
    /// Yields the known-answer vectors declared by the specification for the given <paramref name="variant" />:
    /// each populated typed slot on <see cref="NonCryptographicHashKnownAnswers" /> paired with its
    /// corresponding shared input, followed by every algorithm-specific entry in
    /// <see cref="NonCryptographicHashKnownAnswers.Additional" />.
    /// </summary>
    /// <param name="variant">The variant to generate test vectors for.</param>
    /// <returns>A sequence of <see cref="KnownAnswerTest" /> records driving named-input assertions.</returns>
    protected virtual IEnumerable<KnownAnswerTest> GetTestVectors(TVariant variant)
    {
        var knownAnswers = GetSpecification(variant).KnownAnswers;

        if (knownAnswers.Empty is { } empty)
            yield return CreateVector(nameof(knownAnswers.Empty), NonCryptographicHashSharedInputs.Empty, empty);

        if (knownAnswers.Abc is { } abc)
            yield return CreateVector(nameof(knownAnswers.Abc), NonCryptographicHashSharedInputs.Abc, abc);

        if (knownAnswers.QuickBrownFox is { } qbf)
            yield return CreateVector(nameof(knownAnswers.QuickBrownFox), NonCryptographicHashSharedInputs.QuickBrownFox, qbf);

        if (knownAnswers.Zeros16 is { } zeros)
            yield return CreateVector(nameof(knownAnswers.Zeros16), NonCryptographicHashSharedInputs.Zeros16, zeros);

        if (knownAnswers.Sequential0To255 is { } sequential)
            yield return CreateVector(nameof(knownAnswers.Sequential0To255), NonCryptographicHashSharedInputs.Sequential0To255, sequential);

        foreach (var extra in knownAnswers.Additional)
        {
            yield return new KnownAnswerTest
            {
                Name = extra.Name,
                Input = extra.Input,
                ExpectedOutput = Convert.FromHexString(extra.ExpectedHex),
            };
        }
    }

    /// <summary>
    /// Returns test data that combines algorithm variants, named inputs, and expected hash results for
    /// parameterised known-answer tests.
    /// </summary>
    /// <returns>A sequence of test case arguments: variant, input name, input bytes, expected hash output.</returns>
    public static IEnumerable<object[]> KnownAnswerTestData()
    {
        var instance = new TTest();
        foreach (var variant in instance.GetNonCryptographicHashAlgorithmVariants())
        {
            foreach (var vector in instance.GetTestVectors(variant))
            {
                yield return new object[] { variant, vector.Name, vector.Input, vector.ExpectedOutput };
            }
        }
    }


    /// <summary>
    /// Gets the display name used by <see cref="DynamicDataAttribute" /> for a test case row.
    /// </summary>
    /// <param name="data">
    /// The test case data row. The first element is expected to contain the human-readable standard name.
    /// </param>
    /// <returns>
    /// The standard name for the current test case as a <see cref="string" />.
    /// </returns>
    public static string GetKnownAnswerTestName(MethodInfo methodInfo, object[] data)
    {
        TVariant variant = (TVariant)data[0];
        string testName = (string)data[1];
        return $"{testName} (Variant: {variant})";
    }

    private static KnownAnswerTest CreateVector(string name, byte[] input, string expectedHex) =>
        new()
        {
            Name = name,
            Input = input,
            ExpectedOutput = Convert.FromHexString(expectedHex),
        };
}
