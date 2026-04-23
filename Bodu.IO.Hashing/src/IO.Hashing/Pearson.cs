// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Pearson.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Linq;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>Pearson</c> hash algorithm. This variant applies a
/// non-cryptographic permutation-based transformation using a 256-byte lookup table to produce compact hash
/// values. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The <see href="https://en.wikipedia.org/wiki/Pearson_hashing">Pearson hashing algorithm</see>, introduced
/// by Peter K. Pearson in 1990, computes a fixed-size hash (typically 8-bit or larger) by transforming each
/// byte of the input using a 256-element permutation table.
/// </para>
/// <para>
/// When computing multi-byte hashes (for example a 64-bit digest), the algorithm is repeated for each byte
/// of the result, using a different initialisation for each output byte to reduce collisions.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed partial class Pearson
    : NonCryptographicHashAlgorithm
{
    /// <summary>
    /// The minimum allowable hash size in bits.
    /// </summary>
    public const int MinHashSizeBits = 8;

    /// <summary>
    /// The maximum allowable hash size in bits.
    /// </summary>
    public const int MaxHashSizeBits = 2048;

    private readonly byte[] _permutationTable;
    private readonly PearsonTableType _tableType;
    private readonly byte[] _workingHash;

    private bool _isFirstByte;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pearson" /> class with an 8-bit hash size and the
    /// canonical Pearson permutation table.
    /// </summary>
    public Pearson()
        : this(MinHashSizeBits, PearsonTableType.Pearson)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pearson" /> class with the specified hash size and a
    /// predefined permutation table.
    /// </summary>
    /// <param name="hashSizeBits">
    /// The size of the produced digest in bits. Must be a multiple of 8 in the inclusive range
    /// [<see cref="MinHashSizeBits" />, <see cref="MaxHashSizeBits" />].
    /// </param>
    /// <param name="tableType">
    /// One of the predefined permutation tables. To supply a custom permutation, use the
    /// <see cref="Pearson(int, byte[])" /> overload instead.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSizeBits" /> is outside the inclusive range
    /// [<see cref="MinHashSizeBits" />, <see cref="MaxHashSizeBits" />] or is not a multiple of 8.
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
                "Use the Pearson(int, byte[]) overload to supply a user-defined permutation table.",
                nameof(tableType));
        }

        this._tableType = tableType;
        this._permutationTable = GetPermutationTable(tableType);
        this._workingHash = new byte[hashSizeBits / 8];
        this._isFirstByte = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pearson" /> class with the specified hash size and a
    /// caller-supplied permutation table.
    /// </summary>
    /// <param name="hashSizeBits">
    /// The size of the produced digest in bits. Must be a multiple of 8 in the inclusive range
    /// [<see cref="MinHashSizeBits" />, <see cref="MaxHashSizeBits" />].
    /// </param>
    /// <param name="permutationTable">
    /// A 256-byte permutation containing every byte value 0..255 exactly once.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="permutationTable" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="permutationTable" /> is not a 256-element permutation of 0..255.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSizeBits" /> is outside the inclusive range
    /// [<see cref="MinHashSizeBits" />, <see cref="MaxHashSizeBits" />] or is not a multiple of 8.
    /// </exception>
    public Pearson(int hashSizeBits, byte[] permutationTable)
        : base(ValidateHashSize(hashSizeBits) / 8)
    {
        if (permutationTable is null)
            throw new ArgumentNullException(nameof(permutationTable));
        if (permutationTable.Length != 256 || permutationTable.Distinct().Count() != 256)
            throw new ArgumentException("Table must contain 256 unique bytes.", nameof(permutationTable));

        this._tableType = PearsonTableType.UserDefined;
        this._permutationTable = (byte[])permutationTable.Clone();
        this._workingHash = new byte[hashSizeBits / 8];
        this._isFirstByte = true;
    }

    /// <summary>
    /// Defines the available permutation table presets that can be used with the <see cref="Pearson" />
    /// hashing algorithm.
    /// </summary>
    public enum PearsonTableType
    {
        /// <summary>The original Pearson 1990 permutation.</summary>
        Pearson,

        /// <summary>The AES S-box, used as a permutation table.</summary>
        AESSBox,

        /// <summary>The high-byte lookup of the standard CRC-32 polynomial.</summary>
        CRC32HighByte,

        /// <summary>A permutation derived from the first 64 SHA-256 round constants (K values).</summary>
        SHA256Constants,

        /// <summary>A caller-supplied permutation table.</summary>
        UserDefined,
    }

    /// <summary>
    /// Gets the permutation table preset selected for this instance.
    /// </summary>
    public PearsonTableType TableType => this._tableType;

    /// <summary>
    /// Gets a copy of the 256-byte permutation table currently in use.
    /// </summary>
    public byte[] Table => (byte[])this._permutationTable.Clone();

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
            return;

        ReadOnlySpan<byte> table = this._permutationTable;
        byte[] v = this._workingHash;
        int offset = 0;

        if (this._isFirstByte)
        {
            byte b = source[0];
            for (int j = 0; j < v.Length; j++)
                v[j] = table[(b + j) & 0xFF];

            this._isFirstByte = false;
            offset = 1;
        }

        for (int i = offset; i < source.Length; i++)
        {
            byte b = source[i];
            for (int j = 0; j < v.Length; j++)
                v[j] = table[v[j] ^ b];
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        Array.Clear(this._workingHash, 0, this._workingHash.Length);
        this._isFirstByte = true;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        this._workingHash.AsSpan().CopyTo(destination);

    private static int ValidateHashSize(int hashSizeBits)
    {
        if (hashSizeBits < MinHashSizeBits || hashSizeBits > MaxHashSizeBits)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hashSizeBits),
                hashSizeBits,
                $"Hash size must be in the inclusive range [{MinHashSizeBits}, {MaxHashSizeBits}].");
        }

        if ((hashSizeBits % 8) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hashSizeBits),
                hashSizeBits,
                "Hash size must be a positive multiple of 8.");
        }

        return hashSizeBits;
    }

    private static byte[] GetPermutationTable(PearsonTableType type) => type switch
    {
        PearsonTableType.Pearson => (byte[])PearsonTable.Value.Clone(),
        PearsonTableType.AESSBox => (byte[])AESSBoxTable.Value.Clone(),
        PearsonTableType.CRC32HighByte => (byte[])CRC32HighByteTable.Value.Clone(),
        PearsonTableType.SHA256Constants => (byte[])SHA256ConstantsTable.Value.Clone(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown PearsonTableType value."),
    };
}
