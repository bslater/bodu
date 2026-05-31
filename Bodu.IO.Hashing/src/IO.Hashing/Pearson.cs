// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Pearson.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>Pearson</c> hash algorithm. This variant applies a
/// non-cryptographic permutation-based transformation using a 256-byte lookup table to produce compact hash values.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The <see href="https://en.wikipedia.org/wiki/Pearson_hashing">Pearson hashing algorithm</see>, introduced by Peter
/// K. Pearson in 1990, computes a fixed-size hash (typically 8-bit or larger) by transforming each byte of the input
/// using a 256-element permutation table.
/// </para>
/// <para>
/// When computing multi-byte hashes (for example a 64-bit digest), the algorithm is repeated for each byte of the
/// result, using a different initialization for each output byte to reduce collisions.
/// </para>
/// <para>
/// <strong>When to choose Pearson.</strong> Pearson is interesting in two niches: extremely small lookup tables — a
/// Pearson byte is the cheapest way to hash a key into a 256-bucket index — and resource-poor embedded targets where a
/// 256-byte permutation table is small enough to fit in cache and a 64-bit-sized hash can be assembled from eight
/// independent 8-bit hashes without ever needing wide-integer arithmetic. For modern hash-table workloads with
/// realistic key spaces, prefer <see cref="Fnv1a32" /> / <see cref="Fnv1a64" /> or <see cref="MurmurHash3{T}" />;
/// Pearson's per-byte distribution is weaker than those alternatives.
/// </para>
/// <para>
/// <strong>Output and lifecycle.</strong> Output size is set by the constructor's <c>hashSize</c> argument (a multiple
/// of 8 bits, typically 8, 16, 32, or 64). The digest is emitted in little-endian byte order.
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> is non-destructive; instances are
/// not thread-safe.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// 8-byte (64-bit) Pearson digest assembled from eight permutation passes,
/// using one of the canonical lookup tables.
/// var pearson = new Pearson(hashSizeBits: 64, PearsonTableType.Pearson);
/// byte[] digest = pearson.ComputeHash(System.Text.Encoding.UTF8.GetBytes("payload"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed partial class Pearson
    : NonCryptographicHashAlgorithm
{
    /// <summary>
    /// The maximum allowable hash size in bits.
    /// </summary>
    public const int MaxHashSizeBits = 2048;

    /// <summary>
    /// The minimum allowable hash size in bits.
    /// </summary>
    public const int MinHashSizeBits = 8;

    private readonly byte[] _permutationTable;
    private readonly byte[] _workingHash;

    private bool _isFirstByte;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pearson" /> class with an 8-bit hash size and the canonical Pearson
    /// permutation table.
    /// </summary>
    public Pearson()
        : this(MinHashSizeBits, PearsonTableType.Pearson)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pearson" /> class with the specified hash size and a predefined
    /// permutation table.
    /// </summary>
    /// <param name="hashSizeBits">
    /// The size of the produced digest in bits. Must be a multiple of 8 in the inclusive range [
    /// <see cref="MinHashSizeBits" />, <see cref="MaxHashSizeBits" />].
    /// </param>
    /// <param name="tableType">
    /// One of the predefined permutation tables. To supply a custom permutation, use the
    /// <see cref="Pearson(int, byte[])" /> overload instead.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSizeBits" /> is outside the inclusive range [<see cref="MinHashSizeBits" />,
    /// <see cref="MaxHashSizeBits" />] or is not a multiple of 8.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="tableType" /> is <see cref="PearsonTableType.UserDefined" />.
    /// </exception>
    public Pearson(int hashSizeBits, PearsonTableType tableType)
        : base(ValidateHashSize(hashSizeBits) / 8)
    {
        if (tableType == PearsonTableType.UserDefined)
        {
            throw new ArgumentException(
                HashingResourceStrings.Arg_Invalid_PearsonTableTypeOverloadRequired,
                nameof(tableType));
        }

        TableType = tableType;
        _permutationTable = GetPermutationTable(tableType);
        _workingHash = new byte[hashSizeBits / 8];
        _isFirstByte = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pearson" /> class with the specified hash size and a
    /// caller-supplied permutation table.
    /// </summary>
    /// <param name="hashSizeBits">
    /// The size of the produced digest in bits. Must be a multiple of 8 in the inclusive range [
    /// <see cref="MinHashSizeBits" />, <see cref="MaxHashSizeBits" />].
    /// </param>
    /// <param name="permutationTable">A 256-byte permutation containing every byte value 0..255 exactly once.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="permutationTable" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="permutationTable" /> is not a 256-element permutation of 0..255.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSizeBits" /> is outside the inclusive range [<see cref="MinHashSizeBits" />,
    /// <see cref="MaxHashSizeBits" />] or is not a multiple of 8.
    /// </exception>
    public Pearson(int hashSizeBits, byte[] permutationTable)
        : base(ValidateHashSize(hashSizeBits) / 8)
    {
        ArgumentNullException.ThrowIfNull(permutationTable);
        if (permutationTable.Length != 256 || permutationTable.Distinct().Count() != 256)
            throw new ArgumentException(HashingResourceStrings.Arg_Invalid_PearsonTable, nameof(permutationTable));

        TableType = PearsonTableType.UserDefined;
        _permutationTable = (byte[])permutationTable.Clone();
        _workingHash = new byte[hashSizeBits / 8];
        _isFirstByte = true;
    }

    /// <summary>
    /// Defines the available permutation table presets that can be used with the <see cref="Pearson" /> hashing
    /// algorithm.
    /// </summary>
    public enum PearsonTableType
    {
        /// <summary>
        /// The original Pearson 1990 permutation.
        /// </summary>
        Pearson,

        /// <summary>
        /// The AES S-box, used as a permutation table.
        /// </summary>
        AESSBox,

        /// <summary>
        /// The high-byte lookup of the standard CRC-32 polynomial.
        /// </summary>
        CRC32HighByte,

        /// <summary>
        /// A permutation derived from the first 64 SHA-256 round constants (K values).
        /// </summary>
        SHA256Constants,

        /// <summary>
        /// A caller-supplied permutation table.
        /// </summary>
        UserDefined,
    }

    /// <summary>
    /// Gets a copy of the 256-byte permutation table currently in use.
    /// </summary>
    public byte[] Table => (byte[])_permutationTable.Clone();

    /// <summary>
    /// Gets the permutation table preset selected for this instance.
    /// </summary>
    public PearsonTableType TableType { get; }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
            return;

        ReadOnlySpan<byte> table = _permutationTable;
        var v = _workingHash;
        var offset = 0;

        if (_isFirstByte)
        {
            var b = source[0];
            for (var j = 0; j < v.Length; j++)
                v[j] = table[(b + j) & 0xFF];

            _isFirstByte = false;
            offset = 1;
        }

        for (var i = offset; i < source.Length; i++)
        {
            var b = source[i];
            for (var j = 0; j < v.Length; j++)
                v[j] = table[v[j] ^ b];
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        Array.Clear(_workingHash, 0, _workingHash.Length);
        _isFirstByte = true;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        _workingHash.AsSpan().CopyTo(destination);

    private static byte[] GetPermutationTable(PearsonTableType type) => type switch
    {
        PearsonTableType.Pearson => (byte[])s_pearsonTable.Value.Clone(),
        PearsonTableType.AESSBox => (byte[])s_aESSBoxTable.Value.Clone(),
        PearsonTableType.CRC32HighByte => (byte[])s_cRC32HighByteTable.Value.Clone(),
        PearsonTableType.SHA256Constants => (byte[])s_sHA256ConstantsTable.Value.Clone(),
        _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                string.Format(ResourceStrings.Arg_OutOfRange_EnumValue, type, typeof(PearsonTableType).Name))

    };

    private static int ValidateHashSize(int hashSizeBits)
    {
        ThrowHelper.ThrowIfOutOfRange(hashSizeBits, MinHashSizeBits, MaxHashSizeBits);
        ThrowHelper.ThrowIfNotPositiveMultipleOf(hashSizeBits, 8, nameof(hashSizeBits));

        return hashSizeBits;
    }
}
