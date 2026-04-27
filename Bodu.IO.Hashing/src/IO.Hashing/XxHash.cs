// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XxHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System.IO;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

/// <summary>
/// Base class for the <c>xxHash</c> family of non-cryptographic hash algorithms by Yann Collet. See the
/// <a href="https://github.com/Cyan4973/xxHash">xxHash reference repository</a> for the specification.
/// </summary>
/// <typeparam name="T">
/// The concrete xxHash variant derived from this class. Must expose a public parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// xxHash algorithms are one-shot by design. To satisfy the incremental input contract of
/// <see cref="NonCryptographicHashAlgorithm" />, this base class accumulates all bytes delivered through
/// <see cref="Append(ReadOnlySpan{byte})" /> into an internal buffer and invokes the derived variant's
/// <see cref="ComputeHashCore(ReadOnlySpan{byte})" /> from <see cref="GetCurrentHashCore(Span{byte})" /> once
/// all input is available.
/// </para>
/// <para>
/// Supported output sizes across derived variants are 32, 64, and 128 bits. xxHash32 and xxHash64 accept a
/// seed to vary the output for identical input; xxHash3 additionally supports a custom 192-byte secret.
/// </para>
/// <note type="important">
/// xxHash is <b>not</b> cryptographically secure. It must <b>not</b> be used for password hashing, digital
/// signatures, or any application that requires collision resistance under adversarial conditions.
/// </note>
/// </remarks>
public abstract class XxHash<T>
    : NonCryptographicHashAlgorithm
    where T : XxHash<T>, new()
{
    private readonly MemoryStream _inputBuffer = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="XxHash{T}" /> class with the specified hash output size.
    /// </summary>
    /// <param name="hashLengthInBytes">The hash output length in bytes.</param>
    protected XxHash(int hashLengthInBytes)
        : base(hashLengthInBytes)
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
    /// Applies a single xxHash64 accumulator mixing round.
    /// </summary>
    /// <param name="acc">The current accumulator value.</param>
    /// <param name="lane">The 64-bit lane word to fold in.</param>
    /// <returns>The updated accumulator value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong Round64(ulong acc, ulong lane)
    {
        acc = unchecked(acc + lane * Prime64_2);
        acc = RotateLeft64(acc, 31);
        return unchecked(acc * Prime64_1);
    }

    /// <summary>
    /// Merges a secondary accumulator into the primary during finalisation of an xxHash64 computation.
    /// </summary>
    /// <param name="acc">The primary accumulator.</param>
    /// <param name="accN">The secondary accumulator to fold in.</param>
    /// <returns>The merged accumulator value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong MergeAccumulator64(ulong acc, ulong accN)
    {
        acc ^= Round64(0, accN);
        acc = unchecked(acc * Prime64_1 + Prime64_4);
        return acc;
    }

    /// <summary>
    /// Rotates a 32-bit value left by the specified bit count.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="bits">The rotation distance in bits.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint RotateLeft32(uint value, int bits) =>
        (value << bits) | (value >> (32 - bits));

    /// <summary>
    /// Rotates a 64-bit value left by the specified bit count.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="bits">The rotation distance in bits.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong RotateLeft64(ulong value, int bits) =>
        (value << bits) | (value >> (64 - bits));

    /// <summary>The first xxHash32 prime multiplier.</summary>
    protected const uint Prime32_1 = 0x9E3779B1u;

    /// <summary>The second xxHash32 prime multiplier.</summary>
    protected const uint Prime32_2 = 0x85EBCA77u;

    /// <summary>The third xxHash32 prime multiplier.</summary>
    protected const uint Prime32_3 = 0xC2B2AE3Du;

    /// <summary>The fourth xxHash32 prime multiplier.</summary>
    protected const uint Prime32_4 = 0x27D4EB2Fu;

    /// <summary>The fifth xxHash32 prime multiplier.</summary>
    protected const uint Prime32_5 = 0x165667B1u;

    /// <summary>The first xxHash64 prime multiplier.</summary>
    protected const ulong Prime64_1 = 0x9E3779B185EBCA87uL;

    /// <summary>The second xxHash64 prime multiplier.</summary>
    protected const ulong Prime64_2 = 0xC2B2AE3D27D4EB4FuL;

    /// <summary>The third xxHash64 prime multiplier.</summary>
    protected const ulong Prime64_3 = 0x165667B19E3779F9uL;

    /// <summary>The fourth xxHash64 prime multiplier.</summary>
    protected const ulong Prime64_4 = 0x85EBCA77C2B2AE63uL;

    /// <summary>The fifth xxHash64 prime multiplier.</summary>
    protected const ulong Prime64_5 = 0x27D4EB2F165667C5uL;
}
