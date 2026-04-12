using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class KeyedHashAlgorithmTests<TTest, TAlgorithm, TVariant>
        : Security.Cryptography.HashAlgorithmTests<TTest, TAlgorithm, TVariant>
        where TTest : HashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
        where TAlgorithm : KeyedBlockHashAlgorithm<TAlgorithm>, new()
        where TVariant : Enum
    {
        /// <summary>
        /// Creates a new instance of the hash algorithm under test, preconfigured with a deterministic key.
        /// </summary>
        /// <returns>
        /// A newly constructed instance of <typeparamref name="TAlgorithm" />, initialized with a fixed, repeatable key for deterministic testing.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is used by test cases to obtain a fresh instance of the current <see cref="KeyedHashAlgorithm" /> implementation.
        /// The instance is initialized with the result of <see cref="GetDeterministicKey" /> to ensure that hash computations are stable
        /// and repeatable across test runs.
        /// </para>
        /// <para>
        /// Override this method if the algorithm under test requires special construction (e.g., constructor parameters, clamping, or
        /// post-initialization setup).
        /// </para>
        /// </remarks>
        protected override TAlgorithm CreateAlgorithm() =>
            new TAlgorithm
            {
                Key = GetDeterministicKey()
            };

        /// <summary>
        /// Generates a unique and valid cryptographic key for use with the current <see cref="KeyedHashAlgorithm" /> implementation.
        /// </summary>
        /// <returns>A non-null, non-empty <see cref="byte" /> array containing a randomly generated key suitable for the current algorithm.</returns>
        /// <remarks>
        /// <para>
        /// This method is used by test cases that validate key-dependent behavior, such as verifying that different keys yield different
        /// hash results or that key isolation is preserved across multiple executions.
        /// </para>
        /// <para>
        /// The returned key must conform to the expected key size and format of the algorithm under test. Implementations should ensure
        /// that each invocation yields a distinct, non-zero key appropriate for the specific hash algorithm.
        /// </para>
        /// </remarks>
        protected virtual byte[] GenerateUniqueKey()
        {
            byte[] key = new byte[ExpectedKeySize];
            CryptoHelpers.FillWithRandomNonZeroBytes(key);
            return key;
        }

        /// <summary>
        /// Returns a fixed, deterministic key for use in tests that require consistent hash output across runs.
        /// </summary>
        /// <returns>A non-null <see cref="byte" /> array with a stable, algorithm-appropriate pattern used to verify deterministic behavior.</returns>
        /// <remarks>
        /// <para>
        /// This method is intended for tests that compare hash output consistency across different algorithm instances using the same key.
        /// The returned key is constant and repeatable, ensuring stable results in scenarios like round-trip verification, snapshot
        /// testing, or key reuse validation.
        /// </para>
        /// <para>
        /// The default implementation returns a sequence of incrementing byte values from <c>0</c> to <c>ExpectedKeySize - 1</c>. Override
        /// this method if the algorithm requires special clamping, formatting, or structure for valid key material.
        /// </para>
        /// </remarks>
        protected virtual byte[] GetDeterministicKey() =>
            Enumerable.Range(0, ExpectedKeySize).Select(i => (byte)i).ToArray();

        /// <summary>
        /// Gets the list of valid key lengths, in bytes, that are supported by the algorithm under test.
        /// </summary>
        /// <value>A non-null, read-only list of integers representing the supported key lengths (in bytes).</value>
        /// <remarks>
        /// <para>
        /// This property is used by test cases to validate key acceptance and boundary behavior across supported and unsupported sizes.
        /// Concrete test classes must override this property to return the exact set of key lengths allowed by the algorithm under test.
        /// </para>
        /// <para>
        /// Algorithms with fixed key lengths (e.g., <c>Poly1305</c>) should return a single-item list (e.g., <c>[32]</c>), while those that
        /// support a range of lengths (e.g., <c>HMAC</c>) may return multiple entries.
        /// </para>
        /// </remarks>
        protected abstract IReadOnlyList<int> ValidKeyLengths { get; }
    }
}