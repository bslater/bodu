using Bodu.Extensions;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Represents the base implementation for Threefish block ciphers. This class provides the core infrastructure for Threefish encryption
    /// and decryption operations, including key and tweak scheduling, resource disposal, and core mixing functions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This base class supports 64-bit word operations and defines the common structure required for <c>Threefish-256</c>,
    /// <c>Threefish-512</c>, and <c>Threefish-1024</c> variants. The cipher relies on XOR, addition, and bitwise rotation, with
    /// configurable rounds and rotation constants.
    /// </para>
    /// </remarks>
    internal abstract partial class ThreefishBlockCipher
        : IBlockCipher
    {
        /// <summary>
        /// Gets the expanded key schedule including parity and repeated key words.
        /// </summary>
        // KeySchedule: [K0, K1, K2, K3, K4=parity, K0, K1, K2, K3]
        protected readonly ulong[] KeySchedule;

        /// <summary>
        /// Gets the expanded tweak schedule including derived and repeated tweak words.
        /// </summary>
        // TweakSchedule: [T0, T1, T2=T0^T1, T0, T1]
        protected readonly ulong[] TweakSchedule;

        /// <summary>
        /// Indicates whether the instance has been disposed.
        /// </summary>
        protected bool disposed = false;

        private const ulong KeyParityValue = 0x1BD11BDAA9FC1A22;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreefishBlockCipher" /> class using the specified key and tweak.
        /// </summary>
        /// <param name="key">A 32-byte (256-bit) encryption key.</param>
        /// <param name="tweak">A 16-byte (128-bit) tweak value.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="key" /> or <paramref name="tweak" /> has an invalid length.</exception>
        protected ThreefishBlockCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        {
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(key, BlockSize);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(tweak, 16);

            // Key schedule initialization: 4 words + parity + duplicated key
            KeySchedule = new ulong[BlockWords * 2 + 1];
            MemoryMarshal.Cast<byte, ulong>(key).CopyTo(KeySchedule);
            ulong parity = KeyParityValue;
            for (int i = 0; i < BlockWords; i++)
            {
                ulong word = KeySchedule[i];
                parity ^= word;
                KeySchedule[BlockWords + 1 + i] = word; // repeat key word
            }

            KeySchedule[BlockWords] = parity;

            // Tweak schedule initialization: T0, T1, T2 = T0^T1, then duplicate T0/T1
            TweakSchedule = new ulong[5];
            MemoryMarshal.Cast<byte, ulong>(tweak).CopyTo(TweakSchedule);
            TweakSchedule[2] = TweakSchedule[0] ^ TweakSchedule[1];
            TweakSchedule[3] = TweakSchedule[0];
            TweakSchedule[4] = TweakSchedule[1];
        }

        /// <inheritdoc />
        ~ThreefishBlockCipher()
        {
            Dispose(false);
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Encrypts a full input block and writes the ciphertext to the output span.
        /// </summary>
        /// <param name="input">The plaintext input block.</param>
        /// <param name="output">The output span to receive the encrypted data.</param>
        public abstract void Encrypt(ReadOnlySpan<byte> input, Span<byte> output);

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
        /// <param name="disposing">Whether the method was called from <see cref="Dispose" />.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed) return;

            if (disposing)
            {
                CryptoHelpers.Clear(KeySchedule);  // Securely zeros content
                CryptoHelpers.Clear(TweakSchedule);
            }

            this.disposed = true;
        }

        /// <summary>
        /// Throws an exception if the instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has already been disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(this.disposed, this);
#else
        if (disposed)
            throw new ObjectDisposedException(nameof(T));
#endif
        }
    }
}