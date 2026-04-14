// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides a managed implementation of the <c>Skipjack</c> block cipher engine, operating on 64-bit blocks with an 80-bit key
    /// over 32 rounds. The key schedule is binary-compatible with Bouncy Castle, OpenSSL, and the original NSA reference implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skipjack is a legacy symmetric block cipher whose 32 rounds alternate between two nonlinear rules known as <em>Rule A</em> and
    /// <em>Rule B</em>. In this implementation the round-key byte pointer advances by one per round and each round uses a constant
    /// equal to <c>k + 1</c>.
    /// </para>
    /// <para>
    /// This cipher is included for compatibility and historical purposes only. Due to its small key and block sizes, Skipjack is not
    /// considered secure for use in new systems or applications.
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description><b>Block size:</b><c>8 bytes</c> (64 bits)</description>
    /// </item>
    /// <item>
    /// <description><b>Key size:</b><c>10 bytes</c> (80 bits)</description>
    /// </item>
    /// <item>
    /// <description><b>Rounds:</b><c>32</c> (16 × Rule A + 16 × Rule B)</description>
    /// </item>
    /// </list>
    /// <para>
    /// This implementation is constant-time in its control flow, but the S-box lookup table remains data-dependent. As such, this
    /// implementation is <b>not</b> hardened against timing or cache-based side-channel attacks.
    /// </para>
    /// </remarks>
    public sealed class SkipjackBlockCipher
        : IBlockCipher
    {
        /// <summary>
        /// Internal block size (bytes).
        /// </summary>
        public const int BlockBytes = 8;

        /// <summary>
        /// Length of a Skipjack key (bytes).
        /// </summary>
        public const int KeySize = 10;  // 80 bits

        // Static F-table (8 × 8 S-box)
        private static readonly byte[] F = new byte[256] {
            0xa3,0xd7,0x09,0x83,0xf8,0x48,0xf6,0xf4,0xb3,0x21,0x15,0x78,0x99,0xb1,0xaf,0xf9,
            0xe7,0x2d,0x4d,0x8a,0xce,0x4c,0xca,0x2e,0x52,0x95,0xd9,0x1e,0x4e,0x38,0x44,0x28,
            0x0a,0xdf,0x02,0xa0,0x17,0xf1,0x60,0x68,0x12,0xb7,0x7a,0xc3,0xe9,0xfa,0x3d,0x53,
            0x96,0x84,0x6b,0xba,0xf2,0x63,0x9a,0x19,0x7c,0xae,0xe5,0xf5,0xf7,0x16,0x6a,0xa2,
            0x39,0xb6,0x7b,0x0f,0xc1,0x93,0x81,0x1b,0xee,0xb4,0x1a,0xea,0xd0,0x91,0x2f,0xb8,
            0x55,0xb9,0xda,0x85,0x3f,0x41,0xbf,0xe0,0x5a,0x58,0x80,0x5f,0x66,0x0b,0xd8,0x90,
            0x35,0xd5,0xc0,0xa7,0x33,0x06,0x65,0x69,0x45,0x00,0x94,0x56,0x6d,0x98,0x9b,0x76,
            0x97,0xfc,0xb2,0xc2,0xb0,0xfe,0xdb,0x20,0xe1,0xeb,0xd6,0xe4,0xdd,0x47,0x4a,0x1d,
            0x42,0xed,0x9e,0x6e,0x49,0x3c,0xcd,0x43,0x27,0xd2,0x07,0xd4,0xde,0xc7,0x67,0x18,
            0x89,0xcb,0x30,0x1f,0x8d,0xc6,0x8f,0xaa,0xc8,0x74,0xdc,0xc9,0x5d,0x5c,0x31,0xa4,
            0x70,0x88,0x61,0x2c,0x9f,0x0d,0x2b,0x87,0x50,0x82,0x54,0x64,0x26,0x7d,0x03,0x40,
            0x34,0x4b,0x1c,0x73,0xd1,0xc4,0xfd,0x3b,0xcc,0xfb,0x7f,0xab,0xe6,0x3e,0x5b,0xa5,
            0xad,0x04,0x23,0x9c,0x14,0x51,0x22,0xf0,0x29,0x79,0x71,0x7e,0xff,0x8c,0x0e,0xe2,
            0x0c,0xef,0xbc,0x72,0x75,0x6f,0x37,0xa1,0xec,0xd3,0x8e,0x62,0x8b,0x86,0x10,0xe8,
            0x08,0x77,0x11,0xbe,0x92,0x4f,0x24,0xc5,0x32,0x36,0x9d,0xcf,0xf3,0xa6,0xbb,0xac,
            0x5e,0x6c,0xa9,0x13,0x57,0x25,0xb5,0xe3,0xbd,0xa8,0x3a,0x01,0x05,0x59,0x2a,0x46 };

        private readonly byte[] key = new byte[KeySize];
        private bool disposed = false;

        /// <summary>
        /// Creates a new <see cref="SkipjackBlockCipher" /> instance using the supplied 80-bit key.
        /// </summary>
        /// <param name="keyBytes">Exactly 10 bytes of key material.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="keyBytes" /> is not exactly 10 bytes long.</exception>
        public SkipjackBlockCipher(ReadOnlySpan<byte> keyBytes)
        {
            if (keyBytes.Length != KeySize)
                throw new ArgumentException("Skipjack requires an 80-bit key (10 bytes).", nameof(keyBytes));

            keyBytes.CopyTo(this.key);
        }

        /// <inheritdoc />
        /// <remarks>The block size is fixed at 8 bytes (64 bits) and cannot be changed.</remarks>
        public int BlockSize => BlockBytes;

        /// <summary>
        /// Decrypts a single 64-bit ciphertext block.
        /// </summary>
        /// <param name="input">Ciphertext of at least 8 bytes.</param>
        /// <param name="output">Buffer that receives the decrypted plaintext.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="input" /> or <paramref name="output" /> is too small.</exception>
        /// <exception cref="ObjectDisposedException">The cipher instance has been disposed.</exception>
        /// <remarks>Mirrors the BC/OpenSSL decrypt sequence, including the word-order swap in the input/output stages.</remarks>
        public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(input, BlockBytes);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, BlockBytes);
            this.ThrowIfDisposed();

            // BC decrypt swaps words W1↔W2 and W3↔W4 on input
            ushort w2 = ReadBE16(input, 0);
            ushort w1 = ReadBE16(input, 2);
            ushort w4 = ReadBE16(input, 4);
            ushort w3 = ReadBE16(input, 6);

            int k = 31; // start from last round key

            for (int round = 0; round < 32; round++)
            {
                int rc = k + 1;

                if ((round / 8 & 1) == 0) // inverse RULE B
                {
                    ushort tmp = w4;
                    w4 = w3;
                    w3 = (ushort)(w1 ^ w2 ^ rc);
                    w2 = this.H(k, w1);
                    w1 = tmp;
                }
                else // inverse RULE A
                {
                    ushort tmp = w4;
                    w4 = w3;
                    w3 = w2;
                    w2 = this.H(k, w1);
                    w1 = (ushort)(w2 ^ tmp ^ rc);
                }

                k = (k - 1) & 31;
            }

            // Swap back to BC's output order
            WriteBE16(output, 0, w2);
            WriteBE16(output, 2, w1);
            WriteBE16(output, 4, w4);
            WriteBE16(output, 6, w3);
        }

        /// <summary>
        /// Securely clears key material and marks the instance as disposed.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                CryptoHelpers.Clear(this.key); // fills array with zeros
                this.disposed = true;
            }
        }

        /// <summary>
        /// Encrypts a single 64-bit block.
        /// </summary>
        /// <param name="input">The plaintext block to encrypt. Must be at least <see cref="BlockBytes" /> bytes long.</param>
        /// <param name="output">Buffer that receives the 8-byte ciphertext. Must be at least <see cref="BlockBytes" /> bytes long.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="input" /> or <paramref name="output" /> is too small.</exception>
        /// <exception cref="ObjectDisposedException">The cipher instance has been disposed.</exception>
        /// <remarks>
        /// The routine implements the BC/OpenSSL key-schedule: the key pointer advances by one byte per round and the round constant is (
        /// <c>k + 1</c>). See the class-level remarks for details.
        /// </remarks>
        public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(input, BlockBytes);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, BlockBytes);
            this.ThrowIfDisposed();

            ushort w1 = ReadBE16(input, 0);
            ushort w2 = ReadBE16(input, 2);
            ushort w3 = ReadBE16(input, 4);
            ushort w4 = ReadBE16(input, 6);

            int k = 0; // round-key index 0…31

            for (int round = 0; round < 32; round++)
            {
                int rc = k + 1; // round constant

                if ((round / 8 & 1) == 0) // RULE A (rounds 0-7,16-23)
                {
                    ushort t = this.G(k, w1);
                    w1 = (ushort)(t ^ w4 ^ rc);
                    (w2, w3, w4) = (t, w2, w3);
                }
                else // RULE B (rounds 8-15,24-31)
                {
                    ushort t = w4;
                    w4 = w3;
                    w3 = (ushort)(w1 ^ w2 ^ rc);
                    w2 = this.G(k, w1);
                    w1 = t;
                }

                k = (k + 1) & 31; // +1 byte per round
            }

            WriteBE16(output, 0, w1);
            WriteBE16(output, 2, w2);
            WriteBE16(output, 4, w3);
            WriteBE16(output, 6, w4);
        }

        /// <summary>
        /// Reads a big-endian 16-bit unsigned integer from <paramref name="s" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ReadBE16(ReadOnlySpan<byte> s, int o) =>
            (ushort)((s[o] << 8) | s[o + 1]);

        /// <summary>
        /// Writes <paramref name="v" /> as big-endian 16-bit value into <paramref name="d" />.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteBE16(Span<byte> d, int o, ushort v)
        {
            d[o] = (byte)(v >> 8);
            d[o + 1] = (byte)v;
        }

        /// <summary>
        /// Skipjack <c>G</c> permutation (forward) – uses 4 key bytes starting at index <paramref name="k" />.
        /// </summary>
        /// <param name="k">Round-key index (0–31).</param>
        /// <param name="w">16-bit input word.</param>
        /// <returns>Permuted 16-bit word.</returns>
        /// <remarks>
        /// The four key bytes are selected as <c>key[(k*4 + i) mod 10]</c> for <c>i = 0…3</c>, exactly matching Bouncy Castle / OpenSSL.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort G(int k, ushort w)
        {
            byte g1 = (byte)(w >> 8);
            byte g2 = (byte)w;

            byte g3 = (byte)(F[g2 ^ this.key[(k * 4 + 0) % 10]] ^ g1);
            byte g4 = (byte)(F[g3 ^ this.key[(k * 4 + 1) % 10]] ^ g2);
            byte g5 = (byte)(F[g4 ^ this.key[(k * 4 + 2) % 10]] ^ g3);
            byte g6 = (byte)(F[g5 ^ this.key[(k * 4 + 3) % 10]] ^ g4);

            return (ushort)((g5 << 8) | g6);
        }

        /// <summary>
        /// Inverse Skipjack permutation <c>H = G⁻¹</c>.
        /// </summary>
        /// <param name="k">Round-key index (0–31).</param>
        /// <param name="w">16-bit input word.</param>
        /// <returns>The inverse-permuted 16-bit word.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort H(int k, ushort w)
        {
            byte h1 = (byte)w;
            byte h2 = (byte)(w >> 8);

            byte h3 = (byte)(F[h2 ^ this.key[(k * 4 + 3) % 10]] ^ h1);
            byte h4 = (byte)(F[h3 ^ this.key[(k * 4 + 2) % 10]] ^ h2);
            byte h5 = (byte)(F[h4 ^ this.key[(k * 4 + 1) % 10]] ^ h3);
            byte h6 = (byte)(F[h5 ^ this.key[(k * 4 + 0) % 10]] ^ h4);

            return (ushort)((h6 << 8) | h5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(this.disposed, this);
#else
            if (disposed) throw new ObjectDisposedException(nameof(SkipjackBlockCipher));
#endif
        }
    }
}