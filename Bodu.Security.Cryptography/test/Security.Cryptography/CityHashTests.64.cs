// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests_64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;
using static Bodu.Security.Cryptography.CityHash64Tests;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="CityHash64" /> hash algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Known-answer test (KAT) vectors are encoded as upper-case hexadecimal strings representing the
    /// little-endian 8-byte output produced by <see cref="CityHash64" />.
    /// </para>
    /// <para>
    /// <b>KAT coverage status:</b> The empty-input vector is analytically exact — when <c>len</c> = 0,
    /// the algorithm's <c>Hash64Len0to16</c> path returns the constant <c>K2</c>
    /// (<c>0x9ae16a3b2f90404f</c>) directly, with no arithmetic. All remaining named-input and
    /// incremental KAT values require running the implementation, as <see cref="CityHash64" /> involves
    /// 64×64-bit multiply operations throughout every non-trivial code path. Populate the dictionaries in
    /// <see cref="GetExpectedHashesForNamedInputs" /> and <see cref="GetExpectedHashesForIncrementalInput" />
    /// by executing the following for each standard input, then hard-coding the result:
    /// </para>
    /// <code language="csharp">
    /// using var alg = new CityHash64();
    /// Console.WriteLine(Convert.ToHexString(alg.ComputeHash(input)));
    /// </code>
    /// <para>
    /// The base-class <see cref="HashAlgorithmTests{TTest,TAlgorithm,TVariant}" /> automatically drives
    /// KAT tests only for inputs whose names appear in both <see cref="HashAlgorithmTests{TTest,TAlgorithm,TVariant}.SharedInputs" />
    /// and the dictionaries below, so partial population does not cause test failures — it simply limits coverage.
    /// </para>
    /// </remarks>
    [TestClass]
    public partial class CityHash64Tests
        : Security.Cryptography.CityHashTests<CityHash64Tests, CityHash64>
    {
        // ---------------------------------------------------------------------------------------------------------------
        // HashAlgorithm contract properties
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="CityHash64" /> is not a block algorithm. <see cref="System.Security.Cryptography.HashAlgorithm" />
        /// returns 1 as its default input block size.
        /// </remarks>
        protected override int ExpectedInputBlockSize => 1;

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="CityHash64" /> is not a block algorithm. <see cref="System.Security.Cryptography.HashAlgorithm" />
        /// returns 1 as its default output block size.
        /// </remarks>
        protected override int ExpectedOutputBlockSize => 1;

        // ---------------------------------------------------------------------------------------------------------------
        // Factory
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        protected override CityHash64 CreateAlgorithm() => new CityHash64();

        /// <inheritdoc />
        protected override CityHash64 CreateAlgorithm(CityHashVariant variant) =>
            variant switch
            {
                CityHashVariant.Default => this.CreateAlgorithm(),
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        // ---------------------------------------------------------------------------------------------------------------
        // Known-answer tests — named inputs
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// <b>Empty</b> — analytically exact. When <c>len</c> = 0, <c>Hash64Len0to16</c> bypasses all
        /// branching and returns <c>K2</c> = <c>0x9ae16a3b2f90404f</c> directly. Stored as
        /// little-endian bytes this is <c>[0x4F, 0x40, 0x90, 0x2F, 0x3B, 0x6A, 0xE1, 0x9A]</c>.
        /// </para>
        /// <para>
        /// The remaining entries must be populated by running the implementation. Each value is the
        /// upper-case hexadecimal encoding of <c>BinaryPrimitives.WriteUInt64LittleEndian</c> output.
        /// </para>
        /// </remarks>
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(CityHashVariant variant) =>
            variant switch
            {
                CityHashVariant.Default => new Dictionary<string, string>
                {
                    // Analytically verified: the only code path that avoids 64-bit multiplication.
                    ["Empty"] = "4F40902F3B6AE19A",

                    // TODO: populate by running Convert.ToHexString(new CityHash64().ComputeHash(Encoding.ASCII.GetBytes("ABC")))
                    // ["ABC"] = "????????????????",

                    // TODO: populate by running Convert.ToHexString(new CityHash64().ComputeHash(new byte[16]))
                    // ["Zeros_16"] = "????????????????",

                    // TODO: populate by running Convert.ToHexString(new CityHash64().ComputeHash(Enumerable.Range(0,255).Select(i=>(byte)i).ToArray()))
                    // ["Sequential_0_255"] = "????????????????",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        // ---------------------------------------------------------------------------------------------------------------
        // Known-answer tests — incremental inputs
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// Each entry is the CityHash64 of the byte sequence <c>{0, 1, …, i-1}</c> where <c>i</c>
        /// is the zero-based index. The empty-input value is analytically exact; subsequent entries
        /// must be populated by running the implementation.
        /// </para>
        /// <para>
        /// To generate the complete list for lengths 0–31:
        /// </para>
        /// <code language="csharp">
        /// using var alg = new CityHash64();
        /// byte[] input = new byte[32];
        /// for (int i = 0; i &lt; 32; i++)
        /// {
        ///     if (i > 0) input[i - 1] = (byte)(i - 1);
        ///     Console.WriteLine($"\"{Convert.ToHexString(alg.ComputeHash(input, 0, i))}\",  // len={i}");
        /// }
        /// </code>
        /// </remarks>
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(CityHashVariant variant) =>
            variant switch
            {
                CityHashVariant.Default => new[]
                {
                    "4F40902F3B6AE19A", // len=0  empty  — analytically verified (K2 constant)
                    // TODO: add lengths 1-31 by running the snippet above
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        // ---------------------------------------------------------------------------------------------------------------
        // CityHash64-specific structural tests
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="CityHash64" /> reports a <see cref="System.Security.Cryptography.HashAlgorithm.HashSize" />
        /// of exactly 64 bits.
        /// </summary>
        [TestMethod]
        public void HashSize_WhenDefaultConstructed_ShouldBe64Bits()
        {
            using var algorithm = this.CreateAlgorithm();
            Assert.AreEqual(64, algorithm.HashSize);
        }

        /// <summary>
        /// Verifies that <see cref="CityHash64" /> always produces exactly 8 bytes of output for a representative
        /// range of input lengths that span all four internal algorithm paths.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenCalled_ShouldAlwaysReturn8Bytes()
        {
            using var algorithm = this.CreateAlgorithm();

            // 0–16 path, 17–32 path, 33–64 path, 65+ path.
            foreach (int len in new[] { 0, 1, 8, 16, 17, 32, 33, 64, 65, 200 })
            {
                byte[] hash = algorithm.ComputeHash(new byte[len]);
                Assert.AreEqual(8, hash.Length, $"Expected 8-byte output for input length {len}.");
            }
        }

        /// <summary>
        /// Verifies that the empty-input hash exactly equals the algorithm's <c>K2</c> constant
        /// (<c>0x9ae16a3b2f90404f</c>) stored as little-endian bytes, as mandated by the code path
        /// that returns <c>K2</c> directly when <c>len</c> = 0.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInputIsEmpty_ShouldEqualK2Constant()
        {
            // K2 = 0x9ae16a3b2f90404f encoded as little-endian bytes.
            byte[] expectedK2 = { 0x4F, 0x40, 0x90, 0x2F, 0x3B, 0x6A, 0xE1, 0x9A };

            using var algorithm = this.CreateAlgorithm();
            byte[] actual = algorithm.ComputeHash(Array.Empty<byte>());

            CollectionAssert.AreEqual(expectedK2, actual,
                "Empty-input hash must equal the K2 mixing constant in little-endian byte order.");
        }

        /// <summary>
        /// Verifies that inputs straddling each algorithm-path boundary (≤16, ≤32, ≤64, 65+)
        /// all produce distinct, non-trivial 8-byte hash values for varied, non-zero input data.
        /// </summary>
        [TestMethod]
        public void ComputeHash_AtPathBoundaries_ShouldProduceDistinctNonZeroHashes()
        {
            using var algorithm = this.CreateAlgorithm();

            // One representative length per path.
            int[] boundaryLengths = { 16, 32, 64, 65 };
            var hashes = new List<byte[]>(boundaryLengths.Length);

            foreach (int len in boundaryLengths)
            {
                // Non-zero varied input to avoid degenerate collisions.
                byte[] input = Enumerable.Range(1, len).Select(i => (byte)(i * 7)).ToArray();
                byte[] hash = algorithm.ComputeHash(input);

                Assert.IsTrue(hash.Any(b => b != 0),
                    $"Path for length {len} must not produce an all-zero hash for varied input.");

                hashes.Add(hash);
            }

            // All four path outputs must be mutually distinct.
            for (int i = 0; i < hashes.Count; i++)
            {
                for (int j = i + 1; j < hashes.Count; j++)
                {
                    CollectionAssert.AreNotEqual(hashes[i], hashes[j],
                        $"Paths at lengths {boundaryLengths[i]} and {boundaryLengths[j]} produced identical hashes.");
                }
            }
        }

        /// <summary>
        /// Verifies that inputs of lengths 0, 1, 16, 17, 32, 33, 64, and 65 — each exercising a
        /// different boundary of the four code paths — all hash deterministically (same input always
        /// produces the same output).
        /// </summary>
        [TestMethod]
        public void ComputeHash_AtPathBoundaries_ShouldBeDeterministic()
        {
            using var algorithm = this.CreateAlgorithm();

            foreach (int len in new[] { 0, 1, 16, 17, 32, 33, 64, 65 })
            {
                byte[] input = Enumerable.Range(0, len).Select(i => (byte)(i * 3 + 1)).ToArray();

                CollectionAssert.AreEqual(
                    algorithm.ComputeHash(input),
                    algorithm.ComputeHash(input),
                    $"Length {len}: repeated hashing of the same input must yield identical results.");
            }
        }

        /// <summary>
        /// Verifies that a 200-byte input — which exercises the iterative <c>Hash64Long</c> path that
        /// consumes 64-byte blocks — produces an 8-byte output with broad entropy spread across all bytes.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInputIsLong_ShouldDistributeEntropyAcrossAllBytes()
        {
            byte[] input = Enumerable.Range(0, 200).Select(i => (byte)(i * 31 + 7)).ToArray();

            using var algorithm = this.CreateAlgorithm();
            byte[] hash = algorithm.ComputeHash(input);

            Assert.AreEqual(8, hash.Length);

            // Entropy check: at least half the output bytes should be non-zero.
            int nonZeroCount = hash.Count(b => b != 0);
            Assert.IsTrue(nonZeroCount >= 4,
                $"Expected at least 4 non-zero bytes in the 8-byte hash; got {nonZeroCount}.");
        }

        /// <summary>
        /// Verifies that all four algorithm paths produce outputs that are sensitive to a single-byte
        /// change in the input, confirming basic avalanche behaviour across every path.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenSingleByteChanges_ShouldProduceDifferentHashForEveryPath()
        {
            using var algorithm = this.CreateAlgorithm();

            // Representative lengths for each of the four paths.
            foreach (int len in new[] { 8, 24, 48, 80 })
            {
                byte[] original = Enumerable.Range(0, len).Select(i => (byte)(i + 1)).ToArray();
                byte[] modified = (byte[])original.Clone();
                modified[0] ^= 0xFF;  // flip all bits in the first byte

                CollectionAssert.AreNotEqual(
                    algorithm.ComputeHash(original),
                    algorithm.ComputeHash(modified),
                    $"Length {len}: a single-byte change must produce a different hash.");
            }
        }
    }
}