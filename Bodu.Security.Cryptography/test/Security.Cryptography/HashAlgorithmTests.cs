// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a reusable base class for verifying correctness and consistency of <see cref="HashAlgorithm" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The hash algorithm under test, which must derive from <see cref="HashAlgorithm" />.</typeparam>
/// <typeparam name="TVariant">The enumeration type used to represent algorithm configuration variants.</typeparam>
/// <remarks>
/// This class supplies a standardized infrastructure for testing keyed and unkeyed hash algorithms across multiple configurations,
/// including reusable hash verification, streaming support, variant differentiation, and named test vector evaluation.
/// </remarks>
public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    : Security.Cryptography.CryptoTransformTests<TAlgorithm>
    where TTest : HashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : HashAlgorithm, new()
    where TVariant : struct, Enum
{
    /// <summary>
    /// Defines shared named input vectors used across all hash algorithm test cases.
    /// </summary>
    /// <remarks>
    /// Each entry maps a semantic name (e.g., "Empty", "ABC", "Zeros_16") to a representative input payload. These inputs are used in
    /// conjunction with expected output values returned by <see cref="GetExpectedHashesForNamedInputs(TVariant)" />.
    /// </remarks>
    protected static readonly IReadOnlyDictionary<string, byte[]> SharedInputs = new Dictionary<string, byte[]>
    {
        ["Empty"] = Array.Empty<byte>(),
        ["ABC"] = Encoding.ASCII.GetBytes("ABC"),
        ["QuickBrownFox"] = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"),
        ["Zeros_16"] = new byte[16],
        ["Sequential_0_255"] = Enumerable.Range(0, 255).Select(i => (byte)i).ToArray()
    };

    /// <summary>
    /// Gets a value indicating whether the algorithm supports partial input blocks during streaming.
    /// </summary>
    /// <remarks>
    /// If <see langword="true" />, tests will process data in randomly sized chunks smaller than <see cref="ExpectedInputBlockSize" />
    /// to verify streaming correctness. If <see langword="false" />, only block-aligned or single-pass input will be tested.
    /// </remarks>
    public virtual bool HandlePartialBlocks => true;

    /// <summary>
    /// Returns the <see cref="HashAlgorithmSpecification" /> describing the expected properties of
    /// <typeparamref name="TAlgorithm" /> when constructed for the given <paramref name="variant" />.
    /// </summary>
    protected abstract HashAlgorithmSpecification GetSpecification(TVariant variant);

    /// <summary>
    /// Gets the default variant to use in non-parameterized test scenarios.
    /// </summary>
    /// <remarks>
    /// The default variant represents the canonical or most common configuration of the algorithm under test. It is used for tests that
    /// do not require variant-specific logic.
    /// </remarks>
    protected virtual TVariant DefaultVariant => GetHashAlgorithmVariants().First();

    /// <summary>
    /// Gets the expected hash result for an empty input using the default algorithm variant.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if no expected hash is defined for the "Empty" input and current variant.</exception>
    /// <remarks>
    /// This property is used to verify that the algorithm under test produces the correct result when hashing an empty input. Expected
    /// results are sourced from <see cref="GetExpectedHashesForNamedInputs(TVariant)" />.
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
    /// <returns>An enumerable of <see cref="TVariant" /> values wrapped in object arrays.</returns>
    public static IEnumerable<object[]> HashAlgorithmVariants() =>
        new TTest().GetHashAlgorithmVariants().Select(variant => new object[] { variant });

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
    public virtual IEnumerable<TVariant> GetHashAlgorithmVariants() => Enum.GetValues<TVariant>();

    /// <summary>
    /// Verifies that the expected hash for the "Empty" named input matches the first entry in the incremental hash vector set.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    /// <remarks>
    /// This ensures consistency between fixed test vectors (e.g., "Empty") and the incremental output series, where the first
    /// incremental hash corresponds to hashing zero bytes. Algorithms that do not publish incremental hashes yet return an
    /// empty list from <see cref="GetExpectedHashesForIncrementalInput" />; the consistency check is then skipped as
    /// inconclusive rather than failing.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataSourceType.Method)]
    public void HashAlgorithm_TestData_Check(TVariant variant)
    {
        var incrementalHashes = GetExpectedHashesForIncrementalInput(variant);
        if (incrementalHashes.Count == 0)
        {
            Assert.Inconclusive($"No incremental hashes defined for variant '{variant}'; skipping consistency check.");
            return;
        }

        var emptyA = GetExpectedHashesForNamedInputs(variant)["Empty"];
        var emptyB = incrementalHashes[0];
        Assert.AreEqual(emptyA, emptyB, "Expected hash value for 'Empty' named input should equal the first item of incremental input.");
    }

    /// <summary>
    /// Creates a new instance of the algorithm under test using the default variant.
    /// </summary>
    /// <returns>A fully initialised instance of <typeparamref name="TAlgorithm" /> configured with <see cref="DefaultVariant" />.</returns>
    protected override TAlgorithm CreateAlgorithm() =>
        CreateAlgorithm(DefaultVariant);

    /// <summary>
    /// Creates a new instance of the algorithm for the specified <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The variant to instantiate.</param>
    /// <returns>A new instance of <typeparamref name="TAlgorithm" /> configured for the given variant.</returns>
    protected abstract TAlgorithm CreateAlgorithm(TVariant variant);

    /// <summary>
    /// Returns a list of expected hash outputs for progressive incremental inputs such as input[0..i].
    /// </summary>
    /// <param name="variant">The algorithm variant to retrieve expected results for.</param>
    /// <returns>A list of hexadecimal strings representing the hash outputs at each incremental step.</returns>
    protected abstract IReadOnlyList<string> GetExpectedHashesForIncrementalInput(TVariant variant);

    /// <summary>
    /// Returns a dictionary of expected hash outputs for well-known named inputs, such as "Empty", "ABC", or "Zeros_16".
    /// </summary>
    /// <param name="variant">The algorithm variant to retrieve expected results for.</param>
    /// <returns>A dictionary mapping input names to their expected hexadecimal hash strings for the specified variant.</returns>
    protected abstract IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(TVariant variant);

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
                // excluded field names that are exposed in Bodu and .Net HashAlgorithm types
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
                // excluded property names that are exposed .Net HashAlgorithm types
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
                // excluded property names that are exposed .Net HashAlgorithm types
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
}
