// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Text;

namespace Bodu.IO.Hashing;

/// <summary>
/// Identifies the lone variant used by hash algorithms that do not expose configurable variants, satisfying the
/// <c>TVariant</c> type parameter of <see cref="NonCryptographicHashAlgorithmTests{TTest, TAlgorithm, TVariant}" />.
/// </summary>
public enum SingleTestVariant
{
    /// <summary>The default (and only) configuration of the algorithm under test.</summary>
    Default,
}

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
/// more configurations — shared input vectors, variant differentiation, incremental-append parity, reset
/// semantics, and named known-answer evaluation.
/// </remarks>
public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    where TTest : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : NonCryptographicHashAlgorithm, new()
    where TVariant : struct, Enum
{
    /// <summary>
    /// Defines shared named input vectors used across all non-cryptographic hash algorithm test cases.
    /// </summary>
    /// <remarks>
    /// Each entry maps a semantic name (for example, <c>Empty</c>, <c>ABC</c>, <c>Zeros_16</c>) to a
    /// representative input payload. These inputs are paired with expected output values supplied by
    /// <see cref="GetExpectedHashesForNamedInputs(TVariant)" /> to drive known-answer assertions.
    /// </remarks>
    protected static readonly IReadOnlyDictionary<string, byte[]> SharedInputs = new Dictionary<string, byte[]>
    {
        ["Empty"] = Array.Empty<byte>(),
        ["ABC"] = Encoding.ASCII.GetBytes("ABC"),
        ["QuickBrownFox"] = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"),
        ["Zeros_16"] = new byte[16],
        ["Sequential_0_255"] = Enumerable.Range(0, 255).Select(i => (byte)i).ToArray(),
    };

    /// <summary>
    /// Returns the <see cref="NonCryptographicHashAlgorithmSpecification" /> describing the expected properties
    /// of <typeparamref name="TAlgorithm" /> when constructed for the given <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The variant under test.</param>
    /// <returns>A specification describing expected output width, block size, and distribution parameters.</returns>
    protected abstract NonCryptographicHashAlgorithmSpecification GetSpecification(TVariant variant);

    /// <summary>
    /// Gets the default variant to use in non-parameterised test scenarios.
    /// </summary>
    /// <remarks>
    /// The default variant represents the canonical or most common configuration of the algorithm under test. It
    /// is used for tests that do not require variant-specific logic.
    /// </remarks>
    protected virtual TVariant DefaultVariant => GetNonCryptographicHashAlgorithmVariants().First();

    /// <summary>
    /// Gets the expected hash result for an empty input using <see cref="DefaultVariant" />.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if no expected hash is defined for the <c>Empty</c> named input under the default variant.
    /// </exception>
    /// <remarks>
    /// Expected results are sourced from <see cref="GetExpectedHashesForNamedInputs(TVariant)" />. Override in a
    /// derived class to supply an alternative computation when no <c>Empty</c> entry is provided.
    /// </remarks>
    protected virtual byte[] ExpectedEmptyInputHash =>
        Convert.FromHexString(
            GetExpectedHashesForNamedInputs(DefaultVariant).TryGetValue("Empty", out var hex)
                ? hex
                : throw new KeyNotFoundException(
                    $"Expected hash for \"Empty\" input is not defined for variant '{DefaultVariant}'."));

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
    /// <c>i</c> bytes of the incremental sequence (so index 0 is the empty-input hash). Derived classes may
    /// supply as many or as few entries as they wish; an empty sequence causes the incremental test to be
    /// marked inconclusive.
    /// </returns>
    /// <remarks>
    /// The incremental test iterates once per entry, appending one further byte at each step and comparing the
    /// current hash to the corresponding entry. This lets derived classes validate streaming semantics across
    /// residual-buffer and block-alignment boundaries.
    /// </remarks>
    protected abstract IEnumerable<string> GetIncrementalHashValue(TVariant variant);

    /// <summary>
    /// Returns a dictionary of expected hash outputs for well-known named inputs such as <c>Empty</c>,
    /// <c>ABC</c>, or <c>Zeros_16</c>.
    /// </summary>
    /// <param name="variant">The variant to retrieve expected results for.</param>
    /// <returns>
    /// A dictionary mapping input names (from <see cref="SharedInputs" />) to their expected hex-encoded hash
    /// strings. Sparse dictionaries are permitted — inputs with no matching entry are skipped by
    /// <see cref="GetTestVectors" /> rather than failing.
    /// </returns>
    protected abstract IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(TVariant variant);

    /// <summary>
    /// Combines shared input vectors with expected output values to generate test vectors for a specific variant.
    /// </summary>
    /// <param name="variant">The variant to generate test vectors for.</param>
    /// <returns>
    /// A sequence of <see cref="KnownAnswerTest" /> instances representing named test inputs and their expected
    /// hash results. Inputs without a matching expected entry in
    /// <see cref="GetExpectedHashesForNamedInputs(TVariant)" /> are omitted.
    /// </returns>
    protected virtual IEnumerable<KnownAnswerTest> GetTestVectors(TVariant variant)
    {
        var expected = GetExpectedHashesForNamedInputs(variant);
        foreach (var (name, input) in SharedInputs)
        {
            if (expected.TryGetValue(name, out var hex))
            {
                yield return new KnownAnswerTest
                {
                    Name = name,
                    Input = input,
                    ExpectedOutput = Convert.FromHexString(hex),
                };
            }
        }
    }

    /// <summary>
    /// Returns test data that combines algorithm variants, named inputs, and expected hash results for
    /// parameterised known-answer tests.
    /// </summary>
    /// <returns>A sequence of test case arguments: variant, input name, input bytes, expected hash output.</returns>
    public static IEnumerable<object[]> NamedInputTestData()
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
}
