// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Reflection;
using Bodu.Test;

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
    /// Defines the strategy used to observe the current hash after one further byte has been appended
    /// to a streaming non-cryptographic hash algorithm instance.
    /// </summary>
    /// <param name="algorithm">The algorithm instance under test.</param>
    /// <param name="source">The next input segment to append. This is empty for length <c>0</c>.</param>
    /// <returns>A task producing the current hash bytes without resetting the algorithm.</returns>
    protected delegate Task<byte[]> IncrementalCurrentHashInvoker(
        NonCryptographicHashAlgorithm algorithm,
        byte[] source);
    /// <summary>
    /// Defines the strategy used to compute a non-cryptographic hash over the first
    /// <paramref name="byteCount" /> bytes of a shared input buffer during the incremental-length test.
    /// </summary>
    /// <param name="algorithm">The algorithm instance under test.</param>
    /// <param name="input">
    /// A backing buffer of length <c>maxLength</c>. Only the first <paramref name="byteCount" />
    /// bytes are part of the input; trailing bytes are unrelated scratch space and must be ignored.
    /// </param>
    /// <param name="byteCount">The number of leading bytes from <paramref name="input" /> to hash.</param>
    /// <returns>A task producing the computed hash bytes.</returns>
    protected delegate Task<byte[]> IncrementalHashInvoker(
        NonCryptographicHashAlgorithm algorithm,
        byte[] input,
        int byteCount);

    /// <summary>
    /// Gets the default variant to use in non-parameterised test scenarios.
    /// </summary>
    /// <remarks>
    /// The default variant represents the canonical or most common configuration of the algorithm under test.
    /// It is used for tests that do not require variant-specific logic.
    /// </remarks>
    protected virtual TVariant DefaultVariant => GetNonCryptographicHashAlgorithmVariants().First();

    /// <summary>
    /// Gets the field names excluded from disposal validation tests. Override in a derived class to suppress
    /// fields that are intentionally retained after disposal.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedFieldNames => [];

    /// <summary>
    /// Gets the readable property names excluded from disposal validation tests. Override in a derived class to
    /// suppress properties that are intentionally accessible after disposal.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedReadablePropertyNames => [];

    /// <summary>
    /// Gets the writable property names excluded from disposal validation tests. Override in a derived class to
    /// suppress properties that are intentionally assignable after disposal.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ExcludedWritablePropertyNames => [];

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
            NonCryptographicHashKnownAnswers knownAnswers = GetSpecification(DefaultVariant).KnownAnswers;
            if (knownAnswers.Empty is { } hex)
                return Convert.FromHexString(hex);

            throw new InvalidOperationException(
                $"Expected hash for the empty input is not defined for variant '{DefaultVariant}'.");
        }
    }

    /// <summary>
    /// Enumerates the writable instance fields on <typeparamref name="TAlgorithm" /> that the disposal field-zero
    /// test should validate. Excludes read-only fields, auto-property backing fields, and disposal-state flags.
    /// </summary>
    /// <returns>A sequence of single-element <see cref="object" /> arrays each containing one <see cref="FieldInfo" />.</returns>
    public static IEnumerable<object[]> GetAlgorithmFields() =>
        TestHelpers.GetFieldInfoForType<TAlgorithm>(
            excludeReadOnly: true,
            excludeFields: new TTest().GetExcludedFieldNames()?.ToArray() ?? []);

    /// <summary>
    /// Enumerates the readable public properties on <typeparamref name="TAlgorithm" /> that the disposal
    /// property-read test should validate.
    /// </summary>
    /// <returns>A sequence of single-element <see cref="object" /> arrays each containing one <see cref="PropertyInfo" />.</returns>
    public static IEnumerable<object[]> GetAlgorithmReadableProperties() =>
        TestHelpers.GetPropertyInfoForType<TAlgorithm>(
            TestHelpers.PropertyAccessMode.Read,
            excludeProperties: new TTest().GetExcludedReadablePropertyNames()?.ToArray() ?? []);

    /// <summary>
    /// Enumerates the writable public properties on <typeparamref name="TAlgorithm" /> that the disposal
    /// property-write test should validate.
    /// </summary>
    /// <returns>A sequence of single-element <see cref="object" /> arrays each containing one <see cref="PropertyInfo" />.</returns>
    public static IEnumerable<object[]> GetAlgorithmWritableProperties() =>
        TestHelpers.GetPropertyInfoForType<TAlgorithm>(
            TestHelpers.PropertyAccessMode.Write,
            excludeProperties: new TTest().GetExcludedWritablePropertyNames()?.ToArray() ?? []);


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
        var variant = (TVariant)data[0];
        var testName = (string)data[1];
        return $"{testName} (Variant: {variant})";
    }

    /// <summary>
    /// Returns test data that combines algorithm variants, named inputs, and expected hash results for
    /// parameterised known-answer tests.
    /// </summary>
    /// <returns>A sequence of test case arguments: variant, input name, input bytes, expected hash output.</returns>
    public static IEnumerable<object[]> KnownAnswerTestData()
    {
        var instance = new TTest();
        foreach (TVariant variant in instance.GetNonCryptographicHashAlgorithmVariants())
        {
            foreach (KnownAnswerTest vector in instance.GetTestVectors(variant))
            {
                yield return new object[] { variant, vector.Name, vector.Input, vector.ExpectedOutput };
            }
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
    /// Verifies that the expected hash for the "Empty" named input matches the first entry in the incremental hash vector set.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    /// <remarks>
    /// This ensures consistency between fixed test vectors (e.g., the
    /// <see cref="HashAlgorithmKnownAnswers.Empty" /> slot) and the incremental output series, where the first
    /// incremental hash corresponds to hashing zero bytes. Algorithms that do not publish incremental hashes
    /// yet return an empty list from <see cref="GetExpectedHashesForIncrementalInput" />, or omit the
    /// <see cref="HashAlgorithmKnownAnswers.Empty" /> slot; the consistency check is then skipped as
    /// inconclusive rather than failing.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void HashAlgorithm_TestData_Check(TVariant variant)
    {
        IReadOnlyList<string> incrementalHashes = GetExpectedHashesForIncrementalInput(variant);
        if (incrementalHashes.Count == 0)
        {
            Assert.Inconclusive($"No incremental hashes defined for variant '{variant}'; skipping consistency check.");
            return;
        }

        var emptyA = GetSpecification(variant).KnownAnswers.Empty;
        if (emptyA is null)
        {
            Assert.Inconclusive($"No empty-input known answer defined for variant '{variant}'; skipping consistency check.");
            return;
        }

        var emptyB = incrementalHashes[0];
        Assert.AreEqual(emptyA, emptyB, "Expected hash value for 'Empty' named input should equal the first item of incremental input.");
    }

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
    protected abstract IReadOnlyList<string> GetExpectedHashesForIncrementalInput(TVariant variant);
    /// <summary>
    /// Returns the <see cref="NonCryptographicHashAlgorithmSpecification" /> describing the expected properties
    /// of <typeparamref name="TAlgorithm" /> — including its known-answer test vectors — when constructed for
    /// the given <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The variant under test.</param>
    /// <returns>A specification describing expected output width, block size, distribution parameters, and KATs.</returns>
    protected abstract NonCryptographicHashAlgorithmSpecification GetSpecification(TVariant variant);

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
        NonCryptographicHashKnownAnswers knownAnswers = GetSpecification(variant).KnownAnswers;

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

        foreach (NonCryptographicHashKnownAnswer extra in knownAnswers.Additional)
        {
            yield return new KnownAnswerTest
            {
                Name = extra.Name,
                Input = extra.Input,
                ExpectedOutput = Convert.FromHexString(extra.ExpectedHex),
            };
        }
    }

    private static KnownAnswerTest CreateVector(string name, byte[] input, string expectedHex) =>
        new()
        {
            Name = name,
            Input = input,
            ExpectedOutput = Convert.FromHexString(expectedHex),
        };

    /// <summary>
    /// Drives true streaming incremental verification for a single variant, appending one additional
    /// byte at each step and asserting that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" />
    /// matches the expected value after each append.
    /// </summary>
    /// <param name="variant">The variant identifier supplied by the dynamic data source.</param>
    /// <param name="invoke">The strategy used to append the next segment and observe the current hash.</param>
    /// <returns>A task that completes when all incremental stages have been verified.</returns>
    private async Task AssertIncrementalCurrentHashAsync(TVariant variant, IncrementalCurrentHashInvoker invoke)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        var expectedHashes = GetExpectedHashesForIncrementalInput(variant).ToArray();

        if (expectedHashes.Length == 0)
        {
            Assert.Inconclusive($"No expected hashes defined for variant {variant}.");
            return;
        }

        var coverage = specification.IncrementalCoverageBytes
            ?? (specification.HashLengthInBytes > 1 ? specification.HashLengthInBytes * 8 : 16);
        var maxLength = coverage + 1;
        var expectedEntryCount = maxLength + 1;

        Assert.AreEqual(expectedEntryCount, expectedHashes.Length,
            $"Expected {expectedEntryCount} algorithm entries for variant '{variant}' " +
            $"covering input lengths 0 through {maxLength} " +
            $"(HashLengthInBytes={specification.HashLengthInBytes}, coverage={coverage}), " +
            $"but got {expectedHashes.Length}.");

        TAlgorithm algorithm = CreateAlgorithm(variant);

        for (var byteCount = 0; byteCount <= maxLength; byteCount++)
        {
            byte[] source = byteCount == 0
                ? []
                : [unchecked((byte)(byteCount - 1))];

            var expected = Convert.FromHexString(expectedHashes[byteCount]);
            var actual = await invoke(algorithm, source).ConfigureAwait(false);

            TestHelpers.TraceWriteIfNotEqual(expected, actual);

            CollectionAssert.AreEqual(expected, actual,
                $"Hash mismatch for variant '{variant}' at incremental length {byteCount}.");
        }
    }

    /// <summary>
    /// Drives dense incremental-length verification for a single variant, asserting that the supplied
    /// invoker reproduces every expected hash for input lengths <c>0</c> through <c>coverage + 1</c>.
    /// </summary>
    /// <param name="variant">The variant identifier supplied by the dynamic data source.</param>
    /// <param name="invoke">The strategy used to obtain a hash for each incremental length.</param>
    /// <returns>A task that completes when all incremental lengths have been verified.</returns>
    private async Task AssertIncrementalInputAsync(TVariant variant, IncrementalHashInvoker invoke)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        var expectedHashes = GetExpectedHashesForIncrementalInput(variant).ToArray();

        if (expectedHashes.Length == 0)
        {
            Assert.Inconclusive($"No expected hashes defined for variant {variant}.");
            return;
        }

        var coverage = specification.IncrementalCoverageBytes
            ?? (specification.HashLengthInBytes > 1 ? specification.HashLengthInBytes * 8 : 16);
        var maxLength = coverage + 1;
        var expectedEntryCount = maxLength + 1;

        Assert.AreEqual(expectedEntryCount, expectedHashes.Length,
            $"Expected {expectedEntryCount} algorithm entries for variant '{variant}' " +
            $"covering input lengths 0 through {maxLength} " +
            $"(HashLengthInBytes={specification.HashLengthInBytes}, coverage={coverage}), " +
            $"but got {expectedHashes.Length}.");

        TAlgorithm algorithm = CreateAlgorithm(variant);
        var input = new byte[maxLength];

        for (var byteCount = 0; byteCount <= maxLength; byteCount++)
        {
            if (byteCount > 0)
                input[byteCount - 1] = unchecked((byte)(byteCount - 1));

            var expected = Convert.FromHexString(expectedHashes[byteCount]);
            var actual = await invoke(algorithm, input, byteCount).ConfigureAwait(false);

            TestHelpers.TraceWriteIfNotEqual(expected, actual);

            CollectionAssert.AreEqual(expected, actual,
                $"Hash mismatch for variant '{variant}' at incremental length {byteCount}.");
        }
    }

    /// <summary>
    /// Returns the merged set of field names excluded from the disposal field-zero test. Combines
    /// <see cref="ExcludedFieldNames" /> with the disposal-state flags shared across Bodu and BCL hashing types.
    /// </summary>
    /// <returns>A distinct, materialised collection of field names to exclude.</returns>
    private IReadOnlyCollection<string> GetExcludedFieldNames() =>
        ExcludedFieldNames
            .Concat([
                // Disposal-state flags are intentionally non-default after Dispose; exclude them from the
                // field-zero test so subclasses don't need to know about them.
                "disposed",
                "_disposed"
            ])
            .Distinct()
            .ToArray();

    /// <summary>
    /// Returns the merged set of readable property names excluded from the disposal property-read test. Combines
    /// <see cref="ExcludedReadablePropertyNames" /> with inherited properties on
    /// <see cref="NonCryptographicHashAlgorithm" /> that are not expected to throw after disposal.
    /// </summary>
    /// <returns>A distinct, materialised collection of property names to exclude.</returns>
    private IReadOnlyCollection<string> GetExcludedReadablePropertyNames() =>
        ExcludedReadablePropertyNames
            .Concat([
                // HashLengthInBytes is exposed by the BCL NonCryptographicHashAlgorithm base class and does not
                // observe disposal state.
                nameof(NonCryptographicHashAlgorithm.HashLengthInBytes),
            ])
            .Distinct()
            .ToArray();

    /// <summary>
    /// Returns the merged set of writable property names excluded from the disposal property-write test.
    /// Currently a pass-through of <see cref="ExcludedWritablePropertyNames" />; preserved as a hook for future
    /// inherited write-protected members.
    /// </summary>
    /// <returns>A distinct, materialised collection of property names to exclude.</returns>
    private IReadOnlyCollection<string> GetExcludedWritablePropertyNames() =>
        ExcludedWritablePropertyNames
            .Distinct()
            .ToArray();

}