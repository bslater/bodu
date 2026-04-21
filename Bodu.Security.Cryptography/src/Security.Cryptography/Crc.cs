// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Crc.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes CRC (Cyclic Redundancy Check) values for arbitrary input data using a configurable <see cref="CrcStandard" />. This class
    /// cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Crc" /> supports CRC widths from 1 to 64 bits and honours the polynomial, initial value, input/output reflection, and
    /// final XOR value supplied by <see cref="CrcStandard" />. Precomputed lookup tables are cached via <see cref="GlobalCache" /> and shared
    /// across instances that use identical parameters.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class Crc
        : System.Security.Cryptography.HashAlgorithm
        , IResumableHashAlgorithm
    {
        private static Lazy<CrcLookupTableCache> globalLookupTableCache = new Lazy<CrcLookupTableCache>(() => new CrcLookupTableCache());

        private readonly CrcStandard standard;
        private readonly int hashSizeBytes;
        private ulong[] lookupTable;
        private ulong workingHash;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Crc" /> class using the default CRC standard (CRC-32/ISO-HDLC).
        /// </summary>
        /// <remarks>
        /// The default standard is CRC-32 (ISO-HDLC) with width 32, polynomial <c>0x04C11DB7</c>, initial value <c>0xFFFFFFFF</c>, reflected
        /// input and output, and final XOR <c>0xFFFFFFFF</c>.
        /// </remarks>
        public Crc()
            : this(CrcStandard.CRC32_ISOHDLC)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Crc" /> class using the specified <see cref="CrcStandard" />.
        /// </summary>
        /// <param name="crcStandard">The CRC parameters (polynomial, width, reflection, initial value, final XOR) to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="crcStandard" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <see cref="CrcStandard.Size" /> of <paramref name="crcStandard" /> is outside the supported range (1 to 64 bits).
        /// </exception>
        public Crc(CrcStandard crcStandard)
        {
            ThrowHelper.ThrowIfNull(crcStandard);

            this.standard = crcStandard;
            this.lookupTable = GlobalCache.GetLookupTable(crcStandard.Size, crcStandard.Polynomial, crcStandard.ReflectIn);

            HashSizeValue = crcStandard.Size;
            this.hashSizeBytes = (HashSizeValue + 7) / 8;
            this.workingHash = ComputeInitialState();
        }

        /// <summary>
        /// Gets or sets the process-wide cache used to share CRC lookup tables across <see cref="Crc" /> instances.
        /// </summary>
        /// <value>The active <see cref="CrcLookupTableCache" />. A default cache is lazily created when first accessed.</value>
        /// <exception cref="ArgumentNullException">The value being assigned is <see langword="null" />.</exception>
        public static CrcLookupTableCache GlobalCache
        {
            get => globalLookupTableCache.Value;

            set
            {
                ThrowHelper.ThrowIfNull(value);
                globalLookupTableCache = new Lazy<CrcLookupTableCache>(() => value);
            }
        }

        /// <inheritdoc />
        public override bool CanReuseTransform => true;

        /// <inheritdoc />
        public override bool CanTransformMultipleBlocks => true;

        /// <summary>
        /// Gets the <see cref="CrcStandard" /> parameters that configure this instance.
        /// </summary>
        /// <value>The immutable <see cref="CrcStandard" /> supplied to the constructor.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public CrcStandard CrcStandard
        {
            get { this.ThrowIfDisposed(); return this.standard; }
        }

        /// <summary>
        /// Gets the initial value used in the CRC calculation.
        /// </summary>
        /// <value>The initial value for the CRC calculation.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public ulong InitialValue
        {
            get { this.ThrowIfDisposed(); return this.standard.InitialValue; }
        }

        /// <summary>
        /// Gets the name of the CRC standard.
        /// </summary>
        /// <value>The name of the CRC algorithm.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public string Name
        {
            get { this.ThrowIfDisposed(); return this.standard.Name; }
        }

        /// <summary>
        /// Gets the polynomial used in the CRC calculation.
        /// </summary>
        /// <value>The polynomial value used in the CRC calculation.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public ulong Polynomial
        {
            get { this.ThrowIfDisposed(); return this.standard.Polynomial; }
        }

        /// <summary>
        /// Gets a value indicating whether input bytes are reflected (bit-reversed) before being processed.
        /// </summary>
        /// <value><see langword="true" /> if input bytes are reflected; otherwise, <see langword="false" />.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public bool ReflectIn
        {
            get { this.ThrowIfDisposed(); return this.standard.ReflectIn; }
        }

        /// <summary>
        /// Gets a value indicating whether the CRC result is reflected before XORing with <see cref="XOrOut" />.
        /// </summary>
        /// <value><see langword="true" /> if the result is reflected; otherwise, <see langword="false" />.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public bool ReflectOut
        {
            get { this.ThrowIfDisposed(); return this.standard.ReflectOut; }
        }

        /// <summary>
        /// Gets the size, in bits, of the CRC checksum.
        /// </summary>
        /// <value>The size of the CRC in bits.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public int Size
        {
            get { this.ThrowIfDisposed(); return this.standard.Size; }
        }

        /// <summary>
        /// Gets the value to XOR the final CRC result with.
        /// </summary>
        /// <value>The XOR value for the final CRC result.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        public ulong XOrOut
        {
            get { this.ThrowIfDisposed(); return this.standard.XOrOut; }
        }

        /// <summary>
        /// Computes and returns the CRC hash of the specified input in a single call, resetting internal state first.
        /// </summary>
        /// <param name="data">The input data to hash.</param>
        /// <returns>A byte array containing the finalized CRC value, sized according to <see cref="Size" />.</returns>
        public byte[] ComputeHash(ReadOnlySpan<byte> data)
        {
            Initialize();
            ProcessBlocks(data);

            byte[] buffer = new byte[this.hashSizeBytes];
            TryFinalizeHash(buffer, out _);
            return buffer;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This overload reverses finalization on <paramref name="previousHash" /> by undoing XOR and reflection (if applicable), continues
        /// the CRC computation with the full <paramref name="newData" /> array, and returns the finalized CRC hash value as a new byte array.
        /// </remarks>
        public byte[] ComputeHashFrom(byte[] previousHash, byte[] newData)
        {
            ThrowHelper.ThrowIfNull(previousHash);
            ThrowHelper.ThrowIfNull(newData);

            return ComputeHashFrom(previousHash.AsSpan(), newData.AsSpan());
        }

        /// <inheritdoc />
        /// <remarks>
        /// This overload reverses finalization on <paramref name="previousHash" /> by undoing XOR and reflection (if applicable), continues
        /// the CRC computation with a sliced segment of <paramref name="newData" /> (starting at <paramref name="offset" /> and spanning
        /// <paramref name="length" />), and returns the finalized CRC hash value as a new byte array.
        /// </remarks>
        public byte[] ComputeHashFrom(byte[] previousHash, byte[] newData, int offset, int length)
        {
            ThrowHelper.ThrowIfNull(previousHash);
            ThrowHelper.ThrowIfNull(newData);

            return ComputeHashFrom(previousHash.AsSpan(), newData.AsSpan().Slice(offset, length));
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method reverses finalization on the provided <paramref name="previousHash" />, resumes the CRC computation with the
        /// contents of <paramref name="newData" />, and returns the final CRC hash as a new byte array.
        /// </remarks>
        public byte[] ComputeHashFrom(ReadOnlySpan<byte> previousHash, ReadOnlySpan<byte> newData)
        {
            byte[] buffer = new byte[this.hashSizeBytes];
            TryComputeHashFrom(previousHash, newData, buffer, out _);
            return buffer;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
            => obj is Crc other && this.standard.Equals(other.standard);

        /// <inheritdoc />
        public override int GetHashCode()
            => this.standard.GetHashCode();

        /// <inheritdoc />
        public override void Initialize()
        {
            this.ThrowIfDisposed();
            this.workingHash = ComputeInitialState();
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method reverses finalization on <paramref name="previousHash" /> by undoing the XOR and reflection (if applicable),
        /// continues the CRC computation with <paramref name="newData" />, and finalizes the result into <paramref name="destination" />.
        /// </remarks>
        public bool TryComputeHashFrom(ReadOnlySpan<byte> previousHash, ReadOnlySpan<byte> newData, Span<byte> destination, out int bytesWritten)
        {
            this.ThrowIfDisposed();

            if (previousHash.Length != this.hashSizeBytes)
                throw new ArgumentException("Hash length does not match the expected length.", nameof(previousHash));

            // Deserialise prior hash value.
            this.workingHash = HashSizeValue <= 32
                ? BinaryPrimitives.ReadUInt32LittleEndian(previousHash)
                : BinaryPrimitives.ReadUInt64LittleEndian(previousHash);

            // Undo finalisation (XOR first, then reflect back to the working-state orientation).
            this.workingHash ^= this.standard.XOrOut;
            if (this.standard.ReflectIn ^ this.standard.ReflectOut)
                this.workingHash = CryptoHelpers.ReflectBits(this.workingHash, HashSizeValue);

            // Continue hashing and finalise again.
            ProcessBlocks(newData);
            return TryFinalizeHash(destination, out bytesWritten);
        }

        /// <summary>
        /// Finalizes the CRC computation and writes the resulting hash value into the specified destination buffer.
        /// </summary>
        /// <param name="destination">The span to write the finalized CRC hash value into.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to <paramref name="destination" />.</param>
        /// <returns><see langword="true" /> if the hash was successfully written to the destination; otherwise, <see langword="false" />.</returns>
        /// <remarks>
        /// This method applies any final transformations required by the CRC specification, including reflection and XOR output, and
        /// serializes the internal CRC value into the provided destination buffer.
        /// </remarks>
        public bool TryFinalizeHash(Span<byte> destination, out int bytesWritten)
        {
            this.ThrowIfDisposed();

            this.workingHash = FoldOutputState(this.workingHash);

            if (destination.Length < this.hashSizeBytes)
            {
                bytesWritten = 0;
                return false;
            }

            // Write to a temp span using little-endian layout; slice to match the configured CRC width.
            Span<byte> temp = stackalloc byte[8];
            Unsafe.WriteUnaligned(ref temp[0], this.workingHash);
            temp.Slice(0, this.hashSizeBytes).CopyTo(destination);

            bytesWritten = this.hashSizeBytes;
            return true;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the algorithm and clears transient state from memory.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
        /// </param>
        /// <remarks>
        /// The lookup table is owned by <see cref="GlobalCache" /> and shared across instances, so it is only released (not zeroed) here.
        /// Per-instance transient state is cleared.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) return;

            if (disposing)
            {
                this.workingHash = 0;
                CryptoHelpers.ClearAndNullify(ref HashValue);
                this.lookupTable = null!;
                HashSizeValue = 0;
                State = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            ThrowHelper.ThrowIfNull(array);
            this.ThrowIfDisposed();

            Span<byte> span = array.AsSpan().Slice(ibStart, cbSize);
            ProcessBlocks(span);
        }

        /// <inheritdoc />
        protected override byte[] HashFinal()
        {
            byte[] result = new byte[this.hashSizeBytes];
            TryFinalizeHash(result, out _);
            return result;
        }

        /// <inheritdoc />
        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
            => TryFinalizeHash(destination, out bytesWritten);

        /// <summary>
        /// Returns the working-state representation of <see cref="CrcStandard.InitialValue" />, applying input reflection when required.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong ComputeInitialState()
            => this.standard.ReflectIn
                ? CryptoHelpers.ReflectBits(this.standard.InitialValue, HashSizeValue)
                : this.standard.InitialValue;

        /// <summary>
        /// Applies final output reflection, XOR, and width-mask to the supplied working CRC value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong FoldOutputState(ulong value)
        {
            if (this.standard.ReflectIn ^ this.standard.ReflectOut)
                value = CryptoHelpers.ReflectBits(value, HashSizeValue);

            value ^= this.standard.XOrOut;
            value &= ulong.MaxValue >> (64 - HashSizeValue);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ProcessBitwiseNormal(ReadOnlySpan<byte> data, ulong crc, ulong[] table, int shift)
        {
            foreach (byte b in data)
            {
                for (int i = 0; i < 8; i++)
                {
                    ulong inputBit = (ulong)((b >> (7 - i)) & 1);
                    ulong crcBit = (crc >> shift) & 1;
                    crc = (crc << 1) ^ table[inputBit ^ crcBit];
                }
            }
            return crc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ProcessBitwiseReflected(ReadOnlySpan<byte> data, ulong crc, ulong[] table)
        {
            foreach (byte b in data)
            {
                for (int i = 0; i < 8; i++)
                {
                    ulong inputBit = (ulong)((b >> i) & 1);
                    ulong crcBit = crc & 1;
                    crc = (crc >> 1) ^ table[inputBit ^ crcBit];
                }
            }
            return crc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ProcessBytewiseNormal(ReadOnlySpan<byte> data, ulong crc, ulong[] table, int shift)
        {
            foreach (byte b in data)
            {
                crc = (crc << 8) ^ table[(byte)((crc >> shift) ^ b)];
            }
            return crc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ProcessBytewiseReflected(ReadOnlySpan<byte> data, ulong crc, ulong[] table)
        {
            foreach (byte b in data)
            {
                crc = (crc >> 8) ^ table[(byte)(crc ^ b)];
            }
            return crc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessBlocks(ReadOnlySpan<byte> data)
        {
            if (HashSizeValue >= 8)
            {
                this.workingHash = this.standard.ReflectIn
                    ? ProcessBytewiseReflected(data, this.workingHash, this.lookupTable)
                    : ProcessBytewiseNormal(data, this.workingHash, this.lookupTable, HashSizeValue - 8);
            }
            else
            {
                this.workingHash = this.standard.ReflectIn
                    ? ProcessBitwiseReflected(data, this.workingHash, this.lookupTable)
                    : ProcessBitwiseNormal(data, this.workingHash, this.lookupTable, HashSizeValue - 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(this.disposed, this);
#else
            if (this.disposed)
                throw new ObjectDisposedException(nameof(Crc));
#endif
        }
    }
}
