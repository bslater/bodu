// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the <c>Threefish-512</c> block cipher, which operates on 512-bit (64-byte) blocks using a 512-bit key and
/// a 128-bit tweak.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is a tweakable block cipher optimized for 64-bit platforms and forms the core primitive of the Skein hash
/// function. The <c>Threefish-512</c> variant operates on eight 64-bit words over 72 rounds using modular addition,
/// bitwise rotation, and XOR.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Threefish512" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract. Use <see cref="Threefish512Cipher" />
/// directly only when composing the raw block primitive with an <see cref="IBlockCipherModeTransform" /> (for example
/// via <see cref="BlockCipherModeFactory" />) or with an <see cref="IPaddingStrategy" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Direct single-block use — most callers should prefer the Threefish512 SymmetricAlgorithm.
/// byte[] key   = new byte[64];   // 512-bit key
/// byte[] tweak = new byte[16];   // 128-bit tweak
/// RandomNumberGenerator.Fill(key);
/// RandomNumberGenerator.Fill(tweak);
///
/// using var cipher = new Threefish512Cipher(key, tweak);
///
/// byte[] plaintext  = new byte[64];   // one 512-bit block
/// byte[] ciphertext = new byte[64];
/// cipher.Encrypt(plaintext, ciphertext);
///
/// byte[] roundtrip = new byte[64];
/// cipher.Decrypt(ciphertext, roundtrip);
/// // roundtrip equals plaintext
///]]>
/// </example>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs.
/// SymmetricAlgorithm</seealso> <seealso cref="Threefish512"/>
public sealed partial class Threefish512Cipher
    : ThreefishBlockCipher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish512Cipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 512-bit (64-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behavior.</param>
    public Threefish512Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <summary>
    /// Length of the Threefish-512 key is 512 bits (64 bytes).
    /// </summary>
    public const int KeySize = 512;

    /// <summary>
    /// The spec-defined rotation constants for the Threefish-512 Mix/Unmix operations (rotation amounts 0 through 3).
    /// </summary>
    /// <remarks>
    /// Declared as named <see langword="int" /> constants so the JIT can fold each Mix/Unmix call to a ROL/ROR with an
    /// immediate count, and so all 32 values live in one place rather than being duplicated between Encrypt, Decrypt,
    /// the AVX-512 rotation vectors, and the public rotation schedule.
    /// </remarks>
    private const int R0 = 46, R1 = 36, R2 = 19, R3 = 37;

    /// <summary>
    /// The spec-defined Threefish-512 rotation constants (rotation amounts 4 through 7).
    /// </summary>
    private const int R4 = 33, R5 = 27, R6 = 14, R7 = 42;

    /// <summary>
    /// The spec-defined Threefish-512 rotation constants (rotation amounts 8 through 11).
    /// </summary>
    private const int R8 = 17, R9 = 49, R10 = 36, R11 = 39;

    /// <summary>
    /// The spec-defined Threefish-512 rotation constants (rotation amounts 12 through 15).
    /// </summary>
    private const int R12 = 44, R13 = 9, R14 = 54, R15 = 56;

    /// <summary>
    /// The spec-defined Threefish-512 rotation constants (rotation amounts 16 through 19).
    /// </summary>
    private const int R16 = 39, R17 = 30, R18 = 34, R19 = 24;

    /// <summary>
    /// The spec-defined Threefish-512 rotation constants (rotation amounts 20 through 23).
    /// </summary>
    private const int R20 = 13, R21 = 50, R22 = 10, R23 = 17;

    /// <summary>
    /// The spec-defined Threefish-512 rotation constants (rotation amounts 24 through 27).
    /// </summary>
    private const int R24 = 25, R25 = 29, R26 = 39, R27 = 43;

    /// <summary>
    /// The spec-defined Threefish-512 rotation constants (rotation amounts 28 through 31).
    /// </summary>
    private const int R28 = 8, R29 = 35, R30 = 56, R31 = 22;

    /// <summary>
    /// The full 32-entry Threefish-512 rotation schedule exposed through <see cref="RotationSchedule" />.
    /// </summary>
    private static readonly int[] s_rotationSchedule =
    [
        R0, R1, R2, R3, R4, R5, R6, R7,
        R8, R9, R10, R11, R12, R13, R14, R15,
        R16, R17, R18, R19, R20, R21, R22, R23,
        R24, R25, R26, R27, R28, R29, R30, R31,
    ];

    /// <inheritdoc />
    /// <value>Length of the Threefish-512 block is 512 bits (64 bytes).</value>
    public override int BlockSize => 512;

    /// <inheritdoc />
    protected override int BlockWords => 8;

    /// <inheritdoc />
    protected override int[] RotationSchedule => s_rotationSchedule;

    /// <inheritdoc />
    protected override int Rounds => 72;

    /// <summary>
    /// Decrypts a single 64-byte ciphertext block using the <c>Threefish-512</c> cipher and writes the result to the
    /// specified output buffer.
    /// </summary>
    /// <param name="input">The 64-byte ciphertext block to decrypt.</param>
    /// <param name="output">The 64-byte buffer to receive the decrypted plaintext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> or <paramref name="output" /> is not 64 bytes.
    /// </exception>
    /// <remarks>
    /// Dispatches to an AVX-512F vectorised implementation when supported by the host, falling back to a scalar
    /// register-resident implementation otherwise. <see cref="Avx512F.IsSupported" /> is a JIT intrinsic that folds to
    /// a compile-time constant, so the branch carries no runtime cost.
    /// </remarks>
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != 64 || output.Length != 64)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_BlockLength, 64));
        }

        if (Avx512F.IsSupported)
        {
            DecryptAvx512(input, output);
            return;
        }

        DecryptScalar(input, output);
    }

    /// <summary>
    /// Decrypts a single 64-byte ciphertext block using the scalar register-resident Threefish-512 implementation.
    /// </summary>
    /// <param name="input">
    /// The 64-byte ciphertext block to decrypt. Caller is responsible for length validation.
    /// </param>
    /// <param name="output">The 64-byte buffer to receive the decrypted plaintext block.</param>
    /// <remarks>
    /// Invoked by <see cref="Decrypt" /> on hosts without AVX-512F support. Operates on eight 64-bit words held in
    /// stack-resident locals, with the key/tweak schedule accessed via interior refs so subkey injections skip
    /// per-element bounds checks.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void DecryptScalar(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // Load the ciphertext block as eight 64-bit little-endian words into registers, skipping the
        // intermediate stackalloc + MemoryMarshal.Cast + CopyTo trip through the stack the prior
        // implementation used.
        ref byte inputRef = ref MemoryMarshal.GetReference(input);
        ulong b0 = LoadWordLittleEndian(ref inputRef, 0);
        ulong b1 = LoadWordLittleEndian(ref inputRef, 8);
        ulong b2 = LoadWordLittleEndian(ref inputRef, 16);
        ulong b3 = LoadWordLittleEndian(ref inputRef, 24);
        ulong b4 = LoadWordLittleEndian(ref inputRef, 32);
        ulong b5 = LoadWordLittleEndian(ref inputRef, 40);
        ulong b6 = LoadWordLittleEndian(ref inputRef, 48);
        ulong b7 = LoadWordLittleEndian(ref inputRef, 56);

        // Capture interior refs to the key and tweak schedule arrays so each subkey-injection access
        // skips the per-element bounds check that ulong[] indexing would otherwise emit.
        ref ulong keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref ulong tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        for (int d = (72 / 4) - 1; d >= 1; d -= 2)
        {
            int dm9 = d % 9, dm3 = d % 3;

            b0 -= Unsafe.Add(ref keyRef, dm9 + 1);
            b1 -= Unsafe.Add(ref keyRef, dm9 + 2);
            b2 -= Unsafe.Add(ref keyRef, dm9 + 3);
            b3 -= Unsafe.Add(ref keyRef, dm9 + 4);
            b4 -= Unsafe.Add(ref keyRef, dm9 + 5);
            b5 -= Unsafe.Add(ref keyRef, dm9 + 6) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b6 -= Unsafe.Add(ref keyRef, dm9 + 7) + Unsafe.Add(ref tweakRef, dm3 + 2);
            b7 -= Unsafe.Add(ref keyRef, dm9 + 8) + (ulong)(d + 1);

            Unmix(ref b4, ref b3, R31);
            Unmix(ref b2, ref b5, R30);
            Unmix(ref b0, ref b7, R29);
            Unmix(ref b6, ref b1, R28);
            Unmix(ref b2, ref b7, R27);
            Unmix(ref b0, ref b5, R26);
            Unmix(ref b6, ref b3, R25);
            Unmix(ref b4, ref b1, R24);
            Unmix(ref b0, ref b3, R23);
            Unmix(ref b6, ref b5, R22);
            Unmix(ref b4, ref b7, R21);
            Unmix(ref b2, ref b1, R20);
            Unmix(ref b6, ref b7, R19);
            Unmix(ref b4, ref b5, R18);
            Unmix(ref b2, ref b3, R17);
            Unmix(ref b0, ref b1, R16);

            b0 -= Unsafe.Add(ref keyRef, dm9);
            b1 -= Unsafe.Add(ref keyRef, dm9 + 1);
            b2 -= Unsafe.Add(ref keyRef, dm9 + 2);
            b3 -= Unsafe.Add(ref keyRef, dm9 + 3);
            b4 -= Unsafe.Add(ref keyRef, dm9 + 4);
            b5 -= Unsafe.Add(ref keyRef, dm9 + 5) + Unsafe.Add(ref tweakRef, dm3);
            b6 -= Unsafe.Add(ref keyRef, dm9 + 6) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b7 -= Unsafe.Add(ref keyRef, dm9 + 7) + (ulong)d;

            Unmix(ref b4, ref b3, R15);
            Unmix(ref b2, ref b5, R14);
            Unmix(ref b0, ref b7, R13);
            Unmix(ref b6, ref b1, R12);
            Unmix(ref b2, ref b7, R11);
            Unmix(ref b0, ref b5, R10);
            Unmix(ref b6, ref b3, R9);
            Unmix(ref b4, ref b1, R8);
            Unmix(ref b0, ref b3, R7);
            Unmix(ref b6, ref b5, R6);
            Unmix(ref b4, ref b7, R5);
            Unmix(ref b2, ref b1, R4);
            Unmix(ref b6, ref b7, R3);
            Unmix(ref b4, ref b5, R2);
            Unmix(ref b2, ref b3, R1);
            Unmix(ref b0, ref b1, R0);
        }

        b0 -= Unsafe.Add(ref keyRef, 0);
        b1 -= Unsafe.Add(ref keyRef, 1);
        b2 -= Unsafe.Add(ref keyRef, 2);
        b3 -= Unsafe.Add(ref keyRef, 3);
        b4 -= Unsafe.Add(ref keyRef, 4);
        b5 -= Unsafe.Add(ref keyRef, 5) + Unsafe.Add(ref tweakRef, 0);
        b6 -= Unsafe.Add(ref keyRef, 6) + Unsafe.Add(ref tweakRef, 1);
        b7 -= Unsafe.Add(ref keyRef, 7);

        // Commit the eight plaintext words to the output buffer in little-endian byte order. On LE
        // hosts each call lowers to a single unaligned store.
        ref byte outputRef = ref MemoryMarshal.GetReference(output);
        StoreWordLittleEndian(ref outputRef, 0, b0);
        StoreWordLittleEndian(ref outputRef, 8, b1);
        StoreWordLittleEndian(ref outputRef, 16, b2);
        StoreWordLittleEndian(ref outputRef, 24, b3);
        StoreWordLittleEndian(ref outputRef, 32, b4);
        StoreWordLittleEndian(ref outputRef, 40, b5);
        StoreWordLittleEndian(ref outputRef, 48, b6);
        StoreWordLittleEndian(ref outputRef, 56, b7);
    }

    /// <summary>
    /// Encrypts a single 64-byte plaintext block using the <c>Threefish-512</c> cipher and writes the result to the
    /// specified output buffer.
    /// </summary>
    /// <param name="input">The 64-byte plaintext block to encrypt.</param>
    /// <param name="output">The 64-byte buffer to receive the encrypted ciphertext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> or <paramref name="output" /> is not 64 bytes.
    /// </exception>
    /// <remarks>
    /// Dispatches to an AVX-512F vectorised implementation when supported by the host, falling back to a scalar
    /// register-resident implementation otherwise. <see cref="Avx512F.IsSupported" /> is a JIT intrinsic that folds to
    /// a compile-time constant, so the branch carries no runtime cost.
    /// </remarks>
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != 64 || output.Length != 64)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_BlockLength, 64));
        }

        if (Avx512F.IsSupported)
        {
            EncryptAvx512(input, output);
            return;
        }

        EncryptScalar(input, output);
    }

    /// <summary>
    /// Encrypts a single 64-byte plaintext block using the scalar register-resident Threefish-512 implementation.
    /// </summary>
    /// <param name="input">The 64-byte plaintext block to encrypt. Caller is responsible for length validation.</param>
    /// <param name="output">The 64-byte buffer to receive the encrypted ciphertext block.</param>
    /// <remarks>
    /// Invoked by <see cref="Encrypt" /> on hosts without AVX-512F support. Operates on eight 64-bit words held in
    /// stack-resident locals, with the key/tweak schedule accessed via interior refs so subkey injections skip
    /// per-element bounds checks.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void EncryptScalar(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // Load the plaintext block as eight 64-bit little-endian words into registers.
        ref byte inputRef = ref MemoryMarshal.GetReference(input);
        ulong b0 = LoadWordLittleEndian(ref inputRef, 0);
        ulong b1 = LoadWordLittleEndian(ref inputRef, 8);
        ulong b2 = LoadWordLittleEndian(ref inputRef, 16);
        ulong b3 = LoadWordLittleEndian(ref inputRef, 24);
        ulong b4 = LoadWordLittleEndian(ref inputRef, 32);
        ulong b5 = LoadWordLittleEndian(ref inputRef, 40);
        ulong b6 = LoadWordLittleEndian(ref inputRef, 48);
        ulong b7 = LoadWordLittleEndian(ref inputRef, 56);

        ref ulong keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref ulong tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        // Initial key injection (round 0)
        b0 += Unsafe.Add(ref keyRef, 0);
        b1 += Unsafe.Add(ref keyRef, 1);
        b2 += Unsafe.Add(ref keyRef, 2);
        b3 += Unsafe.Add(ref keyRef, 3);
        b4 += Unsafe.Add(ref keyRef, 4);
        b5 += Unsafe.Add(ref keyRef, 5) + Unsafe.Add(ref tweakRef, 0);
        b6 += Unsafe.Add(ref keyRef, 6) + Unsafe.Add(ref tweakRef, 1);
        b7 += Unsafe.Add(ref keyRef, 7);

        for (int d = 1; d < 72 / 4; d += 2)
        {
            int dm9 = d % 9, dm3 = d % 3;

            Mix(ref b0, ref b1, R0);
            Mix(ref b2, ref b3, R1);
            Mix(ref b4, ref b5, R2);
            Mix(ref b6, ref b7, R3);
            Mix(ref b2, ref b1, R4);
            Mix(ref b4, ref b7, R5);
            Mix(ref b6, ref b5, R6);
            Mix(ref b0, ref b3, R7);
            Mix(ref b4, ref b1, R8);
            Mix(ref b6, ref b3, R9);
            Mix(ref b0, ref b5, R10);
            Mix(ref b2, ref b7, R11);
            Mix(ref b6, ref b1, R12);
            Mix(ref b0, ref b7, R13);
            Mix(ref b2, ref b5, R14);
            Mix(ref b4, ref b3, R15);

            b0 += Unsafe.Add(ref keyRef, dm9);
            b1 += Unsafe.Add(ref keyRef, dm9 + 1);
            b2 += Unsafe.Add(ref keyRef, dm9 + 2);
            b3 += Unsafe.Add(ref keyRef, dm9 + 3);
            b4 += Unsafe.Add(ref keyRef, dm9 + 4);
            b5 += Unsafe.Add(ref keyRef, dm9 + 5) + Unsafe.Add(ref tweakRef, dm3);
            b6 += Unsafe.Add(ref keyRef, dm9 + 6) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b7 += Unsafe.Add(ref keyRef, dm9 + 7) + (ulong)d;

            Mix(ref b0, ref b1, R16);
            Mix(ref b2, ref b3, R17);
            Mix(ref b4, ref b5, R18);
            Mix(ref b6, ref b7, R19);
            Mix(ref b2, ref b1, R20);
            Mix(ref b4, ref b7, R21);
            Mix(ref b6, ref b5, R22);
            Mix(ref b0, ref b3, R23);
            Mix(ref b4, ref b1, R24);
            Mix(ref b6, ref b3, R25);
            Mix(ref b0, ref b5, R26);
            Mix(ref b2, ref b7, R27);
            Mix(ref b6, ref b1, R28);
            Mix(ref b0, ref b7, R29);
            Mix(ref b2, ref b5, R30);
            Mix(ref b4, ref b3, R31);

            b0 += Unsafe.Add(ref keyRef, dm9 + 1);
            b1 += Unsafe.Add(ref keyRef, dm9 + 2);
            b2 += Unsafe.Add(ref keyRef, dm9 + 3);
            b3 += Unsafe.Add(ref keyRef, dm9 + 4);
            b4 += Unsafe.Add(ref keyRef, dm9 + 5);
            b5 += Unsafe.Add(ref keyRef, dm9 + 6) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b6 += Unsafe.Add(ref keyRef, dm9 + 7) + Unsafe.Add(ref tweakRef, dm3 + 2);
            b7 += Unsafe.Add(ref keyRef, dm9 + 8) + (ulong)(d + 1);
        }

        ref byte outputRef = ref MemoryMarshal.GetReference(output);
        StoreWordLittleEndian(ref outputRef, 0, b0);
        StoreWordLittleEndian(ref outputRef, 8, b1);
        StoreWordLittleEndian(ref outputRef, 16, b2);
        StoreWordLittleEndian(ref outputRef, 24, b3);
        StoreWordLittleEndian(ref outputRef, 32, b4);
        StoreWordLittleEndian(ref outputRef, 40, b5);
        StoreWordLittleEndian(ref outputRef, 48, b6);
        StoreWordLittleEndian(ref outputRef, 56, b7);
    }
}
