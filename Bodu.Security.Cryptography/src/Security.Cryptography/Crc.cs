// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        // Static thread-safe global cache property
        private static Lazy<CrcLookupTableCache> globalLookupTableCache = new Lazy<CrcLookupTableCache>(() => new CrcLookupTableCache());

        private readonly int hashSizeBytes;
        private bool disposed = false;
        private ulong[] lookupTable;
        private ulong workingHash;
#if !NET6_0_OR_GREATER

        // Required for .NET Standard 2.0 or older frameworks
        private bool finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Crc" /> class using the default CRC standard (CRC32_ISOHDLC).
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

            // store the crc specification
            Name = crcStandard.Name;
            Size = crcStandard.Size;
            Polynomial = crcStandard.Polynomial;
            InitialValue = crcStandard.InitialValue;
            ReflectIn = crcStandard.ReflectIn;
            ReflectOut = crcStandard.ReflectOut;
            XOrOut = crcStandard.XOrOut;
            this.lookupTable = Crc.GlobalCache?.GetLookupTable(crcStandard.Size, crcStandard.Polynomial, crcStandard.ReflectIn).ToArray()
                ?? CrcLookupTableBuilder.BuildLookupTable(crcStandard.Size, crcStandard.Polynomial, crcStandard.ReflectIn);

            HashSizeValue = crcStandard.Size;
            this.hashSizeBytes = (HashSizeValue + 7) / 8;

            this.workingHash = ReflectIn
                ? CryptoHelpers.ReflectBits(InitialValue, HashSizeValue)
                : InitialValue;
        }

        /// <summary>
        /// Gets or sets the process-wide cache used to share CRC lookup tables across <see cref="Crc" /> instances.
        /// </summary>
        /// <value>The active <see cref="CrcLookupTableCache" />. A default cache is lazily created when first accessed.</value>
        /// <exception cref="InvalidOperationException">The value being assigned is <see langword="null" />.</exception>
        public static CrcLookupTableCache GlobalCache
        {
            get => globalLookupTableCache.Value;

            set
            {
                if (value == null)
                    throw new InvalidOperationException(ResourceStrings.InvalidOperation_CacheValueCannotBeNull);

                globalLookupTableCache = new Lazy<CrcLookupTableCache>(() => value);
            }
        }

        /// <inheritdoc />
        public override bool CanReuseTransform => true;

        /// <inheritdoc />
        public override bool CanTransformMultipleBlocks => true;

        /// <summary>
        /// Gets a snapshot of the <see cref="CrcStandard" /> parameters that configure this instance.
        /// </summary>
        /// <value>A <see cref="CrcStandard" /> containing the polynomial, width, reflection settings, initial value, and final XOR.</value>
        public CrcStandard CrcStandard => new CrcStandard(Name, Size, Polynomial, InitialValue, ReflectIn, ReflectOut, XOrOut);

        /// <summary>
        /// Gets the initial value used in the CRC calculation.
        /// </summary>
        /// <value>The initial value for the CRC calculation.</value>
        public ulong InitialValue { get; private set; }

        /// <summary>
        /// Gets the name of the CRC standard.
        /// </summary>
        /// <value>The name of the CRC algorithm.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the polynomial used in the CRC calculation.
        /// </summary>
        /// <value>The polynomial value used in the CRC calculation.</value>
        public ulong Polynomial { get; private set; }

        /// <summary>
        /// Gets a value indicating whether input bytes are reflected (bit-reversed) before being processed.
        /// </summary>
        /// <value><see langword="true" /> if input bytes are reflected; otherwise, <see langword="false" />.</value>
        public bool ReflectIn { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the CRC result is reflected before XORing with <see cref="XOrOut" />.
        /// </summary>
        /// <value><see langword="true" /> if the result is reflected; otherwise, <see langword="false" />.</value>
        public bool ReflectOut { get; private set; }

        /// <summary>
        /// Gets the size, in bits, of the CRC checksum.
        /// </summary>
        /// <value>The size of the CRC in bits.</value>
        public int Size { get; private set; }

        /// <summary>
        /// Gets the value to XOR the final CRC result with.
        /// </summary>
        /// <value>The XOR value for the final CRC result.</value>
        public ulong XOrOut { get; private set; }

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
        {
            return obj is Crc other &&
                   string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                   Size == other.Size &&
                   Polynomial == other.Polynomial &&
                   InitialValue == other.InitialValue &&
                   ReflectIn == other.ReflectIn &&
                   ReflectOut == other.ReflectOut &&
                   XOrOut == other.XOrOut;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name,
                Size,
                Polynomial,
                InitialValue,
                ReflectIn,
                ReflectOut,
                XOrOut
            );
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            State = 0;
            finalized = false;
#endif
            this.workingHash = ReflectIn
                ? CryptoHelpers.ReflectBits(InitialValue, HashSizeValue)
                : InitialValue;
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

            // Deserialize prior hash value
            this.workingHash = HashSizeValue <= 32
                ? BinaryPrimitives.ReadUInt32LittleEndian(previousHash)
                : BinaryPrimitives.ReadUInt64LittleEndian(previousHash);

            // Undo finalization
            this.workingHash ^= XOrOut;
            if (ReflectIn ^ ReflectOut)
                this.workingHash = CryptoHelpers.ReflectBits(this.workingHash, HashSizeValue);

            // Continue hashing and finalize again
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

            // Reflect final value if needed
            if (ReflectIn ^ ReflectOut)
                this.workingHash = CryptoHelpers.ReflectBits(this.workingHash, HashSizeValue);

            // Apply XOR and mask to match the width
            this.workingHash ^= XOrOut;
            this.workingHash &= ulong.MaxValue >> (64 - HashSizeValue);

            if (destination.Length < this.hashSizeBytes)
            {
                bytesWritten = 0;
                return false;
            }

            // Write to temp span using little-endian layout
            Span<byte> temp = stackalloc byte[8];
            Unsafe.WriteUnaligned(ref temp[0], this.workingHash);

            // Slice from end so we always get correct width regardless of endian
            temp.Slice(0, this.hashSizeBytes).CopyTo(destination);
            bytesWritten = this.hashSizeBytes;
            return true;
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
                this.workingHash = 0;
                if (this.lookupTable is not null)
                {
                    CryptoHelpers.ClearAndNullify(ref HashValue);
                    CryptoHelpers.Clear(this.lookupTable.AsSpan());
                    this.lookupTable = null!;
                }
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the <see cref="Crc" /> hashing algorithm. This method updates the
        /// internal state by processing <paramref name="cbSize" /> bytes starting at the specified <paramref name="ibStart" /> offset.
        /// </summary>
        /// <param name="array">The input byte array containing the data to hash.</param>
        /// <param name="ibStart">The zero-based index in <paramref name="array" /> at which to begin reading data.</param>
        /// <param name="cbSize">The number of bytes to process from <paramref name="array" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="ibStart" /> is less than 0.</para>
        /// <para>-or-</para>
        /// <para><paramref name="cbSize" /> is less than 0.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="ibStart" /> and <paramref name="cbSize" /> specify a range that exceeds the length of <paramref name="array" />.
        /// </exception>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The hash algorithm has already been finalized and cannot accept more input data.
        /// </exception>
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            ThrowHelper.ThrowIfNull(array);
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            ThrowHelper.ThrowIfLessThan(ibStart, 0);
            ThrowHelper.ThrowIfLessThan(cbSize, 0);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, offset, cbSize);
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            Span<byte> span = array.AsSpan().Slice(ibStart, cbSize);
            ProcessBlocks(span);
        }

        /// <summary>
        /// Finalizes the CRC (Cyclic Redundancy Check) computation after all input data has been processed, and returns the resulting
        /// checksum value.
        /// </summary>
        /// <returns>
        /// A byte array containing the CRC result. The length depends on the configured <see cref="HashAlgorithm.HashSize" />, which is
        /// determined by the CRC standard supplied when the instance was created (e.g., 8 bits = 1 byte, 32 bits = 4 bytes, 64 bits = 8 bytes).
        /// </returns>
        /// <remarks>
        /// The hash reflects all data previously supplied via <see cref="HashCore(byte[], int, int)" />. Once finalized, the internal state
        /// is invalidated and <see cref="HashAlgorithm.Initialize" /> must be called before reusing the instance.
        /// </remarks>
        protected override byte[] HashFinal()
        {
#if !NET6_0_OR_GREATER
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
            finalized = true;
            State = 2;
#endif

            byte[] result = new byte[this.hashSizeBytes];
            TryFinalizeHash(result, out _);
            return result;
        }

        /// <inheritdoc />
        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
