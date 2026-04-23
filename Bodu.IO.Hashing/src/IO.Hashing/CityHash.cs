// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.IO.Hashing;

/// <summary>
/// Base class for the <c>CityHash</c> family of non-cryptographic hash algorithms developed by Google. See
/// the <a href="https://github.com/google/cityhash">CityHash reference repository</a> for the specification.
/// </summary>
/// <typeparam name="T">
/// The concrete CityHash variant derived from this class. Must expose a public parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// CityHash is a one-shot algorithm. To satisfy the incremental input contract of
/// <see cref="NonCryptographicHashAlgorithm" />, this base class accumulates all bytes delivered through
/// <see cref="Append(ReadOnlySpan{byte})" /> into an internal buffer and invokes the derived variant's
/// <see cref="ComputeHashCore(ReadOnlySpan{byte})" /> from <see cref="GetCurrentHashCore(Span{byte})" /> once
/// all input is available.
/// </para>
/// <para>
/// Shared mixing primitives (<see cref="Mix(uint)" />, <see cref="Mur(uint, uint)" />,
/// <see cref="Permute3(ref uint, ref uint, ref uint)" />) and algorithm constants are defined here and are
/// available to all derived variants. Supported output sizes are 32 and 64 bits.
/// </para>
/// <note type="important">
/// CityHash is <b>not</b> cryptographically secure. It must <b>not</b> be used for password hashing, digital
/// signatures, or any application that requires collision resistance under adversarial conditions.
/// </note>
/// </remarks>
public abstract class CityHash<T>
    : NonCryptographicHashAlgorithm
    where T : CityHash<T>, new()
{
    /// <summary>The first Murmur-style mixing constant used in 32-bit operations.</summary>
    protected const uint C1 = 0xCC9E2D51U;

    /// <summary>The second Murmur-style mixing constant used in 32-bit operations.</summary>
    protected const uint C2 = 0x1B873593U;

    /// <summary>The 32-bit finalisation magic constant applied during the iterative mixing phase.</summary>
    protected const uint HashMagic = 0xE6546B64U;

    /// <summary>The first 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
    protected const ulong K0 = 0xC3A5C85C97CB3127UL;

    /// <summary>The second 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
    protected const ulong K1 = 0xB492B66FBE98F273UL;

    /// <summary>The third 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
    protected const ulong K2 = 0x9AE16A3B2F90404FUL;

    private static readonly int[] ValidHashSizes = { 32, 64 };

    private readonly MemoryStream _inputBuffer = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CityHash{T}" /> class with the specified hash output
    /// size.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be one of 32 or 64.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported values.
    /// </exception>
    protected CityHash(int hashSize)
        : base(ValidateHashSize(hashSize) / 8)
    {
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length > 0)
            this._inputBuffer.Write(source);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this._inputBuffer.SetLength(0);
        this._inputBuffer.Position = 0;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        // CityHash is a one-shot algorithm; finalisation re-runs over the accumulated buffer so that
        // GetCurrentHash remains non-destructive and may be invoked multiple times.
        byte[] data = this._inputBuffer.ToArray();
        byte[] digest = this.ComputeHashCore(data);
        digest.AsSpan(0, this.HashLengthInBytes).CopyTo(destination);
    }

    /// <summary>
    /// Performs the full hash computation over the complete accumulated input in a single pass.
    /// </summary>
    /// <param name="source">The complete input bytes to hash.</param>
    /// <returns>A byte array containing the final hash output.</returns>
    protected abstract byte[] ComputeHashCore(ReadOnlySpan<byte> source);

    /// <summary>
    /// Applies a final avalanche mixing step to a 32-bit value, improving bit diffusion.
    /// </summary>
    /// <param name="h">The 32-bit value to mix.</param>
    /// <returns>The mixed 32-bit result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint Mix(uint h) => h ^ (h >> 16);

    /// <summary>
    /// Applies a single Murmur-style multiply-rotate-XOR step, combining two 32-bit values.
    /// </summary>
    /// <param name="a">The input value to multiply and fold.</param>
    /// <param name="h">The accumulator value to combine with the result.</param>
    /// <returns>The result of the Murmur mixing step.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint Mur(uint a, uint h) =>
        unchecked((a * C1) ^ h.RotateBitsRightUnchecked(17));

    /// <summary>
    /// Performs a cyclic three-way permutation of the given values, assigning <c>a ← c</c>, <c>c ← b</c>,
    /// <c>b ← a</c>.
    /// </summary>
    /// <param name="a">The first value, receives the original value of <paramref name="c" />.</param>
    /// <param name="b">The second value, receives the original value of <paramref name="a" />.</param>
    /// <param name="c">The third value, receives the original value of <paramref name="b" />.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void Permute3(ref uint a, ref uint b, ref uint c)
    {
        uint t = a;
        a = c;
        c = b;
        b = t;
    }

    private static int ValidateHashSize(int hashSize)
    {
        if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hashSize),
                hashSize,
                $"Invalid hash size: {hashSize}. Valid sizes are: {string.Join(", ", ValidHashSizes)}.");
        }

        return hashSize;
    }
}
