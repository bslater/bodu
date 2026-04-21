// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public enum SingleTestVariant
{
    Default
}

/// <summary>
/// Provides a reusable base class for verifying correctness and consistency of <see cref="NonCryptographicHashAlgorithm" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The hash algorithm under test, which must derive from <see cref="NonCryptographicHashAlgorithm" />.</typeparam>
/// <typeparam name="TVariant">The enumeration type used to represent algorithm configuration variants.</typeparam>
/// <remarks>
/// This class supplies a standardized infrastructure for testing keyed and unkeyed hash algorithms across multiple configurations,
/// including reusable hash verification, streaming support, variant differentiation, and named test vector evaluation.
/// </remarks>
public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    where TTest : NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : NonCryptographicHashAlgorithm, new()
    where TVariant : struct, Enum
{
    /// <summary>
    /// Returns the <see cref="NonCryptographicHashAlgorithmSpecification" /> describing the expected properties of
    /// <typeparamref name="TAlgorithm" /> when constructed for the given <paramref name="variant" />.
    /// </summary>
    protected abstract NonCryptographicHashAlgorithmSpecification GetSpecification(TVariant variant);

    /// <summary>
    /// Gets the default variant to use in non-parameterized test scenarios.
    /// </summary>
    /// <remarks>
    /// The default variant represents the canonical or most common configuration of the algorithm under test. It is used for tests that
    /// do not require variant-specific logic.
    /// </remarks>
    protected virtual TVariant DefaultVariant => GetNonCryptographicHashAlgorithmVariants().First();

    /// <summary>
    /// Gets the expected hash result for an empty input using the default algorithm variant.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if no expected hash is defined for the "Empty" input and current variant.</exception>
    /// <remarks>
    /// This property is used to verify that the algorithm under test produces the correct result when hashing an empty input. Expected
    /// results are sourced from <see cref="GetExpectedHashesForNamedInputs(TVariant)" />.
    /// </remarks>
    protected abstract byte[] ExpectedEmptyInputHash { get; }

    /// <summary>
    /// Returns test case parameters for each defined algorithm variant.
    /// </summary>
    /// <returns>An enumerable of <see cref="TVariant" /> values wrapped in object arrays.</returns>
    public static IEnumerable<object[]> NonCryptographicHashAlgorithmVariants() =>
        new TTest().GetNonCryptographicHashAlgorithmVariants().Select(variant => new object[] { variant });

    /// <summary>
    /// Returns all supported algorithm variants to be tested for the current implementation.
    /// </summary>
    /// <returns>
    /// A sequence of <typeparamref name="TVariant" /> values representing valid configuration variants for the algorithm under test.
    /// </returns>
    /// <remarks>
    /// This method drives variant-specific tests. Each variant may represent a change in output size, internal round configuration, or
    /// other algorithm-specific mode flags.
    /// </remarks>
    public virtual IEnumerable<TVariant> GetNonCryptographicHashAlgorithmVariants() => Enum.GetValues<TVariant>();

    /// <summary>
    /// Creates a new instance of the algorithm under test using the default variant.
    /// </summary>
    /// <returns>A fully initialized instance of <typeparamref name="TAlgorithm" /> configured with <see cref="DefaultVariant" />.</returns>
    protected override TAlgorithm CreateAlgorithm() =>
        CreateAlgorithm(DefaultVariant);

    /// <summary>
    /// Creates a new instance of the algorithm for the specified <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The variant to instantiate.</param>
    /// <returns>A new instance of <typeparamref name="TAlgorithm" /> configured for the given variant.</returns>
    protected abstract TAlgorithm CreateAlgorithm(TVariant variant);

    /// <summary>
    /// Gets the property names excluded from disposal validation tests. Override in a derived class to suppress
    /// properties that are intentionally accessible after disposal.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedFieldNames => [];

    /// <summary>
    /// The default set of property names excluded from disposal validation. Shared by the static
    /// <see cref="DynamicDataAttribute" /> data sources, which cannot access virtual instance members.
    /// </summary>
    private IReadOnlyCollection<string> GetExcludedFieldNames() =>
        ExcludedFieldNames
            .Concat([
                // excluded field names that are exposed in Bodu and .Net NonCryptographicHashAlgorithm types
                "disposed",
                "_disposed"
            ])
            .Distinct()
            .ToArray();

    /// <summary>
    /// Enumerates all instance fields in the algorithm and its base types to validate disposal state.
    /// </summary>
    public static IEnumerable<object[]> GetDisposableFields() =>
        TestHelpers.GetFieldInfoForType<TAlgorithm>(
            excludeFileds: new TTest().GetExcludedFieldNames()?.ToArray() ?? []);

    /// <summary>
    /// Gets the property names excluded from disposal validation tests. Override in a derived class to suppress
    /// properties that are intentionally accessible after disposal.
    /// </summary>
    private IReadOnlyCollection<string> GetExcludedReadablePropertyNames() =>
        ExcludedReadablePropertyNames
            .Concat([
                // excluded property names that are exposed .Net NonCryptographicHashAlgorithm types
                "CanReuseTransform",
                "CanTransformMultipleBlocks",
                "HashSize",
                "InputBlockSize",
                "OutputBlockSize",
            ])
            .Distinct()
            .ToArray();

    /// <summary>
    /// Gets the property names excluded from disposal validation tests. Override in a derived class to suppress
    /// properties that are intentionally accessible after disposal.
    /// </summary>
    private IReadOnlyCollection<string> GetExcludedWriteablePropertyNames() =>
        ExcludedWriteablePropertyNames
            .Concat([
                // excluded property names that are exposed .Net NonCryptographicHashAlgorithm types
            ])
            .Distinct()
            .ToArray();

    /// <summary>
    /// The default set of property names excluded from disposal validation. Shared by the static
    /// <see cref="DynamicDataAttribute" /> data sources, which cannot access virtual instance members.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedReadablePropertyNames => [];

    /// <summary>
    /// The default set of property names excluded from disposal validation. Shared by the static
    /// <see cref="DynamicDataAttribute" /> data sources, which cannot access virtual instance members.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedWriteablePropertyNames => [];

    /// <summary>
    /// Returns all publicly readable properties on <typeparamref name="TAlgorithm" /> as test data for disposal validation.
    /// </summary>
    public static IEnumerable<object[]> GetReadableProperties() =>
        TestHelpers.GetPropertyInfoForType<TAlgorithm>(
            TestHelpers.PropertyAccessMode.Read,
            excludeProperties: new TTest().GetExcludedReadablePropertyNames()?.ToArray() ?? []);

    /// <summary>
    /// Returns all publicly writable properties on <typeparamref name="TAlgorithm" /> as test data for disposal validation.
    /// </summary>
    public static IEnumerable<object[]> GetWritableProperties() =>
        TestHelpers.GetPropertyInfoForType<TAlgorithm>(
            TestHelpers.PropertyAccessMode.Write,
            excludeProperties: new TTest().GetExcludedWriteablePropertyNames()?.ToArray() ?? []);

    /// <summary>
    /// Combines shared input vectors with expected output values to generate test vectors for a specific variant.
    /// </summary>
    /// <param name="variant">The variant to generate test vectors for.</param>
    /// <returns>A sequence of <see cref="KnownAnswerTest" /> instances representing named test inputs and their expected hash results.</returns>
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
                    ExpectedOutput = Convert.FromHexString(hex)
                };
            }
        }
    }

    /// <summary>
    /// Returns the expected incremental hash values for the specified algorithm variant when hashing a sequence of bytes
    /// built by progressively appending values from <c>0x00</c> to <c>0xFF</c>.
    /// </summary>
    /// <param name="variant">
    /// The algorithm variant for which the expected incremental hash values are required.
    /// </param>
    /// <returns>
    /// A sequence of expected hash values for the progressively accumulated input sequence.
    /// The first value is the hash after appending <c>0x00</c>, and each subsequent value is the hash after appending the
    /// next byte in sequence, continuing through <c>0xFF</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Concrete implementations must return values that correspond to repeated incremental hashing operations over the
    /// same logical input sequence.
    /// </para>
    /// <para>
    /// The sequence must be ordered as follows:
    /// </para>
    /// <list type="number">
    /// <item><description>The hash after appending <c>0x00</c>.</description></item>
    /// <item><description>The hash after appending <c>0x01</c>.</description></item>
    /// <item><description>The hash after appending <c>0x02</c>.</description></item>
    /// <item><description>And so on, continuing incrementally through <c>0xFF</c>.</description></item>
    /// </list>
    /// <para>
    /// Each returned value represents the hash state after the next byte has been appended to the accumulated input,
    /// rather than the hash of each byte in isolation.
    /// </para>
    /// </remarks>
    protected abstract IEnumerable<string> GetIncrementalHashValue(TVariant variant);

    /// <summary>
    /// Returns test case parameters for each defined algorithm variant.
    /// </summary>
    /// <returns>An enumerable of <see cref="TVariant" /> values wrapped in object arrays.</returns>
    public static IEnumerable<object[]> NonCryptographicHashAlgorithmVariants() =>
        new TTest().GetNonCryptographicHashAlgorithmVariants().Select(variant => new object[] { variant });

}