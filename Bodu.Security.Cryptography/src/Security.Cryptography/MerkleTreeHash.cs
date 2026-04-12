using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Bodu.Buffers;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides a single-threaded Merkle tree hash implementation with configurable hash algorithm,
    /// block size, and fan-out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Input bytes are divided into fixed-size blocks. Each block is hashed independently to form a
    /// leaf node. Leaf hashes are then grouped by <c>fanOut</c> and combined into parent nodes,
    /// repeating level by level until a single root hash remains.
    /// </para>
    /// <para>
    /// Each call to a <c>ComputeHash</c> overload resets internal state, so the same instance may be
    /// reused across multiple inputs without re-construction.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Concurrent calls from multiple threads produce undefined results.
    /// </para>
    /// </remarks>
    public sealed class MerkleTreeHash : IDisposable
    {
        private readonly int _blockSize;
        private readonly int _fanOut;
        private readonly Func<HashAlgorithm> _algorithmFactory;

        // Accumulates raw bytes for the current partial block; reused across ComputeHash calls.
        private readonly MemoryStream _buffer;

        // Holds the hash values produced at the current tree level during reduction.
        private List<byte[]> _currentLevel;

        /// <summary>
        /// Initialises a new <see cref="MerkleTreeHash"/> instance with the specified hash algorithm
        /// factory, block size, and fan-out.
        /// </summary>
        /// <param name="algorithmFactory">
        ///   Factory that returns a fresh <see cref="HashAlgorithm"/> per hash operation.
        ///   Must not be <see langword="null"/>. A distinct instance is created for each leaf and
        ///   internal node so that no algorithm state is shared across operations.
        /// </param>
        /// <param name="blockSize">
        ///   The size in bytes of each leaf block. Must be greater than zero. Defaults to 1024.
        /// </param>
        /// <param name="fanOut">
        ///   The number of child nodes combined into each parent node during tree reduction.
        ///   Must be at least 2. Defaults to 3. Larger values produce shallower trees.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithmFactory"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="blockSize"/> is less than or equal to zero, or
        ///   <paramref name="fanOut"/> is less than 2.
        /// </exception>
        public MerkleTreeHash(Func<HashAlgorithm> algorithmFactory, int blockSize = 1024, int fanOut = 3)
        {
            _algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
            _blockSize = blockSize > 0 ? blockSize : throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be greater than zero.");
            _fanOut = fanOut >= 2 ? fanOut : throw new ArgumentOutOfRangeException(nameof(fanOut), "Fan-out must be at least 2.");
            _buffer = new MemoryStream(blockSize);
            _currentLevel = new List<byte[]>();
        }

        /// <summary>
        /// Computes the Merkle root hash of the data read from <paramref name="input"/>.
        /// </summary>
        /// <param name="input">The readable stream to hash. Must not be <see langword="null"/>.</param>
        /// <returns>
        ///   A byte array containing the Merkle root hash of all data read from <paramref name="input"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        public byte[] ComputeHash(Stream input)
        {
            ArgumentNullException.ThrowIfNull(input);
            Reset();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(_blockSize * 4);
            try
            {
                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                    ProcessInput(buffer.AsSpan(0, bytesRead));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }

            return ComputeFinalHash();
        }

        /// <summary>
        /// Computes the Merkle root hash of <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The byte span to hash.</param>
        /// <returns>
        ///   A byte array containing the Merkle root hash of <paramref name="data"/>.
        /// </returns>
        public byte[] ComputeHash(ReadOnlySpan<byte> data)
        {
            Reset();
            ProcessInput(data);
            return ComputeFinalHash();
        }

        /// <summary>
        /// Computes the Merkle root hash of a region within <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The source byte array. Must not be <see langword="null"/>.</param>
        /// <param name="offset">The zero-based index at which to begin reading.</param>
        /// <param name="count">The number of bytes to hash.</param>
        /// <returns>
        ///   A byte array containing the Merkle root hash of the specified region.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="offset"/> or <paramref name="count"/> is negative, or
        ///   <paramref name="offset"/> + <paramref name="count"/> exceeds the length of <paramref name="data"/>.
        /// </exception>
        public byte[] ComputeHash(byte[] data, int offset, int count) =>
            ComputeHash(new ReadOnlySpan<byte>(data, offset, count));

        /// <inheritdoc cref="ComputeHash(ReadOnlySpan{byte})"/>
        public byte[] ComputeHash(byte[] data) =>
            ComputeHash(new ReadOnlySpan<byte>(data));

        // -----------------------------------------------------------------------------------------
        // Internal pipeline
        // -----------------------------------------------------------------------------------------

        private void Reset()
        {
            _buffer.SetLength(0);
            _currentLevel.Clear();
        }

        private void ProcessInput(ReadOnlySpan<byte> data)
        {
            while (!data.IsEmpty)
            {
                int toWrite = Math.Min(_blockSize - (int)_buffer.Length, data.Length);
                _buffer.Write(data.Slice(0, toWrite));
                data = data.Slice(toWrite);

                if (_buffer.Length == _blockSize)
                {
                    _currentLevel.Add(ComputeLeafHash(_buffer));
                    _buffer.SetLength(0);
                }
            }
        }

        private byte[] ComputeFinalHash()
        {
            // Zero-pad the partial tail block to a full block size before hashing, so that every
            // leaf is the same width regardless of input alignment. MemoryStream.SetLength fills
            // the extended region with zeros when growing.
            if (_buffer.Length > 0)
            {
                _buffer.SetLength(_blockSize);
                _currentLevel.Add(ComputeLeafHash(_buffer));
                _buffer.SetLength(0);
            }

            // Reduce level by level until a single root hash remains.
            while (_currentLevel.Count > 1)
            {
                int hashLength = _currentLevel[0].Length;
                var nextLevel = new List<byte[]>(_currentLevel.Count / _fanOut + 1);

                for (int i = 0; i < _currentLevel.Count; i += _fanOut)
                {
                    int groupSize = Math.Min(_fanOut, _currentLevel.Count - i);

                    using var bufferBuilder = new PooledBufferBuilder<byte>(hashLength * groupSize);
                    for (int j = 0; j < groupSize; j++)
                        bufferBuilder.AppendRange(_currentLevel[i + j]);

                    nextLevel.Add(ComputeLeafHash(bufferBuilder.AsSpan()));
                }

                _currentLevel = nextLevel;
            }

            return _currentLevel[0];
        }

        // -----------------------------------------------------------------------------------------
        // Hashing helpers
        // -----------------------------------------------------------------------------------------

        private byte[] ComputeLeafHash(MemoryStream stream)
        {
            stream.Position = 0;
            using var hasher = _algorithmFactory();
            return hasher.ComputeHash(stream);
        }

        private byte[] ComputeLeafHash(ReadOnlySpan<byte> span)
        {
            using var hasher = _algorithmFactory();
            byte[] temp = ArrayPool<byte>.Shared.Rent(span.Length);
            try
            {
                span.CopyTo(temp);
                return hasher.ComputeHash(temp, 0, span.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(temp, clearArray: true);
            }
        }

        // -----------------------------------------------------------------------------------------
        // Disposal
        // -----------------------------------------------------------------------------------------

        /// <inheritdoc />
        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}