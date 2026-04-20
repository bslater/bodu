namespace Bodu.Security.Cryptography
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using Bodu.Buffers;

    /// <summary>
    /// Provides a single-threaded Merkle tree hash implementation with configurable hash algorithm,
    /// block size, and fan-out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <img src="../images/diagrams/merkle-tree.svg" alt="Merkle tree construction — the input is sliced into blocks, each block is hashed to a leaf, leaves are grouped by fan-out F and reduced level-by-level until a single root hash remains; a partial tail block is zero-padded to the full block size before hashing." />
    /// </para>
    /// <para>
    /// Input bytes are divided into fixed-size blocks — the top row of the diagram above, with <c>blockSize</c>
    /// labelled <b>B</b>. Each block is hashed independently to form a leaf node (<em>Level 0</em>). Leaf hashes
    /// are then grouped by <c>fanOut</c> (labelled <b>F</b>, shown as 3 in the diagram) and combined into
    /// parent nodes, repeating level by level until a single root hash remains at the top.
    /// </para>
    /// <para>
    /// When the input length is not a multiple of <c>blockSize</c>, the partial tail (the dashed orange <b>B₇</b>
    /// block in the diagram) is zero-padded up to a full block before hashing, so every leaf is the same width
    /// regardless of input alignment. If the final group at any internal level contains fewer than <c>fanOut</c>
    /// children, that short group is promoted with its surviving children only — shown in the diagram as the
    /// single-edged reduction of <b>L₇</b> into <b>N₃</b>.
    /// </para>
    /// <para>
    /// Each call to a <c>ComputeHash</c> overload resets internal state, so the same instance may be
    /// reused across multiple inputs without re-construction.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Concurrent calls from multiple threads produce undefined results.
    /// For a concurrent level-worker pipeline over the same tree structure, see
    /// <see cref="ParallelMerkleTreeHash" />.
    /// </para>
    /// </remarks>
    public sealed class MerkleTreeHash : IDisposable
    {
        private readonly int blockSize;
        private readonly int fanOut;
        private readonly Func<HashAlgorithm> algorithmFactory;

        // Accumulates raw bytes for the current partial block; reused across ComputeHash calls.
        private readonly MemoryStream buffer;

        // Holds the hash values produced at the current tree level during reduction.
        private List<byte[]> currentLevel;

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
            this.algorithmFactory = algorithmFactory ?? throw new ArgumentNullException(nameof(algorithmFactory));
            this.blockSize = blockSize > 0 ? blockSize : throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be greater than zero.");
            this.fanOut = fanOut >= 2 ? fanOut : throw new ArgumentOutOfRangeException(nameof(fanOut), "Fan-out must be at least 2.");
            this.buffer = new MemoryStream(blockSize);
            this.currentLevel = new List<byte[]>();
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
            this.Reset();

            byte[] buffer = ArrayPool<byte>.Shared.Rent(this.blockSize * 4);
            try
            {
                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                    this.ProcessInput(buffer.AsSpan(0, bytesRead));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }

            return this.ComputeFinalHash();
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
            this.Reset();
            this.ProcessInput(data);
            return this.ComputeFinalHash();
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
        public byte[] ComputeHash(byte[] data, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(data);
            return this.ComputeHash(new ReadOnlySpan<byte>(data, offset, count));
        }

        /// <inheritdoc cref="ComputeHash(ReadOnlySpan{byte})"/>
        public byte[] ComputeHash(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return this.ComputeHash(new ReadOnlySpan<byte>(data));
        }

        // -----------------------------------------------------------------------------------------
        // Internal pipeline
        // -----------------------------------------------------------------------------------------

        private void Reset()
        {
            this.buffer.SetLength(0);
            this.currentLevel.Clear();
        }

        private void ProcessInput(ReadOnlySpan<byte> data)
        {
            while (!data.IsEmpty)
            {
                int toWrite = Math.Min(this.blockSize - (int)this.buffer.Length, data.Length);
                this.buffer.Write(data.Slice(0, toWrite));
                data = data.Slice(toWrite);

                if (this.buffer.Length == this.blockSize)
                {
                    this.currentLevel.Add(this.ComputeLeafHash(this.buffer));
                    this.buffer.SetLength(0);
                }
            }
        }

        private byte[] ComputeFinalHash()
        {
            // Zero-pad the partial tail block to a full block size before hashing, so that every
            // leaf is the same width regardless of input alignment. MemoryStream.SetLength fills
            // the extended region with zeros when growing.
            if (this.buffer.Length > 0)
            {
                this.buffer.SetLength(this.blockSize);
                this.currentLevel.Add(this.ComputeLeafHash(this.buffer));
                this.buffer.SetLength(0);
            }

            // Reduce level by level until a single root hash remains.
            while (this.currentLevel.Count > 1)
            {
                int hashLength = this.currentLevel[0].Length;
                var nextLevel = new List<byte[]>(this.currentLevel.Count / this.fanOut + 1);

                for (int i = 0; i < this.currentLevel.Count; i += this.fanOut)
                {
                    int groupSize = Math.Min(this.fanOut, this.currentLevel.Count - i);

                    using var bufferBuilder = new PooledBufferBuilder<byte>(hashLength * groupSize);
                    for (int j = 0; j < groupSize; j++)
                        bufferBuilder.AppendRange(this.currentLevel[i + j]);

                    nextLevel.Add(this.ComputeLeafHash(bufferBuilder.AsSpan()));
                }

                this.currentLevel = nextLevel;
            }

            return this.currentLevel[0];
        }

        // -----------------------------------------------------------------------------------------
        // Hashing helpers
        // -----------------------------------------------------------------------------------------

        private byte[] ComputeLeafHash(MemoryStream stream)
        {
            stream.Position = 0;
            using var hasher = this.algorithmFactory();
            return hasher.ComputeHash(stream);
        }

        private byte[] ComputeLeafHash(ReadOnlySpan<byte> span)
        {
            using var hasher = this.algorithmFactory();
            byte[] result = new byte[hasher.HashSize >> 3];
            if (!hasher.TryComputeHash(span, result, out int bytesWritten))
                throw new CryptographicException("The hash algorithm's destination buffer was too small.");
            if (bytesWritten == result.Length)
                return result;

            byte[] trimmed = new byte[bytesWritten];
            Buffer.BlockCopy(result, 0, trimmed, 0, bytesWritten);
            return trimmed;
        }

        // -----------------------------------------------------------------------------------------
        // Disposal
        // -----------------------------------------------------------------------------------------

        /// <inheritdoc />
        public void Dispose()
        {
            this.buffer.Dispose();
        }
    }
}