#if !NET6_0_OR_GREATER
                if (finalized)
                    throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
                finalized = true;
                State = 2;
#endif

            return TryFinalizeHash(destination, out bytesWritten);
        }

        /// <summary>
        /// Processes the data using a bitwise CRC algorithm without data reflection. Each bit is processed individually, MSB first, using
        /// 1-bit shifts and permutationTable lookups.
        /// </summary>
        /// <param name="data">The input data to be processed.</param>
        /// <param name="crc">The initial CRC state value.</param>
        /// <param name="table">The CRC lookup table to use.</param>
        /// <param name="shift">The number of bits to shift to extract the high bit of the CRC.</param>
        /// <returns>The updated CRC value.</returns>
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

        /// <summary>
        /// Processes the data using a bitwise CRC algorithm with data reflection. Each bit is processed individually, LSB first, using
        /// 1-bit shifts and permutationTable lookups.
        /// </summary>
        /// <param name="data">The input data to be processed.</param>
        /// <param name="crc">The initial CRC state value.</param>
        /// <param name="table">The CRC lookup table to use.</param>
        /// <returns>The updated CRC value.</returns>
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

        /// <summary>
        /// Processes the data using a bytewise CRC algorithm without data reflection. The index is computed from the top bits of the CRC
        /// XORed with the data byte.
        /// </summary>
        /// <param name="data">The input data to be processed.</param>
        /// <param name="crc">The initial CRC state value.</param>
        /// <param name="table">The CRC lookup table to use.</param>
        /// <param name="shift">The number of bits to shift to extract the high byte of the CRC.</param>
        /// <returns>The updated CRC value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ProcessBytewiseNormal(ReadOnlySpan<byte> data, ulong crc, ulong[] table, int shift)
        {
            foreach (byte b in data)
            {
                crc = (crc << 8) ^ table[(byte)((crc >> shift) ^ b)];
            }
            return crc;
        }

        /// <summary>
        /// Processes the data using a bytewise CRC algorithm with data reflection. Each byte is XORed with the low byte of the current CRC
        /// value, then used as a permutationTable index.
        /// </summary>
        /// <param name="data">The input data to be processed.</param>
        /// <param name="crc">The initial CRC state value.</param>
        /// <param name="table">The CRC lookup table to use.</param>
        /// <returns>The updated CRC value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ProcessBytewiseReflected(ReadOnlySpan<byte> data, ulong crc, ulong[] table)
        {
            foreach (byte b in data)
            {
                crc = (crc >> 8) ^ table[(byte)(crc ^ b)];
            }
            return crc;
        }

        /// <summary>
        /// Processes the data in the provided <see cref="ReadOnlySpan{Byte}" /> and calculates the CRC hash value based on the CRC standard
        /// and reflection option.
        /// </summary>
        /// <param name="data">The array of bytes to process for CRC hashing.</param>
        /// <remarks>
        /// This method performs the core CRC calculation by iterating over the byte array, applying bitwise operations based on the CRC
        /// reflection settings. If reflection is enabled, the method processes the data with a different approach than when reflection is
        /// disabled. The CRC value is updated incrementally with each byte in the array.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessBlocks(ReadOnlySpan<byte> data)
        {
            if (HashSizeValue >= 8)
            {
                this.workingHash = ReflectIn
                    ? ProcessBytewiseReflected(data, this.workingHash, this.lookupTable)
                    : ProcessBytewiseNormal(data, this.workingHash, this.lookupTable, HashSizeValue - 8);
            }
            else
            {
                this.workingHash = ReflectIn
                    ? ProcessBitwiseReflected(data, this.workingHash, this.lookupTable)
                    : ProcessBitwiseNormal(data, this.workingHash, this.lookupTable, HashSizeValue - 1);
            }
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when any public method or property is accessed after the instance has been disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(this.disposed, this);
#else
            if (disposed)
                throw new ObjectDisposedException(nameof(Crc));
#endif
        }

        /// <summary>
        /// Throws a <see cref="CryptographicUnexpectedOperationException" /> if the hash algorithm has already started processing data,
        /// indicating that the instance is in a finalized or non-configurable state.
        /// </summary>
        /// <remarks>
        /// This method is used to prevent reconfiguration of algorithm parameters such as the key, number of rounds, or other settings once
        /// hashing has begun. It ensures settings are immutable after initialization.
        /// </remarks>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// Thrown when an attempt is made to modify the algorithm after it has entered a non-zero state, which indicates that hashing has
        /// started or been finalized.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfInvalidState()
        {
            if (State != 0)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
        }
    }
}