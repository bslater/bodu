// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using System.Security.Cryptography;

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
    /// <exception cref="InvalidOperationException">
    /// Thrown if the default variant's <see cref="HashAlgorithmKnownAnswers.Empty" /> slot is unset.
    /// </exception>
    /// <remarks>
    /// This property is used to verify that the algorithm under test produces the correct result when hashing
    /// an empty input. The expected hash is sourced from
    /// <see cref="HashAlgorithmSpecification.KnownAnswers" /> for the <see cref="DefaultVariant" />.
    /// </remarks>
    protected virtual byte[] ExpectedEmptyInputHash
    {
        get
        {
            HashAlgorithmKnownAnswers knownAnswers = GetSpecification(DefaultVariant).KnownAnswers;
            if (knownAnswers.Empty is { } hex)
                return Convert.FromHexString(hex);

            throw new InvalidOperationException(
                $"Expected hash for the empty input is not defined for variant '{DefaultVariant}'.");
        }
    }

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
    /// This ensures consistency between fixed test vectors (e.g., the
    /// <see cref="HashAlgorithmKnownAnswers.Empty" /> slot) and the incremental output series, where the first
    /// incremental hash corresponds to hashing zero bytes. Algorithms that do not publish incremental hashes
    /// yet return an empty list from <see cref="GetExpectedHashesForIncrementalInput" />, or omit the
    /// <see cref="HashAlgorithmKnownAnswers.Empty" /> slot; the consistency check is then skipped as
    /// inconclusive rather than failing.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void HashAlgorithm_TestData_Check(TVariant variant)
    {
        IReadOnlyList<string> incrementalHashes = GetExpectedHashesForIncrementalInput(variant);
        if (incrementalHashes.Count == 0)
        {
            Assert.Inconclusive($"No incremental hashes defined for variant '{variant}'; skipping consistency check.");
            return;
        }

        string? emptyA = GetSpecification(variant).KnownAnswers.Empty;
        if (emptyA is null)
        {
            Assert.Inconclusive($"No empty-input known answer defined for variant '{variant}'; skipping consistency check.");
            return;
        }

        string emptyB = incrementalHashes[0];
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
    /// Yields the known-answer vectors declared by the specification for the given <paramref name="variant" />:
    /// each populated typed slot on <see cref="HashAlgorithmKnownAnswers" /> paired with its corresponding
    /// shared input, followed by every algorithm-specific entry in
    /// <see cref="HashAlgorithmKnownAnswers.Additional" />.
    /// </summary>
    /// <param name="variant">The variant to generate test vectors for.</param>
    /// <returns>A sequence of <see cref="KnownAnswerTest" /> instances driving named-input assertions.</returns>
    protected virtual IEnumerable<KnownAnswerTest> GetTestVectors(TVariant variant)
    {
        HashAlgorithmKnownAnswers knownAnswers = GetSpecification(variant).KnownAnswers;

        if (knownAnswers.Empty is { } empty)
            yield return CreateVector(nameof(knownAnswers.Empty), HashAlgorithmSharedInputs.Empty, empty);

        if (knownAnswers.Abc is { } abc)
            yield return CreateVector(nameof(knownAnswers.Abc), HashAlgorithmSharedInputs.Abc, abc);

        if (knownAnswers.QuickBrownFox is { } qbf)
            yield return CreateVector(nameof(knownAnswers.QuickBrownFox), HashAlgorithmSharedInputs.QuickBrownFox, qbf);

        if (knownAnswers.Zeros16 is { } zeros)
            yield return CreateVector(nameof(knownAnswers.Zeros16), HashAlgorithmSharedInputs.Zeros16, zeros);

        if (knownAnswers.Sequential0To255 is { } sequential)
            yield return CreateVector(nameof(knownAnswers.Sequential0To255), HashAlgorithmSharedInputs.Sequential0To255, sequential);

        foreach (HashAlgorithmKnownAnswer extra in knownAnswers.Additional)
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
    /// Defines the strategy used to invoke a hash algorithm against a buffered input
    /// during the incremental-input test. Implementations either call a synchronous
    /// <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> overload directly, or
    /// adapt <see cref="HashAlgorithm.ComputeHashAsync(Stream, System.Threading.CancellationToken)" />
    /// over an in-memory stream view of the same buffer.
    /// </summary>
    /// <param name="algorithm">The algorithm instance under test.</param>
    /// <param name="input">
    /// A backing buffer of length <c>maxLength</c>. Only the first <paramref name="byteCount" />
    /// bytes are part of the input; trailing bytes are unrelated scratch space and must be
    /// ignored by the implementation.
    /// </param>
    /// <param name="byteCount">The number of leading bytes from <paramref name="input" /> to hash.</param>
    /// <returns>A task producing the computed hash bytes.</returns>
    protected delegate Task<byte[]> IncrementalHashInvoker(HashAlgorithm algorithm, byte[] input, int byteCount);

    /// <summary>
    /// Drives the dense incremental-input verification for a single variant, asserting that the
    /// supplied invoker reproduces every expected hash for input lengths
    /// <c>0</c> through <c>coverage + 1</c>.
    /// </summary>
    /// <param name="variant">The variant identifier supplied by the dynamic data source.</param>
    /// <param name="invoke">The strategy used to obtain a hash for each incremental length.</param>
    /// <returns>A task that completes when all incremental lengths have been verified.</returns>
    private async Task AssertIncrementalInputAsync(TVariant variant, IncrementalHashInvoker invoke)
    {
        var specification = GetSpecification(variant);
        var expectedHashes = GetExpectedHashesForIncrementalInput(variant).ToArray();

        if (expectedHashes.Length == 0)
        {
            Assert.Inconclusive($"No expected hashes defined for variant {variant}.");
            return;
        }

        int coverage = specification.IncrementalCoverageBytes
            ?? (specification.InputBlockSize > 1 ? specification.InputBlockSize * 8 : 16);
        int maxLength = coverage + 1;
        int expectedEntryCount = maxLength + 1;

        Assert.AreEqual(expectedEntryCount, expectedHashes.Length,
            $"Expected {expectedEntryCount} algorithm entries for variant '{variant}' " +
            $"covering input lengths 0 through {maxLength} " +
            $"(InputBlockSize={specification.InputBlockSize}, coverage={coverage}), " +
            $"but got {expectedHashes.Length}.");

        var algorithm = CreateAlgorithm(variant);
        byte[] input = new byte[maxLength];

        try
        {
            for (int byteCount = 0; byteCount <= maxLength; byteCount++)
            {
                if (byteCount > 0)
                    input[byteCount - 1] = unchecked((byte)(byteCount - 1));

                byte[] expected = Convert.FromHexString(expectedHashes[byteCount]);
                byte[] actual = await invoke(algorithm, input, byteCount).ConfigureAwait(false);

                TestHelpers.TraceWriteIfNotEqual(expected, actual);

                CollectionAssert.AreEqual(expected, actual,
                    $"Hash mismatch for variant '{variant}' at incremental length {byteCount}.");

                if (!specification.CanReuseTransform && byteCount < maxLength)
                {
                    algorithm.Dispose();
                    algorithm = CreateAlgorithm(variant);
                }
            }
        }
        finally
        {
            algorithm.Dispose();
        }
    }
}
