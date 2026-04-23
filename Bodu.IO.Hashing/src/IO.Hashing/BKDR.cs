// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDR.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using the BKDR polynomial rolling algorithm from Kernighan and
/// Ritchie's "The C Programming Language". This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte <c>c</c> the hash is updated as <c>hash = (hash * seed) + c</c>. The <see cref="Seed" />
/// multiplier must be one of the supported values (31, 131, 1313, 13131, 131313, 1313131, 13131313, 131313131,
/// 1313131313).
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class BKDR
    : NonCryptographicHashAlgorithm
{
    /// <summary>
    /// Represents the default seed value used by the <see cref="BKDR" /> hash algorithm.
    /// </summary>
    public const uint DefaultSeed = 131U;

    private const int HashLength = 4;

    private static readonly uint[] ValidSeedValues =
    {
        31U, 131U, 1313U, 13131U, 131313U, 1313131U, 13131313U, 131313131U, 1313131313U,
    };

    private readonly uint _seed;
    private uint _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="BKDR" /> class with the default seed of <c>131</c>.
    /// </summary>
    public BKDR()
        : this(DefaultSeed)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BKDR" /> class using the specified seed multiplier.
    /// </summary>
    /// <param name="seed">
    /// The seed multiplier applied to each byte. Must be one of the supported seed constants.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="seed" /> is not one of the supported values (31, 131, 1313, 13131, 131313, 1313131,
    /// 13131313, 131313131, 1313131313).
    /// </exception>
    public BKDR(uint seed)
        : base(HashLength)
    {
        if (Array.IndexOf(ValidSeedValues, seed) == -1)
        {
            throw new ArgumentException(
                $"The value {seed} is not a supported BKDR seed.",
                nameof(seed));
        }

        this._seed = seed;
        this._workingHash = seed;
    }

    /// <summary>
    /// Gets the seed multiplier applied on each byte update.
    /// </summary>
    public uint Seed => this._seed;

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        uint v = this._workingHash;
        uint seed = this._seed;
        foreach (byte b in source)
        {
            v = (v * seed) + b;
        }

        this._workingHash = v;
    }

    /// <inheritdoc />
    public override void Reset() => this._workingHash = this._seed;

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, this._workingHash);
}
