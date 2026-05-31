// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.256.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the <c>Threefish-256</c> block cipher, which operates on 256-bit (32-byte) blocks using a 256-bit key and
/// a 128-bit tweak.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is a tweakable block cipher optimized for 64-bit platforms and forms the core primitive of the Skein hash
/// function. The <c>Threefish-256</c> variant operates on four 64-bit words over 72 rounds using modular addition,
/// bitwise rotation, and XOR.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Threefish256" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract. Use <see cref="Threefish256Cipher" />
/// directly only when composing the raw block primitive with an <see cref="IBlockCipherModeTransform" /> (for example
/// via <see cref="BlockCipherModeFactory" />) or with an <see cref="IPaddingStrategy" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Direct single-block use — most callers should prefer the Threefish256 SymmetricAlgorithm.
/// byte[] key   = new byte[32];   // 256-bit key
/// byte[] tweak = new byte[16];   // 128-bit tweak
/// RandomNumberGenerator.Fill(key);
/// RandomNumberGenerator.Fill(tweak);
///
/// using var cipher = new Threefish256Cipher(key, tweak);
///
/// byte[] plaintext  = new byte[32];   // one 256-bit block
/// byte[] ciphertext = new byte[32];
/// cipher.Encrypt(plaintext, ciphertext);
///
/// byte[] roundtrip = new byte[32];
/// cipher.Decrypt(ciphertext, roundtrip);
/// roundtrip equals plaintext
///]]>
/// </example>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs.
/// SymmetricAlgorithm</seealso> <seealso cref="Threefish256"/>
public sealed partial class Threefish256Cipher
    : ThreefishBlockCipher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish256Cipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behavior.</param>
    public Threefish256Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <summary>
    /// Length of the Threefish-256 key is 256 bits (32 bytes).
    /// </summary>
    public const int KeySize = 256;

    // Spec-defined rotation constants for Threefish-256. Declared as named const ints so the JIT can
    // fold each Mix/Unmix call to a ROL/ROR with an immediate count, and so all 16 values live in one
    // place rather than being duplicated between Encrypt, Decrypt, and the public RotationSchedule.
    private const int R0 = 14, R1 = 16, R2 = 52, R3 = 57;
    private const int R4 = 23, R5 = 40, R6 = 5, R7 = 37;
    private const int R8 = 25, R9 = 33, R10 = 46, R11 = 12;
    private const int R12 = 58, R13 = 22, R14 = 32, R15 = 32;

    private static readonly int[] s_rotationSchedule =
    [
        R0, R1, R2, R3, R4, R5, R6, R7,
        R8, R9, R10, R11, R12, R13, R14, R15,
    ];

    /// <inheritdoc />
    /// <value>Length of the Threefish-256 block is 256 bits (32 bytes).</value>
    public override int BlockSize => 256;

    /// <inheritdoc />
    protected override int BlockWords => 4;

    /// <inheritdoc />
    protected override int[] RotationSchedule => s_rotationSchedule;

    /// <inheritdoc />
    protected override int Rounds => 72;

    /// <summary>
    /// Decrypts a single 32-byte ciphertext block using the <c>Threefish-256</c> cipher and writes the result to the
    /// specified output buffer.
    /// </summary>
    /// <param name="input">The 32-byte ciphertext block to decrypt.</param>
    /// <param name="output">The 32-byte buffer to receive the decrypted plaintext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> or <paramref name="output" /> is not 32 bytes.
    /// </exception>
    /// <remarks>
    /// Dispatches to an AVX-512 vectorised implementation when supported by the host, falling back to a scalar
    /// register-resident implementation otherwise. <see cref="Avx512F.VL.IsSupported" /> is a JIT intrinsic that folds
    /// to a compile-time constant, so the branch carries no runtime cost.
    /// </remarks>
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != 32 || output.Length != 32)
        {
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_BlockLength, 32));
        }

        if (Avx512F.VL.IsSupported)
        {
            DecryptAvx512(input, output);
            return;
        }

        DecryptScalar(input, output);
    }

    /// <summary>
    /// Decrypts a single 32-byte ciphertext block using the scalar register-resident Threefish-256 implementation.
    /// </summary>
    /// <param name="input">
    /// The 32-byte ciphertext block to decrypt. Caller is responsible for length validation.
    /// </param>
    /// <param name="output">The 32-byte buffer to receive the decrypted plaintext block.</param>
    /// <remarks>
    /// Invoked by <see cref="Decrypt" /> on hosts without AVX-512 + VL support. Operates on four 64-bit words held in
    /// stack-resident locals, with the key/tweak schedule accessed via interior refs so subkey injections skip
    /// per-element bounds checks.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void DecryptScalar(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // Load the ciphertext block as four 64-bit little-endian words into registers, skipping the
        // intermediate stackalloc + MemoryMarshal.Cast + CopyTo trip through the stack the prior
        // implementation used.
        ref var inputRef = ref MemoryMarshal.GetReference(input);
        var b0 = LoadWordLittleEndian(ref inputRef, 0);
        var b1 = LoadWordLittleEndian(ref inputRef, 8);
        var b2 = LoadWordLittleEndian(ref inputRef, 16);
        var b3 = LoadWordLittleEndian(ref inputRef, 24);

        // Capture interior refs to the key and tweak schedule arrays so each subkey-injection access
        // skips the per-element bounds check that ulong[] indexing would otherwise emit.
        ref var keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref var tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        for (var d = (72 / 4) - 1; d >= 1; d -= 2)
        {
            var dm5 = d % 5;
            var dm3 = d % 3;

            // Reverse post-round subkey injection
            b0 -= Unsafe.Add(ref keyRef, dm5 + 1);
            b1 -= Unsafe.Add(ref keyRef, dm5 + 2) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b2 -= Unsafe.Add(ref keyRef, dm5 + 3) + Unsafe.Add(ref tweakRef, dm3 + 2);
            b3 -= Unsafe.Add(ref keyRef, dm5 + 4) + (uint)(d + 1);

            // Reverse second 4 rounds
            Unmix(ref b2, ref b1, R15);
            Unmix(ref b0, ref b3, R14);
            Unmix(ref b2, ref b3, R13);
            Unmix(ref b0, ref b1, R12);
            Unmix(ref b2, ref b1, R11);
            Unmix(ref b0, ref b3, R10);
            Unmix(ref b2, ref b3, R9);
            Unmix(ref b0, ref b1, R8);

            // Reverse mid-round subkey injection
            b0 -= Unsafe.Add(ref keyRef, dm5);
            b1 -= Unsafe.Add(ref keyRef, dm5 + 1) + Unsafe.Add(ref tweakRef, dm3);
            b2 -= Unsafe.Add(ref keyRef, dm5 + 2) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b3 -= Unsafe.Add(ref keyRef, dm5 + 3) + (uint)d;

            // Reverse first 4 rounds
            Unmix(ref b2, ref b1, R7);
            Unmix(ref b0, ref b3, R6);
            Unmix(ref b2, ref b3, R5);
            Unmix(ref b0, ref b1, R4);
            Unmix(ref b2, ref b1, R3);
            Unmix(ref b0, ref b3, R2);
            Unmix(ref b2, ref b3, R1);
            Unmix(ref b0, ref b1, R0);
        }

        // Final subkey removal (round 0)
        b0 -= Unsafe.Add(ref keyRef, 0);
        b1 -= Unsafe.Add(ref keyRef, 1) + Unsafe.Add(ref tweakRef, 0);
        b2 -= Unsafe.Add(ref keyRef, 2) + Unsafe.Add(ref tweakRef, 1);
        b3 -= Unsafe.Add(ref keyRef, 3);

        // Commit the four plaintext words to the output buffer in little-endian byte order.
        ref var outputRef = ref MemoryMarshal.GetReference(output);
        StoreWordLittleEndian(ref outputRef, 0, b0);
        StoreWordLittleEndian(ref outputRef, 8, b1);
        StoreWordLittleEndian(ref outputRef, 16, b2);
        StoreWordLittleEndian(ref outputRef, 24, b3);
    }

    /// <summary>
    /// Encrypts a single 32-byte plaintext block using the <c>Threefish-256</c> cipher and writes the result to the
    /// specified output buffer.
    /// </summary>
    /// <param name="input">The 32-byte plaintext block to encrypt.</param>
    /// <param name="output">The 32-byte buffer to receive the encrypted ciphertext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> or <paramref name="output" /> is not 32 bytes.
    /// </exception>
    /// <remarks>
    /// Dispatches to an AVX-512 vectorised implementation when supported by the host, falling back to a scalar
    /// register-resident implementation otherwise. <see cref="Avx512F.VL.IsSupported" /> is a JIT intrinsic that folds
    /// to a compile-time constant, so the branch carries no runtime cost.
    /// </remarks>
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != 32 || output.Length != 32)
        {
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_BlockLength, 32));
        }

        if (Avx512F.VL.IsSupported)
        {
            EncryptAvx512(input, output);
            return;
        }

        EncryptScalar(input, output);
    }

    /// <summary>
    /// Encrypts a single 32-byte plaintext block using the scalar register-resident Threefish-256 implementation.
    /// </summary>
    /// <param name="input">The 32-byte plaintext block to encrypt. Caller is responsible for length validation.</param>
    /// <param name="output">The 32-byte buffer to receive the encrypted ciphertext block.</param>
    /// <remarks>
    /// Invoked by <see cref="Encrypt" /> on hosts without AVX-512 + VL support. See <see cref="DecryptScalar" /> for
    /// the local-resident state strategy this method mirrors.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void EncryptScalar(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // Load the plaintext block as four 64-bit little-endian words into registers.
        ref var inputRef = ref MemoryMarshal.GetReference(input);
        var b0 = LoadWordLittleEndian(ref inputRef, 0);
        var b1 = LoadWordLittleEndian(ref inputRef, 8);
        var b2 = LoadWordLittleEndian(ref inputRef, 16);
        var b3 = LoadWordLittleEndian(ref inputRef, 24);

        ref var keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref var tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        // Initial key injection (round 0)
        b0 += Unsafe.Add(ref keyRef, 0);
        b1 += Unsafe.Add(ref keyRef, 1) + Unsafe.Add(ref tweakRef, 0);
        b2 += Unsafe.Add(ref keyRef, 2) + Unsafe.Add(ref tweakRef, 1);
        b3 += Unsafe.Add(ref keyRef, 3);

        for (var d = 1; d < 72 / 4; d += 2)
        {
            var dm5 = d % 5;
            var dm3 = d % 3;

            // First 4 MIX rounds
            Mix(ref b0, ref b1, R0);
            Mix(ref b2, ref b3, R1);
            Mix(ref b0, ref b3, R2);
            Mix(ref b2, ref b1, R3);
            Mix(ref b0, ref b1, R4);
            Mix(ref b2, ref b3, R5);
            Mix(ref b0, ref b3, R6);
            Mix(ref b2, ref b1, R7);

            // Mid-round subkey injection
            b0 += Unsafe.Add(ref keyRef, dm5);
            b1 += Unsafe.Add(ref keyRef, dm5 + 1) + Unsafe.Add(ref tweakRef, dm3);
            b2 += Unsafe.Add(ref keyRef, dm5 + 2) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b3 += Unsafe.Add(ref keyRef, dm5 + 3) + (ulong)d;

            // Second 4 MIX rounds
            Mix(ref b0, ref b1, R8);
            Mix(ref b2, ref b3, R9);
            Mix(ref b0, ref b3, R10);
            Mix(ref b2, ref b1, R11);
            Mix(ref b0, ref b1, R12);
            Mix(ref b2, ref b3, R13);
            Mix(ref b0, ref b3, R14);
            Mix(ref b2, ref b1, R15);

            // Post-round subkey injection
            b0 += Unsafe.Add(ref keyRef, dm5 + 1);
            b1 += Unsafe.Add(ref keyRef, dm5 + 2) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b2 += Unsafe.Add(ref keyRef, dm5 + 3) + Unsafe.Add(ref tweakRef, dm3 + 2);
            b3 += Unsafe.Add(ref keyRef, dm5 + 4) + (ulong)d + 1;
        }

        ref var outputRef = ref MemoryMarshal.GetReference(output);
        StoreWordLittleEndian(ref outputRef, 0, b0);
        StoreWordLittleEndian(ref outputRef, 8, b1);
        StoreWordLittleEndian(ref outputRef, 16, b2);
        StoreWordLittleEndian(ref outputRef, 24, b3);
    }
}
