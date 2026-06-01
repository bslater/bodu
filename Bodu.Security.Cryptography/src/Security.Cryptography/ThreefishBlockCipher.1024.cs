// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.1024.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the <c>Threefish-1024</c> block cipher, which operates on 1024-bit (128-byte) blocks using a 1024-bit key
/// and a 128-bit tweak.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is a tweakable block cipher optimized for 64-bit platforms and forms the core primitive of the Skein hash
/// function. The <c>Threefish-1024</c> variant operates on sixteen 64-bit words over 80 rounds using modular addition,
/// bitwise rotation, and XOR.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Threefish1024" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract. Use <see cref="Threefish1024Cipher" />
/// directly only when composing the raw block primitive with an <see cref="IBlockCipherModeTransform" /> (for example
/// via <see cref="BlockCipherModeFactory" />) or with an <see cref="IPaddingStrategy" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Direct single-block use — most callers should prefer the Threefish1024 SymmetricAlgorithm.
/// byte[] key   = new byte[128];   // 1024-bit key
/// byte[] tweak = new byte[16];    // 128-bit tweak
/// RandomNumberGenerator.Fill(key);
/// RandomNumberGenerator.Fill(tweak);
///
/// using var cipher = new Threefish1024Cipher(key, tweak);
///
/// byte[] plaintext  = new byte[128];   // one 1024-bit block
/// byte[] ciphertext = new byte[128];
/// cipher.Encrypt(plaintext, ciphertext);
///
/// byte[] roundtrip = new byte[128];
/// cipher.Decrypt(ciphertext, roundtrip);
/// roundtrip equals plaintext
///]]>
/// </example>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs.
/// SymmetricAlgorithm</seealso> <seealso cref="Threefish1024"/>
public sealed partial class Threefish1024Cipher
    : ThreefishBlockCipher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish1024Cipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 1024-bit (128-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behavior.</param>
    public Threefish1024Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <summary>
    /// Length of the Threefish-1024 key is 1024 bits (128 bytes).
    /// </summary>
    public const int KeySize = 1024;

    // Spec-defined rotation constants for Threefish-1024. Declared as named const ints so the JIT can
    // fold each Mix/Unmix call to a ROL/ROR with an immediate count, and so all 64 values live in one
    // place rather than being duplicated between Encrypt, Decrypt, and the public RotationSchedule.
    private const int R0 = 24, R1 = 13, R2 = 8, R3 = 47;
    private const int R4 = 8, R5 = 17, R6 = 22, R7 = 37;
    private const int R8 = 38, R9 = 19, R10 = 10, R11 = 55;
    private const int R12 = 49, R13 = 18, R14 = 23, R15 = 52;
    private const int R16 = 33, R17 = 4, R18 = 51, R19 = 13;
    private const int R20 = 34, R21 = 41, R22 = 59, R23 = 17;
    private const int R24 = 5, R25 = 20, R26 = 48, R27 = 41;
    private const int R28 = 47, R29 = 28, R30 = 16, R31 = 25;
    private const int R32 = 41, R33 = 9, R34 = 37, R35 = 31;
    private const int R36 = 12, R37 = 47, R38 = 44, R39 = 30;
    private const int R40 = 16, R41 = 34, R42 = 56, R43 = 51;
    private const int R44 = 4, R45 = 53, R46 = 42, R47 = 41;
    private const int R48 = 31, R49 = 44, R50 = 47, R51 = 46;
    private const int R52 = 19, R53 = 42, R54 = 44, R55 = 25;
    private const int R56 = 9, R57 = 48, R58 = 35, R59 = 52;
    private const int R60 = 23, R61 = 31, R62 = 37, R63 = 20;

    private static readonly int[] s_rotationSchedule =
    [
        R0, R1, R2, R3, R4, R5, R6, R7,
        R8, R9, R10, R11, R12, R13, R14, R15,
        R16, R17, R18, R19, R20, R21, R22, R23,
        R24, R25, R26, R27, R28, R29, R30, R31,
        R32, R33, R34, R35, R36, R37, R38, R39,
        R40, R41, R42, R43, R44, R45, R46, R47,
        R48, R49, R50, R51, R52, R53, R54, R55,
        R56, R57, R58, R59, R60, R61, R62, R63,
    ];

    /// <inheritdoc />
    /// <value>Length of the Threefish-1024 block is 1024 bits (128 bytes).</value>
    public override int BlockSize => 1024;

    /// <inheritdoc />
    protected override int BlockWords => 16;

    /// <inheritdoc />
    protected override int[] RotationSchedule => s_rotationSchedule;

    /// <inheritdoc />
    protected override int Rounds => 80;

    /// <summary>
    /// Decrypts a single 128-byte ciphertext block using the <c>Threefish-1024</c> cipher and writes the result to the
    /// specified output buffer.
    /// </summary>
    /// <param name="input">The 128-byte ciphertext block to decrypt.</param>
    /// <param name="output">The 128-byte buffer to receive the decrypted plaintext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> or <paramref name="output" /> is not 128 bytes.
    /// </exception>
    /// <remarks>
    /// Dispatches to an AVX-512F vectorised implementation when supported by the host, falling back to a scalar
    /// register-resident implementation otherwise. <see cref="Avx512F.IsSupported" /> is a JIT intrinsic that folds to
    /// a compile-time constant, so the branch carries no runtime cost.
    /// </remarks>
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != 128 || output.Length != 128)
        {
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_BlockLength, 128));
        }

        if (Avx512F.IsSupported)
        {
            DecryptAvx512(input, output);
            return;
        }

        DecryptScalar(input, output);
    }

    /// <summary>
    /// Decrypts a single 128-byte ciphertext block using the scalar register-resident Threefish-1024 implementation.
    /// </summary>
    /// <param name="input">
    /// The 128-byte ciphertext block to decrypt. Caller is responsible for length validation.
    /// </param>
    /// <param name="output">The 128-byte buffer to receive the decrypted plaintext block.</param>
    /// <remarks>
    /// Invoked by <see cref="Decrypt" /> on hosts without AVX-512F support. Operates on sixteen 64-bit words held in
    /// stack-resident locals — note that the working set exceeds the x64 general-purpose register file, so the JIT will
    /// spill some words to stack. Even with spills, the elimination of bounds-checked Span indexing on every Mix/key
    /// access remains a net win versus the prior stackalloc-based path.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void DecryptScalar(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // Load the ciphertext block as sixteen 64-bit little-endian words into locals.
        ref var inputRef = ref MemoryMarshal.GetReference(input);
        var b0 = LoadWordLittleEndian(ref inputRef, 0);
        var b1 = LoadWordLittleEndian(ref inputRef, 8);
        var b2 = LoadWordLittleEndian(ref inputRef, 16);
        var b3 = LoadWordLittleEndian(ref inputRef, 24);
        var b4 = LoadWordLittleEndian(ref inputRef, 32);
        var b5 = LoadWordLittleEndian(ref inputRef, 40);
        var b6 = LoadWordLittleEndian(ref inputRef, 48);
        var b7 = LoadWordLittleEndian(ref inputRef, 56);
        var b8 = LoadWordLittleEndian(ref inputRef, 64);
        var b9 = LoadWordLittleEndian(ref inputRef, 72);
        var b10 = LoadWordLittleEndian(ref inputRef, 80);
        var b11 = LoadWordLittleEndian(ref inputRef, 88);
        var b12 = LoadWordLittleEndian(ref inputRef, 96);
        var b13 = LoadWordLittleEndian(ref inputRef, 104);
        var b14 = LoadWordLittleEndian(ref inputRef, 112);
        var b15 = LoadWordLittleEndian(ref inputRef, 120);

        ref var keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref var tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        for (var d = (80 / 4) - 1; d >= 1; d -= 2)
        {
            int dm17 = d % 17, dm3 = d % 3;

            b0 -= Unsafe.Add(ref keyRef, dm17 + 1);
            b1 -= Unsafe.Add(ref keyRef, dm17 + 2);
            b2 -= Unsafe.Add(ref keyRef, dm17 + 3);
            b3 -= Unsafe.Add(ref keyRef, dm17 + 4);
            b4 -= Unsafe.Add(ref keyRef, dm17 + 5);
            b5 -= Unsafe.Add(ref keyRef, dm17 + 6);
            b6 -= Unsafe.Add(ref keyRef, dm17 + 7);
            b7 -= Unsafe.Add(ref keyRef, dm17 + 8);
            b8 -= Unsafe.Add(ref keyRef, dm17 + 9);
            b9 -= Unsafe.Add(ref keyRef, dm17 + 10);
            b10 -= Unsafe.Add(ref keyRef, dm17 + 11);
            b11 -= Unsafe.Add(ref keyRef, dm17 + 12);
            b12 -= Unsafe.Add(ref keyRef, dm17 + 13);
            b13 -= Unsafe.Add(ref keyRef, dm17 + 14) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b14 -= Unsafe.Add(ref keyRef, dm17 + 15) + Unsafe.Add(ref tweakRef, dm3 + 2);
            b15 -= Unsafe.Add(ref keyRef, dm17 + 16) + (uint)d + 1;

            Unmix(ref b12, ref b7, R63);
            Unmix(ref b10, ref b3, R62);
            Unmix(ref b8, ref b5, R61);
            Unmix(ref b14, ref b1, R60);
            Unmix(ref b4, ref b9, R59);
            Unmix(ref b6, ref b13, R58);
            Unmix(ref b2, ref b11, R57);
            Unmix(ref b0, ref b15, R56);

            Unmix(ref b10, ref b9, R55);
            Unmix(ref b8, ref b11, R54);
            Unmix(ref b14, ref b13, R53);
            Unmix(ref b12, ref b15, R52);
            Unmix(ref b6, ref b1, R51);
            Unmix(ref b4, ref b3, R50);
            Unmix(ref b2, ref b5, R49);
            Unmix(ref b0, ref b7, R48);

            Unmix(ref b8, ref b1, R47);
            Unmix(ref b14, ref b5, R46);
            Unmix(ref b12, ref b3, R45);
            Unmix(ref b10, ref b7, R44);
            Unmix(ref b4, ref b15, R43);
            Unmix(ref b6, ref b11, R42);
            Unmix(ref b2, ref b13, R41);
            Unmix(ref b0, ref b9, R40);

            Unmix(ref b14, ref b15, R39);
            Unmix(ref b12, ref b13, R38);
            Unmix(ref b10, ref b11, R37);
            Unmix(ref b8, ref b9, R36);
            Unmix(ref b6, ref b7, R35);
            Unmix(ref b4, ref b5, R34);
            Unmix(ref b2, ref b3, R33);
            Unmix(ref b0, ref b1, R32);

            b0 -= Unsafe.Add(ref keyRef, dm17);
            b1 -= Unsafe.Add(ref keyRef, dm17 + 1);
            b2 -= Unsafe.Add(ref keyRef, dm17 + 2);
            b3 -= Unsafe.Add(ref keyRef, dm17 + 3);
            b4 -= Unsafe.Add(ref keyRef, dm17 + 4);
            b5 -= Unsafe.Add(ref keyRef, dm17 + 5);
            b6 -= Unsafe.Add(ref keyRef, dm17 + 6);
            b7 -= Unsafe.Add(ref keyRef, dm17 + 7);
            b8 -= Unsafe.Add(ref keyRef, dm17 + 8);
            b9 -= Unsafe.Add(ref keyRef, dm17 + 9);
            b10 -= Unsafe.Add(ref keyRef, dm17 + 10);
            b11 -= Unsafe.Add(ref keyRef, dm17 + 11);
            b12 -= Unsafe.Add(ref keyRef, dm17 + 12);
            b13 -= Unsafe.Add(ref keyRef, dm17 + 13) + Unsafe.Add(ref tweakRef, dm3);
            b14 -= Unsafe.Add(ref keyRef, dm17 + 14) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b15 -= Unsafe.Add(ref keyRef, dm17 + 15) + (uint)d;

            Unmix(ref b12, ref b7, R31);
            Unmix(ref b10, ref b3, R30);
            Unmix(ref b8, ref b5, R29);
            Unmix(ref b14, ref b1, R28);
            Unmix(ref b4, ref b9, R27);
            Unmix(ref b6, ref b13, R26);
            Unmix(ref b2, ref b11, R25);
            Unmix(ref b0, ref b15, R24);

            Unmix(ref b10, ref b9, R23);
            Unmix(ref b8, ref b11, R22);
            Unmix(ref b14, ref b13, R21);
            Unmix(ref b12, ref b15, R20);
            Unmix(ref b6, ref b1, R19);
            Unmix(ref b4, ref b3, R18);
            Unmix(ref b2, ref b5, R17);
            Unmix(ref b0, ref b7, R16);

            Unmix(ref b8, ref b1, R15);
            Unmix(ref b14, ref b5, R14);
            Unmix(ref b12, ref b3, R13);
            Unmix(ref b10, ref b7, R12);
            Unmix(ref b4, ref b15, R11);
            Unmix(ref b6, ref b11, R10);
            Unmix(ref b2, ref b13, R9);
            Unmix(ref b0, ref b9, R8);

            Unmix(ref b14, ref b15, R7);
            Unmix(ref b12, ref b13, R6);
            Unmix(ref b10, ref b11, R5);
            Unmix(ref b8, ref b9, R4);
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
        b5 -= Unsafe.Add(ref keyRef, 5);
        b6 -= Unsafe.Add(ref keyRef, 6);
        b7 -= Unsafe.Add(ref keyRef, 7);
        b8 -= Unsafe.Add(ref keyRef, 8);
        b9 -= Unsafe.Add(ref keyRef, 9);
        b10 -= Unsafe.Add(ref keyRef, 10);
        b11 -= Unsafe.Add(ref keyRef, 11);
        b12 -= Unsafe.Add(ref keyRef, 12);
        b13 -= Unsafe.Add(ref keyRef, 13) + Unsafe.Add(ref tweakRef, 0);
        b14 -= Unsafe.Add(ref keyRef, 14) + Unsafe.Add(ref tweakRef, 1);
        b15 -= Unsafe.Add(ref keyRef, 15);

        ref var outputRef = ref MemoryMarshal.GetReference(output);
        StoreWordLittleEndian(ref outputRef, 0, b0);
        StoreWordLittleEndian(ref outputRef, 8, b1);
        StoreWordLittleEndian(ref outputRef, 16, b2);
        StoreWordLittleEndian(ref outputRef, 24, b3);
        StoreWordLittleEndian(ref outputRef, 32, b4);
        StoreWordLittleEndian(ref outputRef, 40, b5);
        StoreWordLittleEndian(ref outputRef, 48, b6);
        StoreWordLittleEndian(ref outputRef, 56, b7);
        StoreWordLittleEndian(ref outputRef, 64, b8);
        StoreWordLittleEndian(ref outputRef, 72, b9);
        StoreWordLittleEndian(ref outputRef, 80, b10);
        StoreWordLittleEndian(ref outputRef, 88, b11);
        StoreWordLittleEndian(ref outputRef, 96, b12);
        StoreWordLittleEndian(ref outputRef, 104, b13);
        StoreWordLittleEndian(ref outputRef, 112, b14);
        StoreWordLittleEndian(ref outputRef, 120, b15);
    }

    /// <summary>
    /// Encrypts a single 128-byte plaintext block using the <c>Threefish-1024</c> cipher and writes the result to the
    /// specified output buffer.
    /// </summary>
    /// <param name="input">The 128-byte plaintext block to encrypt.</param>
    /// <param name="output">The 128-byte buffer to receive the encrypted ciphertext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> or <paramref name="output" /> is not 128 bytes.
    /// </exception>
    /// <remarks>
    /// Dispatches to an AVX-512F vectorised implementation when supported by the host, falling back to a scalar
    /// register-resident implementation otherwise. <see cref="Avx512F.IsSupported" /> is a JIT intrinsic that folds to
    /// a compile-time constant, so the branch carries no runtime cost.
    /// </remarks>
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        if (input.Length != 128 || output.Length != 128)
        {
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_BlockLength, 128));
        }

        if (Avx512F.IsSupported)
        {
            EncryptAvx512(input, output);
            return;
        }

        EncryptScalar(input, output);
    }

    /// <summary>
    /// Encrypts a single 128-byte plaintext block using the scalar register-resident Threefish-1024 implementation.
    /// </summary>
    /// <param name="input">
    /// The 128-byte plaintext block to encrypt. Caller is responsible for length validation.
    /// </param>
    /// <param name="output">The 128-byte buffer to receive the encrypted ciphertext block.</param>
    /// <remarks>
    /// Invoked by <see cref="Encrypt" /> on hosts without AVX-512F support. See <see cref="DecryptScalar" /> for
    /// register-pressure notes that apply to the 16-word working set.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void EncryptScalar(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ref var inputRef = ref MemoryMarshal.GetReference(input);
        var b0 = LoadWordLittleEndian(ref inputRef, 0);
        var b1 = LoadWordLittleEndian(ref inputRef, 8);
        var b2 = LoadWordLittleEndian(ref inputRef, 16);
        var b3 = LoadWordLittleEndian(ref inputRef, 24);
        var b4 = LoadWordLittleEndian(ref inputRef, 32);
        var b5 = LoadWordLittleEndian(ref inputRef, 40);
        var b6 = LoadWordLittleEndian(ref inputRef, 48);
        var b7 = LoadWordLittleEndian(ref inputRef, 56);
        var b8 = LoadWordLittleEndian(ref inputRef, 64);
        var b9 = LoadWordLittleEndian(ref inputRef, 72);
        var b10 = LoadWordLittleEndian(ref inputRef, 80);
        var b11 = LoadWordLittleEndian(ref inputRef, 88);
        var b12 = LoadWordLittleEndian(ref inputRef, 96);
        var b13 = LoadWordLittleEndian(ref inputRef, 104);
        var b14 = LoadWordLittleEndian(ref inputRef, 112);
        var b15 = LoadWordLittleEndian(ref inputRef, 120);

        ref var keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref var tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        b0 += Unsafe.Add(ref keyRef, 0);
        b1 += Unsafe.Add(ref keyRef, 1);
        b2 += Unsafe.Add(ref keyRef, 2);
        b3 += Unsafe.Add(ref keyRef, 3);
        b4 += Unsafe.Add(ref keyRef, 4);
        b5 += Unsafe.Add(ref keyRef, 5);
        b6 += Unsafe.Add(ref keyRef, 6);
        b7 += Unsafe.Add(ref keyRef, 7);
        b8 += Unsafe.Add(ref keyRef, 8);
        b9 += Unsafe.Add(ref keyRef, 9);
        b10 += Unsafe.Add(ref keyRef, 10);
        b11 += Unsafe.Add(ref keyRef, 11);
        b12 += Unsafe.Add(ref keyRef, 12);
        b13 += Unsafe.Add(ref keyRef, 13) + Unsafe.Add(ref tweakRef, 0);
        b14 += Unsafe.Add(ref keyRef, 14) + Unsafe.Add(ref tweakRef, 1);
        b15 += Unsafe.Add(ref keyRef, 15);

        for (var d = 1; d < 80 / 4; d += 2)
        {
            int dm17 = d % 17, dm3 = d % 3;

            Mix(ref b0, ref b1, R0);
            Mix(ref b2, ref b3, R1);
            Mix(ref b4, ref b5, R2);
            Mix(ref b6, ref b7, R3);
            Mix(ref b8, ref b9, R4);
            Mix(ref b10, ref b11, R5);
            Mix(ref b12, ref b13, R6);
            Mix(ref b14, ref b15, R7);
            Mix(ref b0, ref b9, R8);
            Mix(ref b2, ref b13, R9);
            Mix(ref b6, ref b11, R10);
            Mix(ref b4, ref b15, R11);
            Mix(ref b10, ref b7, R12);
            Mix(ref b12, ref b3, R13);
            Mix(ref b14, ref b5, R14);
            Mix(ref b8, ref b1, R15);
            Mix(ref b0, ref b7, R16);
            Mix(ref b2, ref b5, R17);
            Mix(ref b4, ref b3, R18);
            Mix(ref b6, ref b1, R19);
            Mix(ref b12, ref b15, R20);
            Mix(ref b14, ref b13, R21);
            Mix(ref b8, ref b11, R22);
            Mix(ref b10, ref b9, R23);
            Mix(ref b0, ref b15, R24);
            Mix(ref b2, ref b11, R25);
            Mix(ref b6, ref b13, R26);
            Mix(ref b4, ref b9, R27);
            Mix(ref b14, ref b1, R28);
            Mix(ref b8, ref b5, R29);
            Mix(ref b10, ref b3, R30);
            Mix(ref b12, ref b7, R31);

            b0 += Unsafe.Add(ref keyRef, dm17);
            b1 += Unsafe.Add(ref keyRef, dm17 + 1);
            b2 += Unsafe.Add(ref keyRef, dm17 + 2);
            b3 += Unsafe.Add(ref keyRef, dm17 + 3);
            b4 += Unsafe.Add(ref keyRef, dm17 + 4);
            b5 += Unsafe.Add(ref keyRef, dm17 + 5);
            b6 += Unsafe.Add(ref keyRef, dm17 + 6);
            b7 += Unsafe.Add(ref keyRef, dm17 + 7);
            b8 += Unsafe.Add(ref keyRef, dm17 + 8);
            b9 += Unsafe.Add(ref keyRef, dm17 + 9);
            b10 += Unsafe.Add(ref keyRef, dm17 + 10);
            b11 += Unsafe.Add(ref keyRef, dm17 + 11);
            b12 += Unsafe.Add(ref keyRef, dm17 + 12);
            b13 += Unsafe.Add(ref keyRef, dm17 + 13) + Unsafe.Add(ref tweakRef, dm3);
            b14 += Unsafe.Add(ref keyRef, dm17 + 14) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b15 += Unsafe.Add(ref keyRef, dm17 + 15) + (uint)d;

            Mix(ref b0, ref b1, R32);
            Mix(ref b2, ref b3, R33);
            Mix(ref b4, ref b5, R34);
            Mix(ref b6, ref b7, R35);
            Mix(ref b8, ref b9, R36);
            Mix(ref b10, ref b11, R37);
            Mix(ref b12, ref b13, R38);
            Mix(ref b14, ref b15, R39);
            Mix(ref b0, ref b9, R40);
            Mix(ref b2, ref b13, R41);
            Mix(ref b6, ref b11, R42);
            Mix(ref b4, ref b15, R43);
            Mix(ref b10, ref b7, R44);
            Mix(ref b12, ref b3, R45);
            Mix(ref b14, ref b5, R46);
            Mix(ref b8, ref b1, R47);
            Mix(ref b0, ref b7, R48);
            Mix(ref b2, ref b5, R49);
            Mix(ref b4, ref b3, R50);
            Mix(ref b6, ref b1, R51);
            Mix(ref b12, ref b15, R52);
            Mix(ref b14, ref b13, R53);
            Mix(ref b8, ref b11, R54);
            Mix(ref b10, ref b9, R55);
            Mix(ref b0, ref b15, R56);
            Mix(ref b2, ref b11, R57);
            Mix(ref b6, ref b13, R58);
            Mix(ref b4, ref b9, R59);
            Mix(ref b14, ref b1, R60);
            Mix(ref b8, ref b5, R61);
            Mix(ref b10, ref b3, R62);
            Mix(ref b12, ref b7, R63);

            b0 += Unsafe.Add(ref keyRef, dm17 + 1);
            b1 += Unsafe.Add(ref keyRef, dm17 + 2);
            b2 += Unsafe.Add(ref keyRef, dm17 + 3);
            b3 += Unsafe.Add(ref keyRef, dm17 + 4);
            b4 += Unsafe.Add(ref keyRef, dm17 + 5);
            b5 += Unsafe.Add(ref keyRef, dm17 + 6);
            b6 += Unsafe.Add(ref keyRef, dm17 + 7);
            b7 += Unsafe.Add(ref keyRef, dm17 + 8);
            b8 += Unsafe.Add(ref keyRef, dm17 + 9);
            b9 += Unsafe.Add(ref keyRef, dm17 + 10);
            b10 += Unsafe.Add(ref keyRef, dm17 + 11);
            b11 += Unsafe.Add(ref keyRef, dm17 + 12);
            b12 += Unsafe.Add(ref keyRef, dm17 + 13);
            b13 += Unsafe.Add(ref keyRef, dm17 + 14) + Unsafe.Add(ref tweakRef, dm3 + 1);
            b14 += Unsafe.Add(ref keyRef, dm17 + 15) + Unsafe.Add(ref tweakRef, dm3 + 2);
            b15 += Unsafe.Add(ref keyRef, dm17 + 16) + (uint)d + 1;
        }

        ref var outputRef = ref MemoryMarshal.GetReference(output);
        StoreWordLittleEndian(ref outputRef, 0, b0);
        StoreWordLittleEndian(ref outputRef, 8, b1);
        StoreWordLittleEndian(ref outputRef, 16, b2);
        StoreWordLittleEndian(ref outputRef, 24, b3);
        StoreWordLittleEndian(ref outputRef, 32, b4);
        StoreWordLittleEndian(ref outputRef, 40, b5);
        StoreWordLittleEndian(ref outputRef, 48, b6);
        StoreWordLittleEndian(ref outputRef, 56, b7);
        StoreWordLittleEndian(ref outputRef, 64, b8);
        StoreWordLittleEndian(ref outputRef, 72, b9);
        StoreWordLittleEndian(ref outputRef, 80, b10);
        StoreWordLittleEndian(ref outputRef, 88, b11);
        StoreWordLittleEndian(ref outputRef, 96, b12);
        StoreWordLittleEndian(ref outputRef, 104, b13);
        StoreWordLittleEndian(ref outputRef, 112, b14);
        StoreWordLittleEndian(ref outputRef, 120, b15);
    }
}
