// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Crc.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using Bodu;
using Bodu.Extensions;

/// <summary>
/// Computes CRC (Cyclic Redundancy Check) values for arbitrary input data using a configurable <see cref="CrcStandard" />.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Crc" /> supports CRC widths from 1 to 64 bits and honours the polynomial, initial value, input/output
/// reflection, and final XOR value supplied by <see cref="CrcStandard" />. Precomputed lookup tables are cached via
/// <see cref="GlobalCache" /> and shared across instances that use identical parameters.
/// </para>
/// <para>
/// Instances derive from <see cref="NonCryptographicHashAlgorithm" />, exposing the standard
/// <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" /> / <see cref="NonCryptographicHashAlgorithm.Reset" /> /
/// <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> surface. The final reflection, XOR-out, and
/// width-masking step is performed on a snapshot of the accumulator, so
/// <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> is non-destructive and may be called multiple times
/// without disturbing in-progress hashing.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Crc
    : NonCryptographicHashAlgorithm
    , IResumableHashAlgorithm
{
    private static Lazy<CrcLookupTableCache> globalLookupTableCache =
        new(() => new CrcLookupTableCache());

    private readonly CrcStandard standard;
    private readonly int hashSizeBits;
    private readonly ulong[] lookupTable;
    private ulong workingHash;

    /// <summary>
    /// Initialises a new instance of the <see cref="Crc" /> class using the default CRC standard (CRC-32/ISO-HDLC).
    /// </summary>
    /// <remarks>
    /// The default standard is CRC-32 (ISO-HDLC) with width 32, polynomial <c>0x04C11DB7</c>, initial value
    /// <c>0xFFFFFFFF</c>, reflected input and output, and final XOR <c>0xFFFFFFFF</c>.
    /// </remarks>
    public Crc()
        : this(CrcStandard.CRC32_ISOHDLC)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="Crc" /> class using the specified <see cref="CrcStandard" />.
    /// </summary>
    /// <param name="crcStandard">The CRC parameters (polynomial, width, reflection, initial value, final XOR) to use.</param>
    /// <exception cref="ArgumentNullException"><paramref name="crcStandard" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The <see cref="CrcStandard.Size" /> of <paramref name="crcStandard" /> is outside the supported range (1 to 64 bits).
    /// </exception>
    public Crc(CrcStandard crcStandard)
        : base(hashLengthInBytes: HashLengthInBytesFor(crcStandard))
    {
        this.standard = crcStandard;
        this.hashSizeBits = crcStandard.Size;
        this.lookupTable = GlobalCache.GetLookupTable(crcStandard.Size, crcStandard.Polynomial, crcStandard.ReflectIn);
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

    /// <summary>
    /// Gets the <see cref="CrcStandard" /> parameters that configure this instance.
    /// </summary>
    /// <value>The immutable <see cref="CrcStandard" /> supplied to the constructor.</value>
    public CrcStandard CrcStandard => this.standard;

    /// <summary>Gets the initial value used in the CRC calculation.</summary>
    public ulong InitialValue => this.standard.InitialValue;

    /// <summary>Gets the name of the CRC standard.</summary>
    public string Name => this.standard.Name;

    /// <summary>Gets the polynomial used in the CRC calculation.</summary>
    public ulong Polynomial => this.standard.Polynomial;

    /// <summary>Gets a value indicating whether input bytes are reflected (bit-reversed) before being processed.</summary>
    public bool ReflectIn => this.standard.ReflectIn;

    /// <summary>Gets a value indicating whether the CRC result is reflected before XOR-ing with <see cref="XOrOut" />.</summary>
    public bool ReflectOut => this.standard.ReflectOut;

    /// <summary>Gets the size, in bits, of the CRC checksum.</summary>
    public int Size => this.standard.Size;

    /// <summary>Gets the value to XOR the final CRC result with.</summary>
    public ulong XOrOut => this.standard.XOrOut;

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        this.ProcessBlocks(source);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this.workingHash = this.ComputeInitialState();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Finalisation (output reflection, XOR-out, and width-masking) is applied to a snapshot of the accumulator so that
    /// the instance remains usable for further <see cref="Append(ReadOnlySpan{byte})" /> calls after retrieval.
    /// </remarks>
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        ulong folded = this.FoldOutputState(this.workingHash);
        WriteHashBytes(folded, this.HashLengthInBytes, destination);
    }

    /// <summary>
    /// Computes and returns the CRC hash of the specified input in a single call, resetting internal state first.
    /// </summary>
    /// <param name="data">The input data to hash.</param>
    /// <returns>A byte array containing the finalised CRC value, sized according to <see cref="Size" />.</returns>
    public byte[] ComputeHash(ReadOnlySpan<byte> data)
    {
        this.Reset();
        this.ProcessBlocks(data);

        byte[] buffer = new byte[this.HashLengthInBytes];
        ulong folded = this.FoldOutputState(this.workingHash);
        WriteHashBytes(folded, this.HashLengthInBytes, buffer);
        return buffer;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reverses finalisation on <paramref name="previousHash" /> by undoing XOR-out and reflection (if applicable),
    /// continues the CRC computation with the full <paramref name="newData" /> array, and returns the finalised CRC
    /// hash value as a new byte array.
    /// </remarks>
    public byte[] ComputeHashFrom(byte[] previousHash, byte[] newData)
    {
        ThrowHelper.ThrowIfNull(previousHash);
        ThrowHelper.ThrowIfNull(newData);

        return this.ComputeHashFrom(previousHash.AsSpan(), newData.AsSpan());
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reverses finalisation on <paramref name="previousHash" /> and continues the CRC computation with a sliced segment
    /// of <paramref name="newData" /> (starting at <paramref name="offset" /> and spanning <paramref name="length" />).
    /// </remarks>
    public byte[] ComputeHashFrom(byte[] previousHash, byte[] newData, int offset, int length)
    {
        ThrowHelper.ThrowIfNull(previousHash);
        ThrowHelper.ThrowIfNull(newData);

        return this.ComputeHashFrom(previousHash.AsSpan(), newData.AsSpan().Slice(offset, length));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reverses finalisation on <paramref name="previousHash" />, resumes the CRC computation with the contents of
    /// <paramref name="newData" />, and returns the final CRC hash as a new byte array.
    /// </remarks>
    public byte[] ComputeHashFrom(ReadOnlySpan<byte> previousHash, ReadOnlySpan<byte> newData)
    {
        byte[] buffer = new byte[this.HashLengthInBytes];
        this.TryComputeHashFrom(previousHash, newData, buffer, out _);
        return buffer;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reverses finalisation on <paramref name="previousHash" /> by undoing the XOR-out and reflection (if applicable),
    /// continues the CRC computation with <paramref name="newData" />, and finalises the result into
    /// <paramref name="destination" />. After this call returns, the instance accumulator reflects the finalised state;
    /// call <see cref="Reset" /> before reusing the instance for a new computation from the initial value.
    /// </remarks>
    public bool TryComputeHashFrom(
        ReadOnlySpan<byte> previousHash,
        ReadOnlySpan<byte> newData,
        Span<byte> destination,
        out int bytesWritten)
    {
        if (previousHash.Length != this.HashLengthInBytes)
            throw new ArgumentException(
                "Hash length does not match the expected length.",
                nameof(previousHash));

        // Deserialise prior hash value. The width-byte hash is stored little-endian; read into an 8-byte buffer so
        // that widths below 64 bits zero-extend cleanly.
        Span<byte> fullWord = stackalloc byte[sizeof(ulong)];
        previousHash.CopyTo(fullWord);
        this.workingHash = BinaryPrimitives.ReadUInt64LittleEndian(fullWord);

        // Undo finalisation: XOR first, then reflect back to the working-state orientation when the algorithm applies
        // XOR-reflected output.
        this.workingHash ^= this.standard.XOrOut;
        if (this.standard.ReflectIn ^ this.standard.ReflectOut)
            this.workingHash = NumericExtensions.ReverseBitsUnchecked(this.workingHash, this.hashSizeBits);

        // Continue hashing and finalise again.
        this.ProcessBlocks(newData);

        if (destination.Length < this.HashLengthInBytes)
        {
            bytesWritten = 0;
            return false;
        }

        ulong folded = this.FoldOutputState(this.workingHash);
        WriteHashBytes(folded, this.HashLengthInBytes, destination);
        bytesWritten = this.HashLengthInBytes;
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is Crc other && this.standard.Equals(other.standard);

    /// <summary>
    /// Returns the working-state representation of <see cref="CrcStandard.InitialValue" />, applying input reflection
    /// when required.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong ComputeInitialState()
        => this.standard.ReflectIn
            ? NumericExtensions.ReverseBitsUnchecked(this.standard.InitialValue, this.hashSizeBits)
            : this.standard.InitialValue;

    /// <summary>
    /// Applies final output reflection, XOR-out, and width-masking to the supplied working CRC value.
    /// </summary>
    /// <remarks>
    /// The supplied <paramref name="value" /> is passed by value and the result is returned; no instance state is
    /// mutated. This is the load-bearing property that lets <see cref="GetCurrentHashCore" /> stay non-destructive.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong FoldOutputState(ulong value)
    {
        if (this.standard.ReflectIn ^ this.standard.ReflectOut)
            value = NumericExtensions.ReverseBitsUnchecked(value, this.hashSizeBits);

        value ^= this.standard.XOrOut;
        value &= ulong.MaxValue >> (64 - this.hashSizeBits);
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
        if (this.hashSizeBits >= 8)
        {
            this.workingHash = this.standard.ReflectIn
                ? ProcessBytewiseReflected(data, this.workingHash, this.lookupTable)
                : ProcessBytewiseNormal(data, this.workingHash, this.lookupTable, this.hashSizeBits - 8);
        }
        else
        {
            this.workingHash = this.standard.ReflectIn
                ? ProcessBitwiseReflected(data, this.workingHash, this.lookupTable)
                : ProcessBitwiseNormal(data, this.workingHash, this.lookupTable, this.hashSizeBits - 1);
        }
    }

    /// <summary>
    /// Writes the low <paramref name="byteCount" /> bytes of <paramref name="value" /> to <paramref name="destination" />
    /// in little-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteHashBytes(ulong value, int byteCount, Span<byte> destination)
    {
        Span<byte> fullWord = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(fullWord, value);
        fullWord.Slice(0, byteCount).CopyTo(destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int HashLengthInBytesFor(CrcStandard crcStandard)
    {
        ThrowHelper.ThrowIfNull(crcStandard);
        return (crcStandard.Size + 7) / 8;
    }
}
