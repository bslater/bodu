// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for managed Threefish block cipher engines, providing shared key and tweak
/// scheduling, resource disposal, and the core MIX/UNMIX primitives used by the <c>Threefish-256</c>,
/// <c>Threefish-512</c>, and <c>Threefish-1024</c> variants.
/// </summary>
/// <remarks>
/// <para>
/// Derived classes — <see cref="Threefish256Cipher" />, <see cref="Threefish512Cipher" />, and
/// <see cref="Threefish1024Cipher" /> — supply the block size, word count, rotation schedule, and round count for a
/// specific Threefish variant, along with their own <see cref="Encrypt" /> and <see cref="Decrypt" /> implementations.
/// </para>
/// <para>
/// External callers cannot derive new variants: the constructor and protected members are scoped
/// <c>private protected</c>, limiting derivation to the three variants that ship with this library. Use
/// <see cref="Threefish256Cipher" />, <see cref="Threefish512Cipher" />, or <see cref="Threefish1024Cipher" /> for the
/// raw block primitive, or one of the higher-level <see cref="Threefish256" />, <see cref="Threefish512" />, or
/// <see cref="Threefish1024" /> wrappers for the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs.
/// SymmetricAlgorithm</seealso>
public abstract partial class ThreefishBlockCipher
    : IBlockCipher
{
    /// <summary>
    /// The expanded key schedule, containing the original key words, the parity word, and the repeated key words used
    /// during subkey injection.
    /// </summary>
    // KeySchedule: [K0, K1, K2, K3, K4=parity, K0, K1, K2, K3]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Scoped private protected so only the three in-assembly Threefish variant classes can access the key schedule directly, avoiding property dispatch on the hot encrypt/decrypt path.")]
    private protected readonly ulong[] _keySchedule;

    /// <summary>
    /// The expanded tweak schedule, containing the two tweak words, their XOR, and the repeated tweak words used during
    /// subkey injection.
    /// </summary>
    // TweakSchedule: [T0, T1, T2=T0^T1, T0, T1]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Scoped private protected so only the three in-assembly Threefish variant classes can access the tweak schedule directly, avoiding property dispatch on the hot encrypt/decrypt path.")]
    private protected readonly ulong[] _tweakSchedule;

    /// <summary>
    /// Indicates whether the instance has been disposed.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Scoped private protected so only in-assembly Threefish variant classes can read the disposal flag directly in ThrowIfDisposed without virtual dispatch.")]
    private protected bool _disposed = false;

    private const ulong KeyParityValue = 0x1BD11BDAA9FC1A22;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreefishBlockCipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Its byte length must equal <see cref="BlockSize" /> / 8 (32, 64, or 128 bytes).
    /// </param>
    /// <param name="tweak">The 16-byte (128-bit) tweak value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> or <paramref name="tweak" /> has an invalid length.
    /// </exception>
    private protected ThreefishBlockCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(key, this.BlockSize / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(tweak, 16);

        // Key schedule initialization: 4 words + parity + duplicated key
        this._keySchedule = new ulong[(this.BlockWords * 2) + 1];
        MemoryMarshal.Cast<byte, ulong>(key).CopyTo(this._keySchedule);
        var parity = KeyParityValue;
        for (var i = 0; i < this.BlockWords; i++)
        {
            var word = this._keySchedule[i];
            parity ^= word;
            this._keySchedule[this.BlockWords + 1 + i] = word; // repeat key word
        }

        this._keySchedule[this.BlockWords] = parity;

        // Tweak schedule initialization: T0, T1, T2 = T0^T1, then duplicate T0/T1
        this._tweakSchedule = new ulong[5];
        MemoryMarshal.Cast<byte, ulong>(tweak).CopyTo(this._tweakSchedule);
        this._tweakSchedule[2] = this._tweakSchedule[0] ^ this._tweakSchedule[1];
        this._tweakSchedule[3] = this._tweakSchedule[0];
        this._tweakSchedule[4] = this._tweakSchedule[1];
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ThreefishBlockCipher" /> class.
    /// </summary>
    ~ThreefishBlockCipher()
    {
        this.Dispose(false);
    }

    /// <inheritdoc />
    public abstract int BlockSize { get; }

    /// <summary>
    /// Gets the number of 64-bit words in a single block.
    /// </summary>
    protected abstract int BlockWords { get; }

    /// <summary>
    /// Gets the rotation constants used for MIX/UNMIX operations in this cipher variant.
    /// </summary>
    protected abstract int[] RotationSchedule { get; }

    /// <summary>
    /// Gets the total number of cipher rounds.
    /// </summary>
    protected abstract int Rounds { get; }

    /// <summary>
    /// Decrypts a full ciphertext block and writes the plaintext to the output span.
    /// </summary>
    /// <param name="input">The ciphertext input block.</param>
    /// <param name="output">The output span to receive the decrypted data.</param>
    public abstract void Decrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <inheritdoc />
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Encrypts a full input block and writes the ciphertext to the output span.
    /// </summary>
    /// <param name="input">The plaintext input block.</param>
    /// <param name="output">The output span to receive the encrypted data.</param>
    public abstract void Encrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <summary>
    /// Replaces the key and tweak schedules in place, allowing the cipher instance to be reused across successive
    /// Threefish block calls without allocating a new instance or re-running the constructor.
    /// </summary>
    /// <param name="key">The replacement key. Its byte length must equal <see cref="BlockSize" /> / 8.</param>
    /// <param name="tweak">The replacement 16-byte (128-bit) tweak value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> or <paramref name="tweak" /> has an invalid length.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This hook is intended for the Skein hash construction, whose UBI mode of operation invokes Threefish once per
    /// block with a freshly derived key (the current chaining value) and a recomputed tweak (encoding block type,
    /// position, and the first/final flags). Rebuilding the schedules in place avoids per-block allocation of the
    /// <see cref="_keySchedule" /> and <see cref="_tweakSchedule" /> arrays.
    /// </para>
    /// <para>
    /// The implementation mirrors the constructor's schedule setup exactly: key parity is recomputed as
    /// <c>C240 ^ K0 ^ K1 ^ … ^ K(n-1)</c>, and the tweak's derived word is recomputed as <c>T0 ^ T1</c>. Exposed with
    /// <see langword="internal" /> visibility so that only same-assembly consumers may call it.
    /// </para>
    /// </remarks>
    internal void Rekey(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(key, this.BlockSize / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(tweak, 16);
        this.ThrowIfDisposed();

        // Repopulate the key schedule: [K0..K(n-1), parity, K0..K(n-1)].
        MemoryMarshal.Cast<byte, ulong>(key).CopyTo(this._keySchedule);
        var parity = KeyParityValue;
        for (var i = 0; i < this.BlockWords; i++)
        {
            var word = this._keySchedule[i];
            parity ^= word;
            this._keySchedule[this.BlockWords + 1 + i] = word;
        }

        this._keySchedule[this.BlockWords] = parity;

        // Repopulate the tweak schedule: [T0, T1, T0^T1, T0, T1].
        MemoryMarshal.Cast<byte, ulong>(tweak).CopyTo(this._tweakSchedule);
        this._tweakSchedule[2] = this._tweakSchedule[0] ^ this._tweakSchedule[1];
        this._tweakSchedule[3] = this._tweakSchedule[0];
        this._tweakSchedule[4] = this._tweakSchedule[1];
    }

    /// <summary>
    /// Performs a Threefish mixing operation by rotating and XORing the input values.
    /// </summary>
    /// <param name="a">The first value (accumulator), modified in-place.</param>
    /// <param name="b">The second value, rotated and XORed with <paramref name="a" />.</param>
    /// <param name="rotation">The rotation amount (in bits).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void Mix(ref ulong a, ref ulong b, int rotation)
    {
        a += b;
        b = BitOperations.RotateLeft(b, rotation) ^ a;
    }

    /// <summary>
    /// Reverses the Threefish mixing operation performed by <see cref="Mix" />.
    /// </summary>
    /// <param name="a">The accumulator used in the forward pass.</param>
    /// <param name="b">The rotated/XORed value to unmix.</param>
    /// <param name="rotation">The rotation amount (in bits) used during encryption.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void Unmix(ref ulong a, ref ulong b, int rotation)
    {
        b ^= a;
        b = BitOperations.RotateRight(b, rotation);
        a -= b;
    }

    /// <summary>
    /// Releases all internal buffers and sensitive material. Securely clears the key and tweak schedules.
    /// </summary>
    /// <param name="disposing">Whether the method was called from <see cref="Dispose()" />.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed) return;

        // Key and tweak schedules are owned exclusively by this instance and are zeroed in both
        // the deterministic Dispose() path and the finalizer path so that key material is never
        // retained if the caller omits an explicit Dispose call.
        CryptoHelpers.Clear(this._keySchedule);
        CryptoHelpers.Clear(this._tweakSchedule);

        this._disposed = true;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif

}
