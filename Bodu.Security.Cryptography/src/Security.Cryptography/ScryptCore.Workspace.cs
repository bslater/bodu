// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.Workspace.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// Owns the working memory of one ROMix worker - the <c>N</c> units of <c>V</c> and one unit of scratch - in native
    /// memory, and clears it when released.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The memory comes from a <see cref="NativeBufferPool" />, so a derivation neither allocates a large managed array
    /// nor leaves one for a gen2 collection. On disposal every word the workspace spans is cleared before the buffer
    /// goes back to the pool, which is what keeps every pooled buffer all zero.
    /// </para>
    /// <para>
    /// ROMix writes every unit of <c>V</c>, and the scratch, before it reads them. A workspace therefore never needs to
    /// start zeroed, and a freshly allocated, uninitialized buffer is as good as a pooled one.
    /// </para>
    /// </remarks>
    internal sealed unsafe class Workspace
        : IDisposable
    {
        /// <summary>The pool the buffer came from and returns to.</summary>
        private readonly NativeBufferPool _pool;

        /// <summary>The size of the buffer, in bytes, which may exceed the words in use.</summary>
        private readonly nuint _capacity;

        /// <summary>The number of words in <c>V</c>: <c>N</c> units of 32·r words.</summary>
        private readonly int _chainWords;

        /// <summary>The number of words in one unit: 32·r.</summary>
        private readonly int _unitWords;

        /// <summary>The words of <c>V</c> followed by the scratch unit, or <see langword="null" /> once released.</summary>
        private uint* _words;

        /// <summary>
        /// Initializes a new instance of the <see cref="Workspace" /> class over a buffer taken from a pool.
        /// </summary>
        /// <param name="pool">The pool the buffer came from.</param>
        /// <param name="words">The buffer.</param>
        /// <param name="capacity">The buffer's size, in bytes.</param>
        /// <param name="chainWords">The number of words in <c>V</c>.</param>
        /// <param name="unitWords">The number of words in one unit.</param>
        private Workspace(NativeBufferPool pool, uint* words, nuint capacity, int chainWords, int unitWords)
        {
            _pool = pool;
            _words = words;
            _capacity = capacity;
            _chainWords = chainWords;
            _unitWords = unitWords;
        }

        /// <summary>
        /// Gets the <c>N</c> units of <c>V</c>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The workspace has been released.</exception>
        internal Span<uint> Chain
        {
            get
            {
                ObjectDisposedException.ThrowIf(_words is null, this);

                return new Span<uint>(_words, _chainWords);
            }
        }

        /// <summary>
        /// Gets the unit of scratch that ROMix alternates with its block.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The workspace has been released.</exception>
        internal Span<uint> Scratch
        {
            get
            {
                ObjectDisposedException.ThrowIf(_words is null, this);

                return new Span<uint>(_words + _chainWords, _unitWords);
            }
        }

        /// <summary>
        /// Obtains a workspace for units of the specified size from the specified pool.
        /// </summary>
        /// <param name="costN">The CPU/memory cost parameter <c>N</c>.</param>
        /// <param name="unitWords">The number of words in one unit: 32·r.</param>
        /// <param name="pool">The pool to take the buffer from and return it to.</param>
        /// <returns>A workspace the caller owns and must dispose.</returns>
        /// <exception cref="OutOfMemoryException">The workspace cannot be allocated.</exception>
        internal static Workspace Rent(int costN, int unitWords, NativeBufferPool pool)
        {
            int chainWords = costN * unitWords;
            byte* buffer = pool.Rent((nuint)(chainWords + unitWords) * sizeof(uint), out nuint capacity);
            return new Workspace(pool, (uint*)buffer, capacity, chainWords, unitWords);
        }

        /// <summary>
        /// Clears every word in use and returns the buffer to its pool.
        /// </summary>
        public void Dispose()
        {
            uint* words = _words;
            if (words is null)
                return;

            _words = null;

            // Only the words in use can hold anything; the rest of a larger pooled buffer is still zero from its last use.
            NativeBufferPool.Clear((byte*)words, (nuint)(_chainWords + _unitWords) * sizeof(uint));
            _pool.Return((byte*)words, _capacity);
        }
    }
}
