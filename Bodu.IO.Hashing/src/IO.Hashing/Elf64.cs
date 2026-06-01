// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 64-bit non-cryptographic hash using the ELF (Executable and Linkable Format) hash algorithm originally
/// used in UNIX System V object files. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// ELF hashing shifts and folds the running hash for each byte of input, periodically XORing the high bits back into
/// the low bits. An optional <see cref="Seed" /> may be supplied to alter the initial state and is reconfigurable only
/// while the algorithm has not yet consumed any input. <see cref="Reset" /> returns the instance to the reconfigurable
/// state.
/// </para>
/// <para>
/// <strong>When to choose ELF64.</strong> The ELF hash is the symbol-table hash baked into UNIX System V object files
/// and the GNU dynamic linker's <c>.gnu.hash</c> section — pick it when interoperating with ELF-format tooling or when
/// reproducing a digest computed by the system linker. For general-purpose hash-table keying prefer
/// <see cref="Fnv1a64" /> (better avalanche on the same per-byte cost) or <see cref="MurmurHash3_128" /> (much better
/// distribution on inputs longer than ~16 bytes). The 64-bit width makes ELF64 a reasonable choice for medium-sized
/// identifier maps where 32 bits would invite collisions.
/// </para>
/// <para>
/// <strong>Output and lifecycle.</strong> Produces a 64-bit (8-byte) digest in little-endian byte order.
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
/// var elf = new Elf64();
/// byte[] digest = elf.ComputeHash(System.Text.Encoding.ASCII.GetBytes("symbol_name"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class Elf64
    : NonCryptographicHashAlgorithm
{
    private const int HashLength = 8;
    private const ulong HighBitsMask = 0xF000000000000000UL;
    private const int HighBitsShift = 56;

    private ulong _seed;
    private bool _started;
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
        _seed = seed;
        _workingHash = seed;
    }

    /// <summary>
    /// Gets or sets the initial seed applied to the running hash accumulator.
    /// </summary>
    /// <value>The seed value applied before hashing begins.</value>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The algorithm has already consumed input and cannot be reconfigured until <see cref="Reset" /> is invoked.
    /// </exception>
    public ulong Seed
    {
        get => _seed;

        set
        {
            ThrowIfInvalidState();
            _seed = value;
            _workingHash = value;
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
            return;

        var v = _workingHash;
        foreach (var b in source)
        {
            v = (v << 4) + b;

            var high = v & HighBitsMask;
            v ^= high >> HighBitsShift;
            v &= ~high;
        }

        _workingHash = v;
        _started = true;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _workingHash = _seed;
        _started = false;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt64BigEndian(destination, _workingHash);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState() =>
        HashingThrowHelper.ThrowIfAlgorithmAlreadyStarted(_started);
}
