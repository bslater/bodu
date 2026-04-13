// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Bodu.Infrastructure;
using Newtonsoft.Json.Linq;

namespace Bodu.Security.Cryptography
{
    public enum SingleTestVariant
    {
        Default
    }

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
        where TVariant : Enum
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
        /// Gets a value indicating whether the algorithm maintains state across transform operations.
        /// </summary>
        /// <remarks>
        /// Stateless algorithms return <see langword="true" /> to indicate that each hash computation is independent of prior state.
        /// Stateful algorithms must ensure correctness across reinitialization and partial block input.
        /// </remarks>
        public virtual bool IsStateless => false;

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
        protected override byte[] ExpectedEmptyInputHash =>
            Convert.FromHexString(
                GetExpectedHashesForNamedInputs(DefaultVariant).TryGetValue("Empty", out var hex)
                    ? hex
                    : throw new KeyNotFoundException(
                        $"Expected hash for \"Empty\" input is not defined for variant '{DefaultVariant}'."));

        /// <summary>
        /// Gets or sets the expected value of <see cref="HashAlgorithm.InputBlockSize" /> for the algorithm under test. Default is 1 for
        /// streaming hash algorithms.
        /// </summary>
        protected virtual int ExpectedInputBlockSize { get; init; } = 1;

        /// <summary>
        /// Gets or sets the expected value of <see cref="HashAlgorithm.OutputBlockSize" /> for the algorithm under test. Default is 1 for
        /// most hash algorithms.
        /// </summary>
        protected virtual int ExpectedOutputBlockSize { get; init; } = 1;

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
        public abstract IEnumerable<TVariant> GetHashAlgorithmVariants();

        /// <summary>
        /// Verifies that the expected hash for the "Empty" named input matches the first entry in the incremental hash vector set.
        /// </summary>
        /// <param name="variant">The algorithm variant under test.</param>
        /// <remarks>
        /// This ensures consistency between fixed test vectors (e.g., "Empty") and the incremental output series, where the first
        /// incremental hash corresponds to hashing zero bytes.
        /// </remarks>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants), DynamicDataSourceType.Method)]
        public void HashAlgorithm_TestData_Check(TVariant variant)
        {
            var emptyA = this.GetExpectedHashesForNamedInputs(variant)["Empty"];
            var emptyB = this.GetExpectedHashesForIncrementalInput(variant)[0];
            Assert.AreEqual(emptyA, emptyB, "Expected hash value for 'Empty' named input should equal the first item of incremental input.");
        }

        /// <summary>
        /// Returns public writable properties of the algorithm under test for use in dynamic property validation.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="PropertyInfo" /> arrays, each containing a single writable property. If no writable properties are
        /// found, a single <see langword="null" /> entry is returned to indicate an inconclusive test case.
        /// </returns>
        /// <remarks>
        /// This method supports validation of runtime immutability rules for cryptographic algorithms. It is commonly used to test whether
        /// modifying certain properties after hashing has begun results in an exception.
        /// </remarks>
        protected static IEnumerable<object[]> GetWritableProperties()
        {
            var algorithmType = typeof(TAlgorithm);
            var properties = algorithmType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .Where(p => p.SetMethod?.IsPublic == true)
                .ToList();

            if (properties.Count == 0)
            {
                yield return new object[] { null! };
                yield break;
            }

            foreach (var prop in properties)
                yield return new object[] { prop };
        }

        /// <summary>
        /// Creates a new instance of the algorithm under test using the default variant.
        /// </summary>
        /// <returns>A fully initialized instance of <typeparamref name="TAlgorithm" /> configured with <see cref="DefaultVariant" />.</returns>
        protected override TAlgorithm CreateAlgorithm() =>
            this.CreateAlgorithm(DefaultVariant);

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
}