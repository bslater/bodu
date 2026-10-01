// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipherBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for managed Serpent block cipher engines, providing the shared S-boxes, bitsliced
/// linear transform, prekey recurrence, and resource-disposal plumbing used by the standard <c>Serpent-128</c> variant
/// and the non-standard wide-block tweakable variants (<c>Serpent-256</c>, <c>Serpent-512</c>, <c>Serpent-1024</c>).
/// </summary>
/// <remarks>
/// <para>
/// Derived classes supply the state width (in 32-bit words), the round count, and their own <see cref="Encrypt" /> and
/// <see cref="Decrypt" /> implementations, whose rounds run in <see cref="SerpentCore" />. The base class exposes the
/// Serpent S-boxes <c>S0..S7</c> the key schedules apply (Osvik's Boolean circuits, from <see cref="SerpentCore" />)
/// and the round-key expansion helper <see cref="ExpandPrekeys" /> driven by the golden-ratio constant
/// <c>phi = 0x9E3779B9</c>.
/// </para>
/// <para>
/// Serpent operates on four 32-bit words in bitsliced form. Each bit position across those four words represents one
/// 4-bit S-box input. The helpers in this base class keep that representation explicit so the concrete ciphers can
/// share the standard key-schedule operations.
/// </para>
/// <para>
/// External callers cannot derive new variants: the constructor and protected members are scoped
/// <c>private protected</c>. Use <see cref="Serpent128Cipher" /> or one of the wide-block
/// <see cref="Serpent256Cipher" /> / <see cref="Serpent512Cipher" /> / <see cref="Serpent1024Cipher" /> types directly,
/// or compose with <see cref="IBlockCipherModeTransform" /> via <see cref="BlockCipherModeFactory" />.
/// </para>
/// </remarks>
public abstract partial class SerpentBlockCipherBase
    : IBlockCipher
{
    /// <summary>The golden-ratio fractional constant used in the Serpent prekey recurrence.</summary>
    /// <remarks>
    /// Serpent defines this value as <c>floor((sqrt(5) - 1) * 2^31)</c>, encoded as <c>0x9E3779B9</c>. It is mixed into
    /// every generated prekey word together with the word index before the 11-bit rotation.
    /// </remarks>
    private protected const uint Phi = 0x9E3779B9u;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Scoped private protected so only in-assembly Serpent variant classes can read the disposal flag directly in ThrowIfDisposed without virtual dispatch.")]
    private protected bool _disposed;

    /// <summary>
    /// Finalizes an instance of the <see cref="SerpentBlockCipherBase" /> class.
    /// </summary>
    /// <remarks>
    /// The finalizer delegates to the standard dispose pattern so derived cipher implementations can clear round-key
    /// material even if callers fail to dispose the instance explicitly.
    /// </remarks>
    ~SerpentBlockCipherBase()
    {
        Dispose(false);
    }

    /// <inheritdoc />
    public abstract int BlockSize { get; }

    /// <inheritdoc />
    public abstract void Decrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <inheritdoc />
    public abstract void Encrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases all internal buffers and sensitive material.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> when invoked from <see cref="Dispose()" />; <see langword="false" /> when invoked from
    /// the finalizer.
    /// </param>
    /// <remarks>
    /// The base implementation records the disposed state. Derived Serpent implementations should override this method
    /// to clear expanded round keys, tweak material, and other sensitive buffers before calling
    /// <c>base.Dispose(disposing)</c>.
    /// </remarks>
    protected virtual void Dispose(bool disposing) => _disposed = true;

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Applies the Serpent S-box identified by <paramref name="sBoxIndex" /> to the four 32-bit words in bitsliced
    /// form.
    /// </summary>
    /// <param name="sBoxIndex">The S-box index in the range <c>0..7</c>.</param>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    /// <remarks>
    /// Each of the 32 bit columns across the four words is one 4-bit S-box input: bit 0 from <paramref name="x0" />,
    /// bit 1 from <paramref name="x1" />, bit 2 from <paramref name="x2" />, and bit 3 from <paramref name="x3" />. The
    /// substitution runs as Osvik's Boolean circuit for the S-box (see <see cref="SerpentCore" />), with no table
    /// lookup and no data-dependent branch.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void ApplySBox(int sBoxIndex, ref uint x0, ref uint x1, ref uint x2, ref uint x3) =>
        SerpentCore.SBox(sBoxIndex, ref x0, ref x1, ref x2, ref x3);

    /// <summary>
    /// Runs the Serpent prekey recurrence to populate the prekey buffer.
    /// </summary>
    /// <param name="seed">
    /// The first <paramref name="window" /> entries of <paramref name="prekeys" />, already seeded with the key
    /// material.
    /// </param>
    /// <param name="prekeys">
    /// The buffer receiving the expanded prekey sequence. Must start with <paramref name="seed" />.
    /// </param>
    /// <param name="window">
    /// The recurrence window in words. Must be at least 8 to satisfy the Serpent recurrence indices
    /// <c>i-8, i-5, i-3, i-1</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// The recurrence is the Serpent key schedule expansion
    /// <c>w[i] = ROL(w[i-window] ^ w[i-5] ^ w[i-3] ^ w[i-1] ^ phi ^ i, 11)</c> for
    /// <c>i = 0..prekeys.Length - window - 1</c>. The caller initializes <c>prekeys[0..window-1]</c> with the seed and
    /// this helper computes the remaining entries in place.
    /// </para>
    /// <para>
    /// Standard Serpent-128 uses <c>window = 8</c>. The wide-block variants use larger windows matched to their state
    /// width, but keep the same recurrence shape so the indices remain relative to the active state window.
    /// </para>
    /// </remarks>
    private protected static void ExpandPrekeys(ReadOnlySpan<uint> seed, Span<uint> prekeys, int window)
    {
        // Seed the recurrence with the padded key words prepared by the concrete cipher implementation.
        seed.CopyTo(prekeys);

        for (int i = 0; i + window < prekeys.Length; i++)
        {
            // Compute the next prekey word from the Serpent recurrence, then rotate left by 11 bits.
            // The use of i + window maps the specification's w[i] output onto the caller's seeded buffer layout.
            uint value = prekeys[i] ^ prekeys[i + window - 5] ^ prekeys[i + window - 3] ^ prekeys[i + window - 1] ^ Phi ^ (uint)i;
            prekeys[i + window] = value.RotateBitsLeftUnchecked(11);
        }
    }

    /// <summary>
    /// Returns the Serpent S-box index used by round-key <paramref name="roundIndex" /> in the key schedule.
    /// </summary>
    /// <param name="roundIndex">The round-key index, in the range <c>0..R</c>.</param>
    /// <returns>The S-box index, in the range <c>0..7</c>.</returns>
    /// <remarks>
    /// Serpent's key schedule applies the S-boxes to successive prekey words in descending cyclic order:
    /// <c>K_0 → S3, K_1 → S2, K_2 → S1, K_3 → S0, K_4 → S7, …</c>, following the standard formula
    /// <c>(3 − roundIndex) mod 8</c>. The same ordering is reused for the wide-block variants so that <c>K_0</c> always
    /// uses <c>S3</c>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static int KeyScheduleSBoxIndex(int roundIndex)
    {
        // Bitwise AND with 7 performs modulo 8 for the descending S-box schedule.
        int value = (3 - roundIndex) & 7;
        return value;
    }
}
