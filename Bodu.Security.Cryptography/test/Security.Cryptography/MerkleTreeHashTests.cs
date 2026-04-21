// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Unit tests for <see cref="MerkleTreeHash" />.
    /// </summary>
    /// <remarks>
    /// Inherits the shared test suite from
    /// <see cref="MerkleTreeHashTestsBase{THasher}" /> — which covers constructor validation,
    /// dispose semantics, and every byte-oriented <c>ComputeHash</c> scenario that applies equally
    /// to the sequential and parallel implementations. Tests unique to the sequential implementation
    /// (most notably the <c>ComputeHash(Stream)</c> overload, which the parallel class does not
    /// expose) live in partial files on this class.
    /// </remarks>
    [TestClass]
    public partial class MerkleTreeHashTests : MerkleTreeHashTestsBase<MerkleTreeHash>
    {
        // ─── Factory thunks — adapt the base class to MerkleTreeHash's concrete ctor/overloads ────

        /// <inheritdoc />
        protected override MerkleTreeHash Construct(
            Func<HashAlgorithm>? factory, int? blockSize = null, int? fanOut = null) =>
            new MerkleTreeHash(
                factory!,
                blockSize: blockSize ?? MerkleTestData.DefaultBlockSize,
                fanOut: fanOut ?? MerkleTestData.DefaultFanOut);

        /// <inheritdoc />
        protected override byte[] ComputeHash(MerkleTreeHash hasher, byte[] data) =>
            hasher.ComputeHash(data);

        /// <inheritdoc />
        protected override byte[] ComputeHash(MerkleTreeHash hasher, byte[] data, int offset, int count) =>
            hasher.ComputeHash(data, offset, count);

        /// <inheritdoc />
        protected override byte[] ComputeHash(MerkleTreeHash hasher, ReadOnlySpan<byte> data) =>
            hasher.ComputeHash(data);
    }
}