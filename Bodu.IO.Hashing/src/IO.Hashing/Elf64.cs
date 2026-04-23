// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 64-bit non-cryptographic hash using the ELF (Executable and Linkable Format) hash algorithm
/// originally used in UNIX System V object files. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// ELF hashing shifts and folds the running hash for each byte of input, periodically XORing the high bits
/// back into the low bits. An optional <see cref="Seed" /> may be supplied to alter the initial state.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Elf64
    : NonCryptographicHashAlgorithm
{
    private const int HashLength = 8;
    private const ulong HighBitsMask = 0xF000000000000000UL;
    private const int HighBitsShift = 56;

    private readonly ulong _seed;
    private ulong _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="Elf64" /> class with a seed of <c>0</c>.
    /// </summary>
    public Elf64()
        : this(seed: 0UL)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Elf64" /> class using the specified initial seed.
    /// </summary>
    /// <param name="seed">The initial seed applied to the running hash accumulator.</param>
    public Elf64(ulong seed)
        : base(HashLength)
    {
        this._seed = seed;
        this._workingHash = seed;
    }

    /// <summary>
    /// Gets the initial seed applied to the running hash accumulator.
    /// </summary>
    public ulong Seed => this._seed;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Append(ReadOnlySpan<byte> source)
    {
        ulong v = this._workingHash;
        foreach (byte b in source)
        {
            v = (v << 4) + b;

            ulong high = v & HighBitsMask;
            v ^= high >> HighBitsShift;
            v &= ~high;
        }

        this._workingHash = v;
    }

    /// <inheritdoc />
    public override void Reset() => this._workingHash = this._seed;

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt64BigEndian(destination, this._workingHash);
}
