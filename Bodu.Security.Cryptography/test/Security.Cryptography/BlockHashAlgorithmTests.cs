// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides a reusable base class for verifying the correctness of
    /// <see cref="BlockHashAlgorithm{T}" /> implementations.
    /// </summary>
    /// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
    /// <typeparam name="TAlgorithm">
    /// The hash algorithm under test. Must derive from <see cref="BlockHashAlgorithm{TAlgorithm}" /> and
    /// expose a public parameterless constructor.
    /// </typeparam>
    /// <typeparam name="TVariant">The enumeration type used to represent algorithm configuration variants.</typeparam>
    /// <remarks>
    /// <para>
    /// Consolidates tests that are specific to the block-buffered hashing contract supplied by
    /// <see cref="BlockHashAlgorithm{T}" />: residual-buffer accumulation across transform calls,
    /// block-aligned versus unaligned input parity, padded-final-block correctness, and state reset
    /// semantics for internal buffering after <see cref="HashAlgorithm.Initialize" />.
    /// </para>
    /// <para>
    /// All test methods are declared <see langword="virtual" /> so that derived test classes may
    /// override them to introduce algorithm-specific expectations, extend coverage, or suppress a
    /// scenario that does not apply to a particular implementation (for example, one-shot MACs).
    /// </para>
    /// </remarks>
    public abstract partial class BlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
        : HashAlgorithmTests<TTest, TAlgorithm, TVariant>
        where TTest : HashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
        where TAlgorithm : BlockHashAlgorithm<TAlgorithm>, new()
        where TVariant : Enum
    {
        /// <summary>
        /// Gets the internal block size, in bytes, that the algorithm under test buffers into complete
        /// blocks before dispatching to <c>ProcessBlock</c>.
        /// </summary>
        /// <value>A positive integer matching the algorithm's configured <c>BlockSizeBytes</c> field.</value>
        /// <remarks>
        /// Derived test classes must override this property to return the exact block size declared by
        /// the concrete algorithm (for example, <c>64</c> for Tiger or <c>16</c> for SipHash). The value
        /// drives block-aligned and unaligned streaming scenarios.
        /// </remarks>
        protected abstract int ExpectedBlockSizeBytes { get; }

        /// <summary>
        /// Gets the set of chunk sizes used to verify that streaming input across arbitrary segment
        /// boundaries produces the same result as hashing the input in a single pass.
        /// </summary>
        /// <value>A non-empty sequence of positive chunk sizes spanning sub-block and supra-block regions.</value>
        /// <remarks>
        /// The default set deliberately straddles the block boundary at
        /// <see cref="ExpectedBlockSizeBytes" /> to exercise the residual-buffer path in
        /// <see cref="BlockHashAlgorithm{T}" />. Override to narrow or extend the matrix for a specific
        /// algorithm.
        /// </remarks>
        protected virtual IEnumerable<int> StreamingChunkSizes =>
            new[]
            {
                1,
                this.ExpectedBlockSizeBytes - 1,
                this.ExpectedBlockSizeBytes,
                this.ExpectedBlockSizeBytes + 1,
                (this.ExpectedBlockSizeBytes * 2) + 3,
            };

        /// <summary>
        /// Gets the set of input lengths used to verify padding of the final partial block.
        /// </summary>
        /// <value>A sequence of non-negative input lengths covering empty, sub-block, aligned, and supra-block sizes.</value>
        /// <remarks>
        /// The default set is derived from <see cref="ExpectedBlockSizeBytes" /> and is designed to
        /// exercise the full residual-length domain accepted by
        /// <see cref="BlockHashAlgorithm{T}.PadBlock" />.
        /// </remarks>
        protected virtual IEnumerable<int> UnalignedInputLengths =>
            new[]
            {
                0,
                1,
                this.ExpectedBlockSizeBytes - 1,
                this.ExpectedBlockSizeBytes,
                this.ExpectedBlockSizeBytes + 1,
                (this.ExpectedBlockSizeBytes * 2) + (this.ExpectedBlockSizeBytes / 2),
            };

    }
}
