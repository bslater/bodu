// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Crc.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// General-purpose CRC (Cyclic Redundancy Check) engine driven by a <see cref="CrcStandard"/> parameter set —
/// supports any catalogue width from 1 to 64 bits, snapshot-style intermediate digests, and resumption of a previous
/// digest with additional input.
/// </summary>
/// <remarks>
/// <para>
/// CRCs are the workhorse of integrity checks in storage, networking, and file formats: every <c>.zip</c>, <c>.png</c>,
/// Ethernet frame, USB packet, and Modbus message uses one. Each protocol bakes in slightly different choices —
/// polynomial, initial value, input/output bit reflection, final XOR — and the same byte sequence can produce a
/// different digest under <c>CRC-16/ARC</c>, <c>CRC-16/MODBUS</c>, or <c>CRC-32/ISO-HDLC</c>. Rather than ship a class
/// per variant, <see cref="Crc"/> consumes the parameters from a <see cref="CrcStandard"/> and the same engine
/// computes the right answer for every catalogue entry.
/// </para>
/// <para>
/// <strong>Picking a standard.</strong> The full RevEng catalogue is exposed two ways:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><see cref="CrcStandard"/> static properties</term>
///     <description>For the common entries (<see cref="CrcStandard.CRC32_ISOHDLC"/>,
///     <see cref="CrcStandard.CRC32_ISCSI"/>, <see cref="CrcStandard.CRC16_MODBUS"/>, …) — direct, allocation-free
///     references to the canonical instance.</description>
///   </item>
///   <item>
///     <term><see cref="CrcStandard.Get(CrcStandards)"/> with a <see cref="CrcStandards"/> enum value</term>
///     <description>For programmatic look-up across the full catalogue (e.g. when reading a configuration value).</description>
///   </item>
///   <item>
///     <term><see cref="CrcStandard.FromName(string)"/></term>
///     <description>Resolves both canonical names and aliases — <c>"CRC-32"</c>, <c>"PKZIP"</c>,
///     <c>"CRC-32/ISO-HDLC"</c> all return the same instance.</description>
///   </item>
/// </list>
/// <para>
/// <strong>API surface.</strong> <see cref="Crc"/> derives from
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm"/> and exposes the standard
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})"/> /
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset"/> /
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()"/> shape. The
/// <see cref="Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions"/> companion adds one-shot
/// <c>ComputeHash</c>, stream variants, and constant-time <c>VerifyHash</c> / <c>TryVerifyHash</c> on top.
/// </para>
/// <para>
/// <strong>Snapshot semantics.</strong> The final reflection, XOR-out, and width mask are applied to a
/// <em>copy</em> of the running accumulator, so <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()"/>
/// can be called as often as the caller likes without disturbing further <c>Append</c> calls — useful for emitting
/// progressive checksums of an unfinished stream.
/// </para>
/// <para>
/// <strong>Resumption.</strong> <see cref="Crc"/> implements <see cref="IResumableHashAlgorithm"/>: given a previously
/// emitted digest and additional bytes, it produces the digest of the concatenated input <em>without</em> needing the
/// original bytes back — handy for log-tail integrity checks and content-addressed storage. Resumption is only valid
/// against a digest produced by an instance configured with the same <see cref="CrcStandard"/>.
/// </para>
/// <para>
/// <strong>Performance.</strong> Lookup tables are precomputed once per
/// <c>(width, polynomial, reflectIn)</c> tuple and shared via <see cref="GlobalCache"/>, so creating multiple
/// <see cref="Crc"/> instances for the same standard is cheap. The hot path uses byte-at-a-time table lookups; for
/// throughput-critical code consider re-using a single instance and feeding it large spans rather than repeatedly
/// constructing new ones. Instances are <strong>not thread-safe</strong>; share behind explicit synchronization.
/// </para>
/// <note type="important">CRC is <strong>not</strong> cryptographically secure. It detects accidental corruption,
/// not adversarial tampering — collisions are easy to construct. Use a member of <c>Bodu.Security.Cryptography</c> or
/// <see cref="System.Security.Cryptography.HashAlgorithm"/> for password hashing, digital signatures, message
/// authentication, or any context where a determined attacker could choose the input.</note>
/// <example>
/// <code language="csharp">
/// using System.IO.Hashing;
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// // 1. Standard PKZIP / Ethernet CRC-32 of a buffer.
/// var crc32 = new Crc(CrcStandard.CRC32_ISOHDLC);
/// byte[] digest = crc32.ComputeHash(File.ReadAllBytes("payload.bin"));
///
/// // 2. Modbus RTU — different polynomial/init/reflect choices, same engine.
/// var modbus = new Crc(CrcStandard.CRC16_MODBUS);
/// modbus.Append(frameHeader);
/// modbus.Append(framePayload);
/// byte[] frameCrc = modbus.GetCurrentHash();   // non-destructive snapshot
///
/// // 3. Resumption — fold an appended log segment into yesterday's digest without re-reading the original bytes.
/// var resumable = (IResumableHashAlgorithm)new Crc(CrcStandard.CRC32_ISOHDLC);
/// byte[] updated = resumable.ComputeHashFrom(digest, File.ReadAllBytes("payload.appended.bin"));
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="CrcStandard"/>
/// <seealso cref="CrcStandards"/>
/// <seealso cref="CrcLookupTableCache"/>
/// <seealso cref="IResumableHashAlgorithm"/>
public sealed class Crc
    : NonCryptographicHashAlgorithm,
      IResumableHashAlgorithm
{

    private static Lazy<CrcLookupTableCache> s_globalLookupTableCache =
        new Lazy<CrcLookupTableCache>(() => new CrcLookupTableCache());
    private readonly int _hashSizeBits;
    private readonly ulong[] _lookupTable;

    private readonly CrcStandard _standard;
    private ulong _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="Crc" /> class using the default CRC standard (CRC-32/ISO-HDLC).
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
    /// Initializes a new instance of the <see cref="Crc" /> class using the specified <see cref="CrcStandard" />.
    /// </summary>
    /// <param name="crcStandard">The CRC parameters (polynomial, width, reflection, initial value, final XOR) to use.</param>
    /// <exception cref="ArgumentNullException"><paramref name="crcStandard" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The <see cref="CrcStandard.Size" /> of <paramref name="crcStandard" /> is outside the supported range (1 to 64 bits).
    /// </exception>
    public Crc(CrcStandard crcStandard)
        : base(hashLengthInBytes: HashLengthInBytesFor(crcStandard))
    {
        ArgumentNullException.ThrowIfNull(crcStandard);

        this._standard = crcStandard;
        this._hashSizeBits = crcStandard.Size;
        this._lookupTable = GlobalCache.GetLookupTable(crcStandard.Size, crcStandard.Polynomial, crcStandard.ReflectIn);
        this._workingHash = this.ComputeInitialState();
    }

    /// <summary>
    /// Gets or sets the process-wide cache used to share CRC lookup tables across <see cref="Crc" /> instances.
    /// </summary>
    /// <value>The active <see cref="CrcLookupTableCache" />. A default cache is lazily created when first accessed.</value>
    /// <exception cref="ArgumentNullException">The value being assigned is <see langword="null" />.</exception>
    public static CrcLookupTableCache GlobalCache
    {
        get => s_globalLookupTableCache.Value;

        set
        {
            ThrowHelper.ThrowIfNull(value);
            s_globalLookupTableCache = new Lazy<CrcLookupTableCache>(() => value);
        }
    }

    /// <summary>
    /// Gets the <see cref="CrcStandard" /> parameters that configure this instance.
    /// </summary>
    /// <value>The immutable <see cref="CrcStandard" /> supplied to the constructor.</value>
    public CrcStandard CrcStandard => this._standard;

    /// <summary>Gets the initial value used in the CRC calculation.</summary>
    public ulong InitialValue => this._standard.InitialValue;

    /// <summary>Gets the name of the CRC standard.</summary>
    public string Name => this._standard.Name;

    /// <summary>Gets the polynomial used in the CRC calculation.</summary>
    public ulong Polynomial => this._standard.Polynomial;

    /// <summary>Gets a value indicating whether input bytes are reflected (bit-reversed) before being processed.</summary>
    public bool ReflectIn => this._standard.ReflectIn;

    /// <summary>Gets a value indicating whether the CRC result is reflected before XOR-ing with <see cref="XOrOut" />.</summary>
    public bool ReflectOut => this._standard.ReflectOut;

    /// <summary>Gets the size, in bits, of the CRC checksum.</summary>
    public int Size => this._standard.Size;

    /// <summary>Gets the value to XOR the final CRC result with.</summary>
    public ulong XOrOut => this._standard.XOrOut;

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        this.ProcessBlocks(source);
    }

    /// <summary>
    /// Computes and returns the CRC hash of the specified input in a single call, resetting internal state first.
    /// </summary>
    /// <param name="data">The input data to hash.</param>
    /// <returns>A byte array containing the finalized CRC value, sized according to <see cref="Size" />.</returns>
    public byte[] ComputeHash(ReadOnlySpan<byte> data)
    {
        this.Reset();
        this.ProcessBlocks(data);

        var buffer = new byte[this.HashLengthInBytes];
        var folded = this.FoldOutputState(this._workingHash);
        WriteHashBytes(folded, this.HashLengthInBytes, buffer);
        return buffer;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reverses finalization on <paramref name="previousHash" /> by undoing XOR-out and reflection (if applicable),
    /// continues the CRC computation with the full <paramref name="newData" /> array, and returns the finalized CRC
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
    /// Reverses finalization on <paramref name="previousHash" />, resumes the CRC computation with the contents of
    /// <paramref name="newData" />, and returns the final CRC hash as a new byte array.
    /// </remarks>
    public byte[] ComputeHashFrom(ReadOnlySpan<byte> previousHash, ReadOnlySpan<byte> newData)
    {
        var buffer = new byte[this.HashLengthInBytes];
        this.TryComputeHashFrom(previousHash, newData, buffer, out _);
        return buffer;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reverses finalization on <paramref name="previousHash" /> and continues the CRC computation with a sliced segment
    /// of <paramref name="newData" /> (starting at <paramref name="offset" /> and spanning <paramref name="length" />).
    /// </remarks>
    public byte[] ComputeHashFrom(byte[] previousHash, byte[] newData, int offset, int length)
    {
        ThrowHelper.ThrowIfNull(previousHash);
        ThrowHelper.ThrowIfNull(newData);

        return this.ComputeHashFrom(previousHash.AsSpan(), newData.AsSpan().Slice(offset, length));
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this._workingHash = this.ComputeInitialState();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reverses finalization on <paramref name="previousHash" /> by undoing the XOR-out and reflection (if applicable),
    /// continues the CRC computation with <paramref name="newData" />, and finalizes the result into
    /// <paramref name="destination" />. After this call returns, the instance accumulator reflects the finalized state;
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
                HashingResourceStrings.Arg_Invalid_PreviousHashLengthMismatch,
                nameof(previousHash));

        // Deserialize prior hash value. The width-byte hash is stored little-endian; read into an 8-byte buffer so
        // that widths below 64 bits zero-extend cleanly.
        Span<byte> fullWord = stackalloc byte[sizeof(ulong)];
        previousHash.CopyTo(fullWord);
        this._workingHash = BinaryPrimitives.ReadUInt64LittleEndian(fullWord);

        // Undo finalization: XOR first, then reflect back to the working-state orientation when the algorithm applies
        // XOR-reflected output.
        this._workingHash ^= this._standard.XOrOut;
        if (this._standard.ReflectIn ^ this._standard.ReflectOut)
            this._workingHash = NumericExtensions.ReverseBitsUnchecked(this._workingHash, this._hashSizeBits);

        // Continue hashing and finalize again.
        this.ProcessBlocks(newData);

        if (destination.Length < this.HashLengthInBytes)
        {
            bytesWritten = 0;
            return false;
        }

        var folded = this.FoldOutputState(this._workingHash);
        WriteHashBytes(folded, this.HashLengthInBytes, destination);
        bytesWritten = this.HashLengthInBytes;
        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Finalization (output reflection, XOR-out, and width-masking) is applied to a snapshot of the accumulator so that
    /// the instance remains usable for further <see cref="Append(ReadOnlySpan{byte})" /> calls after retrieval.
    /// </remarks>
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        var folded = this.FoldOutputState(this._workingHash);
        WriteHashBytes(folded, this.HashLengthInBytes, destination);
    }

    /// <summary>
    /// Returns the output byte length for <paramref name="crcStandard" />, rounding up the polynomial
    /// width in bits to the next whole byte.
    /// </summary>
    /// <param name="crcStandard">The CRC standard whose output size is requested. Must not be
    /// <see langword="null" />.</param>
    /// <returns>The output length in bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="crcStandard" /> is <see langword="null" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int HashLengthInBytesFor(CrcStandard crcStandard)
    {
        ThrowHelper.ThrowIfNull(crcStandard);
        return (crcStandard.Size + 7) / 8;
    }

    /// <summary>
    /// Updates <paramref name="crc" /> by feeding <paramref name="data" /> through a non-reflected
    /// bit-by-bit CRC step using the lookup <paramref name="table" />.
    /// </summary>
    /// <param name="data">The input bytes.</param>
    /// <param name="crc">The current CRC accumulator.</param>
    /// <param name="table">The 2-entry bit-wise lookup table for the active polynomial.</param>
    /// <param name="shift">The bit offset of the MSB in the CRC register (width − 1 for wide CRCs,
    /// or width − 1 for narrow CRCs where the register is left-aligned).</param>
    /// <returns>The updated CRC accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ProcessBitwiseNormal(ReadOnlySpan<byte> data, ulong crc, ulong[] table, int shift)
    {
        foreach (var b in data)
        {
            for (var i = 0; i < 8; i++)
            {
                var inputBit = (ulong)((b >> (7 - i)) & 1);
                var crcBit = (crc >> shift) & 1;
                crc = (crc << 1) ^ table[inputBit ^ crcBit];
            }
        }
        return crc;
    }

    /// <summary>
    /// Updates <paramref name="crc" /> by feeding <paramref name="data" /> through a reflected
    /// bit-by-bit CRC step using the lookup <paramref name="table" />.
    /// </summary>
    /// <param name="data">The input bytes.</param>
    /// <param name="crc">The current CRC accumulator.</param>
    /// <param name="table">The 2-entry bit-wise lookup table for the active polynomial.</param>
    /// <returns>The updated CRC accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ProcessBitwiseReflected(ReadOnlySpan<byte> data, ulong crc, ulong[] table)
    {
        foreach (var b in data)
        {
            for (var i = 0; i < 8; i++)
            {
                var inputBit = (ulong)((b >> i) & 1);
                var crcBit = crc & 1;
                crc = (crc >> 1) ^ table[inputBit ^ crcBit];
            }
        }
        return crc;
    }

    /// <summary>
    /// Updates <paramref name="crc" /> by feeding <paramref name="data" /> through a non-reflected
    /// byte-wise CRC step using the 256-entry lookup <paramref name="table" />.
    /// </summary>
    /// <param name="data">The input bytes.</param>
    /// <param name="crc">The current CRC accumulator.</param>
    /// <param name="table">The 256-entry byte-wise lookup table for the active polynomial.</param>
    /// <param name="shift">The bit offset of the top byte in the CRC register (width − 8).</param>
    /// <returns>The updated CRC accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ProcessBytewiseNormal(ReadOnlySpan<byte> data, ulong crc, ulong[] table, int shift)
    {
        foreach (var b in data)
        {
            crc = (crc << 8) ^ table[(byte)((crc >> shift) ^ b)];
        }
        return crc;
    }

    /// <summary>
    /// Updates <paramref name="crc" /> by feeding <paramref name="data" /> through a reflected
    /// byte-wise CRC step using the 256-entry lookup <paramref name="table" />.
    /// </summary>
    /// <param name="data">The input bytes.</param>
    /// <param name="crc">The current CRC accumulator.</param>
    /// <param name="table">The 256-entry byte-wise lookup table for the active polynomial.</param>
    /// <returns>The updated CRC accumulator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ProcessBytewiseReflected(ReadOnlySpan<byte> data, ulong crc, ulong[] table)
    {
        foreach (var b in data)
        {
            crc = (crc >> 8) ^ table[(byte)(crc ^ b)];
        }
        return crc;
    }

    /// <summary>
    /// Writes the low <paramref name="byteCount" /> bytes of <paramref name="value" /> to <paramref name="destination" />
    /// in little-endian order.
    /// </summary>
    /// <param name="value">The 64-bit value whose low bytes are to be written.</param>
    /// <param name="byteCount">The number of low-order bytes to write; must be between 1 and 8.</param>
    /// <param name="destination">The destination span; must be at least <paramref name="byteCount" /> bytes long.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteHashBytes(ulong value, int byteCount, Span<byte> destination)
    {
        Span<byte> fullWord = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(fullWord, value);
        fullWord.Slice(0, byteCount).CopyTo(destination);
    }

    /// <summary>
    /// Returns the working-state representation of <see cref="CrcStandard.InitialValue" />, applying input reflection
    /// when required.
    /// </summary>
    /// <returns>The initial CRC accumulator value, bit-reflected if the standard's
    /// <see cref="CrcStandard.ReflectIn" /> flag is set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong ComputeInitialState()
        => this._standard.ReflectIn
            ? NumericExtensions.ReverseBitsUnchecked(this._standard.InitialValue, this._hashSizeBits)
            : this._standard.InitialValue;

    /// <summary>
    /// Applies final output reflection, XOR-out, and width-masking to the supplied working CRC value.
    /// </summary>
    /// <param name="value">The working CRC accumulator to finalize.</param>
    /// <returns>The finalized CRC output value, width-masked to the standard's polynomial size.</returns>
    /// <remarks>
    /// The supplied <paramref name="value" /> is passed by value and the result is returned; no instance state is
    /// mutated. This is the load-bearing property that lets <see cref="GetCurrentHashCore" /> stay non-destructive.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong FoldOutputState(ulong value)
    {
        if (this._standard.ReflectIn ^ this._standard.ReflectOut)
            value = NumericExtensions.ReverseBitsUnchecked(value, this._hashSizeBits);

        value ^= this._standard.XOrOut;
        value &= ulong.MaxValue >> (64 - this._hashSizeBits);
        return value;
    }

    /// <summary>
    /// Routes <paramref name="data" /> through the reflected or non-reflected byte-wise path
    /// when the hash width is at least a byte, or the corresponding bit-wise path otherwise.
    /// </summary>
    /// <param name="data">The input bytes to feed into the CRC accumulator.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessBlocks(ReadOnlySpan<byte> data)
    {
        if (this._hashSizeBits >= 8)
        {
            this._workingHash = this._standard.ReflectIn
                ? ProcessBytewiseReflected(data, this._workingHash, this._lookupTable)
                : ProcessBytewiseNormal(data, this._workingHash, this._lookupTable, this._hashSizeBits - 8);
        }
        else
        {
            this._workingHash = this._standard.ReflectIn
                ? ProcessBitwiseReflected(data, this._workingHash, this._lookupTable)
                : ProcessBitwiseNormal(data, this._workingHash, this._lookupTable, this._hashSizeBits - 1);
        }
    }

}
