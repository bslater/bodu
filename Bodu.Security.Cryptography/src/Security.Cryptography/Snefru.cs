using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides the base implementation of the <c>Snefru</c> cryptographic hash algorithm - a symmetric, permutation-based hash function
    /// designed by Ralph Merkle. This class implements the core Snefru compression logic using a fixed number of S-box and rotation rounds
    /// over 512-bit blocks.
    /// </summary>
    /// <typeparam name="T">The concrete Snefru type implementing this algorithm.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="Snefru{T}" /> is a non-keyed hash function that produces fixed-size digests of either 128 or 256 bits. It was one of the
    /// earliest cryptographic hash functions developed and was submitted to the NIST hash function competition in the early 1990s. Though
    /// no longer considered secure by modern cryptographic standards, it remains an academically significant design due to its simplicity
    /// and early influence.
    /// </para>
    /// <para>This abstract base class defines the core Snefru block transformation logic and is extended by:</para>
    /// <list type="bullet">
    /// <item>
    /// <description><see cref="Snefru128" /> – Produces a 128-bit (16-byte) hash output with 4 words of internal state.</description>
    /// </item>
    /// <item>
    /// <description><see cref="Snefru256" /> – Produces a 256-bit (32-byte) hash output with 8 words of internal state.</description>
    /// </item>
    /// </list>
    /// <para>The algorithm operates in the following stages:</para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// <b>Initialization:</b> The internal state is cleared to all zeros, and each input block is padded to double the block size to
    /// facilitate buffer mixing.
    /// </description>
    /// </item>
    /// <item>
    /// <description><b>Permutation Rounds:</b> For each block, 8 rounds of transformation are applied. Each round consists of:
    /// <list type="bullet">
    /// <item>
    /// <description>An S-box substitution step based on precomputed 8-bit lookup tables.</description>
    /// </item>
    /// <item>
    /// <description>Multiple circular left bitwise rotations on 32-bit words.</description>
    /// </item>
    /// </list>
    /// These operations introduce non-linearity and diffusion to the internal buffer.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Finalization:</b> After all input is processed, the internal state is serialized in big-endian format to produce the final hash.
    /// </description>
    /// </item>
    /// </list>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public abstract partial class Snefru<T>
        : BlockHashAlgorithm<T>
        where T : Snefru<T>, new()
    {
        private const int TotalWords = 16;                              // number of 32-bit words in the working buffer.
        private static readonly int Mask = TotalWords - 1;
        private static readonly int[] Shifts = [16, 8, 16, 24];         // fixed bitwise rotation amounts applied after each S-box round.
        private static readonly int[] ValidHashSizes = { 128, 256 };

        // bitmask to constrain index calculations to the buffer length.
        private readonly uint[] buffer = new uint[TotalWords];         // internal working buffer used for permutation and round processing.

        private readonly uint[] state;                                 // internal state used to accumulate the hash output across input blocks.

        // valid Snefru hash sizes (in bits).
        private bool disposed = false;

#if !NET6_0_OR_GREATER

        // Required for .NET Standard 2.0 or older frameworks
        private bool finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Snefru{T}" /> class with the specified output hash size.
        /// </summary>
        /// <param name="hashSize">The size of the output hash, in bits. Must be either 128 or 256.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="hashSize" /> is not one of the supported values.</exception>
        protected Snefru(int hashSize)
            : base(64 - (hashSize >> 3)) // BlockSizeBytes = 64 - outputBytes
        {
            if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
                throw new ArgumentOutOfRangeException(nameof(hashSize),
                    string.Format(ResourceStrings.CryptographicException_InvalidHashSize, hashSize, string.Join(", ", ValidHashSizes)));

            this.state = new uint[hashSize >> 5];
            HashSizeValue = hashSize;

            InitializeState();
        }

        /// <summary>
        /// Gets a value indicating whether this transform instance can be reused after a hash operation is completed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the transform supports multiple hash computations via <see cref="HashAlgorithm.Initialize" />;
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks>
        /// Reusable transforms allow the internal state to be reset for subsequent operations using the same instance. One-shot algorithms
        /// that clear sensitive key material after finalization typically return <see langword="false" />.
        /// </remarks>
        public override bool CanReuseTransform => true;

        /// <summary>
        /// Gets a value indicating whether this transform supports processing multiple blocks of data in a single operation.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if multiple input blocks can be transformed in sequence without intermediate finalization; otherwise, <see langword="false" />.
        /// </value>
        /// <remarks>
        /// Most hash algorithms and block ciphers support multi-block transformations for streaming input. If <see langword="false" />, the
        /// transform must be invoked one block at a time.
        /// </remarks>
        public override bool CanTransformMultipleBlocks => true;

        /// <inheritdoc />
        public override void Initialize()
        {
            this.ThrowIfDisposed();

            base.Initialize();
            InitializeState();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
        /// </param>
        /// <remarks>Ensures all internal secrets are overwritten with zeros before releasing resources.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) return;

            if (disposing)
            {
                CryptoHelpers.Clear(MemoryMarshal.AsBytes(this.buffer.AsSpan()));
                CryptoHelpers.Clear(MemoryMarshal.AsBytes(this.state.AsSpan()));
                CryptoHelpers.ClearAndNullify(ref HashValue);
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Pads the final input block for the <c>Snefru</c> hash algorithm by appending zeros and encoding the total message length.
        /// </summary>
        /// <param name="block">The final block of unprocessed input, typically containing fewer than <c>BlockSize</c> bytes.</param>
        /// <param name="messageLength">The total number of bytes processed prior to this block (excluding the current partial block).</param>
        /// <returns>
        /// A padded byte array of exactly <c>2 × BlockSize</c> bytes, containing the input block followed by zeros and an 8-byte big-endian
        /// length field. The result is aligned for final compression and ready for use by <see cref="ProcessBlock(ReadOnlySpan{byte})" />.
        /// </returns>
        /// <remarks>
        /// Snefru's final padding block is double the standard block size to support its dual-block internal buffer design. The method pads
        /// the input block with zeros and appends a 64-bit big-endian integer representing the total message length (in bits).
        /// </remarks>
        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
        {
            int paddedLength = 2 * BlockSizeBytes;
            Span<byte> padded = paddedLength <= 128 ? stackalloc byte[128] : new byte[paddedLength];
            block.CopyTo(padded);
            BinaryPrimitives.WriteUInt64BigEndian(padded.Slice(paddedLength - 8), messageLength << 3);
            return padded.Slice(0, paddedLength).ToArray();
        }

        /// <summary>
        /// Transforms a single 512-bit block using Snefru S-box and rotation rounds. Updates internal state via XOR with permuted buffer values.
        /// </summary>
        /// <param name="block">The 64-byte input block to hash.</param>
        /// <remarks>
        /// The method performs 8 rounds, each consisting of 4 shifts and S-box applications, to mix input entropy into the state.
        /// </remarks>
        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
            this.state.AsSpan().CopyTo(this.buffer);
            LoadBlockToBuffer(block, this.buffer.AsSpan(this.state.Length));

            for (int round = 0; round < 8; round++)
            {
                foreach (int shift in Shifts)
                {
                    ApplySBoxRounds(round);
                    RotateWords(shift);
                }
            }

            for (int i = 0; i < this.state.Length; i++)
            {
                this.state[i] ^= this.buffer[Mask - i];
            }
        }

        /// <summary>
        /// Finalizes the hash computation by serializing the internal state to a byte array in big-endian format.
        /// </summary>
        /// <returns>The computed hash as a byte array.</returns>
        protected override byte[] ProcessFinalBlock()
        {
            byte[] output = new byte[this.state.Length * sizeof(uint)];
            WriteStateBigEndian(this.state, output);

            return output;
        }

        /// <summary>
        /// Converts a 64-byte input block into a sequence of 32-bit words in big-endian format.
        /// </summary>
        /// <param name="block">The input block to convert.</param>
        /// <param name="destination">The destination span for storing converted words.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadBlockToBuffer(ReadOnlySpan<byte> block, Span<uint> destination)
        {
            var inputWords = MemoryMarshal.Cast<byte, uint>(block);
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = BinaryPrimitives.ReverseEndianness(inputWords[i]);
            }
        }

        /// <summary>
        /// Writes a big-endian byte representation of a 32-bit word array to the given destination span.
        /// </summary>
        /// <param name="source">The source 32-bit word span.</param>
        /// <param name="destination">The output byte span to populate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteStateBigEndian(ReadOnlySpan<uint> source, Span<byte> destination)
        {
            for (int i = 0; i < source.Length; i++)
            {
                BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(i * 4, 4), source[i]);
            }
        }

        /// <summary>
        /// Applies Snefru's S-box substitution rounds using the configured constants for the given round.
        /// </summary>
        /// <param name="round">The current round index (0–7).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplySBoxRounds(int round)
        {
            for (int kk = 0; kk < this.buffer.Length; kk++)
            {
                int next = (kk + 1) & Mask;
                int last = (kk + Mask) & Mask;
                int sBoxNumber = (round << 1) + ((kk >> 1) & 0x01);

                uint sboxEntry = Constants[sBoxNumber][this.buffer[kk] & 0xff];

                this.buffer[next] ^= sboxEntry;
                this.buffer[last] ^= sboxEntry;
            }
        }

        /// <summary>
        /// Clears the internal state array to prepare for new input.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeState() => Array.Clear(this.state);

        /// <summary>
        /// Performs a circular left bitwise rotation on each word in the internal buffer.
        /// </summary>
        /// <param name="shiftAmount">The number of bits to rotate left by.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RotateWords(int shiftAmount)
        {
            for (int i = 0; i < this.buffer.Length; i++)
            {
                this.buffer[i] = (this.buffer[i] >> shiftAmount) | (this.buffer[i] << (32 - shiftAmount));
            }
        }
    }
}