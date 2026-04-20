// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Applies Offset CodeBook mode version 3 (OCB3) to an underlying <see cref="IBlockCipher" />,
    /// providing single-pass authenticated encryption with associated data per RFC 7253.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — OCB3 realises both the keystream and the MAC pipelines as a single offset-driven pass over each block." />
    /// </para>
    /// <para>
    /// OCB3 collapses the two pipelines of the generic AEAD shape above into a <em>single pass</em>: the
    /// keystream and the MAC chain share the same per-block offset Δ<sub>i</sub>, so each block is touched
    /// by the cipher exactly once. In the diagram, this corresponds to merging the top and bottom arrows
    /// that reach the MAC — the ciphertext output is simultaneously the next input to the authentication
    /// accumulator.
    /// </para>
    /// <para>
    /// The nonce is derived from the first 12 bytes of the IV supplied to the constructor.
    /// The tag length defaults to 16 bytes (TAGLEN = 128) and may be set to any value from
    /// 1 to 16 bytes via the <paramref name="tagLen" /> constructor parameter.
    /// Supported RFC 7253 values are 8, 12, and 16 bytes (64, 96, and 128 bits).
    /// </para>
    /// <para>
    /// Offset initialisation uses the RFC 7253 §2.4 K_top stretch:
    /// <code>
    ///   Nonce  = num2str(TAGLEN mod 128, 7) || zeros(120-bitlen(N)) || 1 || N
    ///   bottom = str2num(Nonce[123..128])
    ///   K_top  = ENCIPHER(K, Nonce[1..122] || zeros(6))
    ///   Stretch = K_top || (K_top[1..64] XOR K_top[9..72])   -- adjacent-byte XOR
    ///   Offset_0 = Stretch[1+bottom..128+bottom]
    /// </code>
    /// </para>
    /// <para>
    /// The L array uses GF(2^128) doubling with polynomial x^128 + x^7 + x^2 + x + 1 (big-endian).
    /// </para>
    /// </remarks>
    /// <seealso href="../guides/cryptography/aead-modes.html#ocb3--single-pass-rfc-7253">OCB3 walk-through in the AEAD-modes guide</seealso>
    /// <seealso cref="AesBlockCipher" />
    /// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions" />
    public sealed class OcbModeTransform
        : IAeadBlockCipherModeTransform
    {
        private const int NonceLengthBytes = 12;
        private const int MaxLValues = 32; // enough for 2^32 blocks

        private readonly IBlockCipher cipher;
        private readonly byte[] nonce;        // 12-byte nonce
        private readonly int tagLen;          // TAGLEN in bytes (1–blockSize)
        private readonly byte[] lStar;        // L_* = E(0^128)
        private readonly byte[] lDollar;      // L_$ = double(L_*)
        private readonly byte[][] lArray;     // L[0] = double(L_$), L[1] = double(L[0]), …
        private byte[]? aad;
        private bool aadProcessed;

        /// <summary>
        /// Initialises a new instance of the <see cref="OcbModeTransform" /> class.
        /// </summary>
        /// <param name="cipher">The block cipher (must have a 16-byte block size).</param>
        /// <param name="iv">
        /// The initialisation vector. The first 12 bytes are used as the OCB3 nonce.
        /// Must equal the cipher block size. A defensive copy is taken.
        /// </param>
        /// <param name="tagLen">
        /// Tag length in bytes. Must be between 1 and the cipher block size (16 for AES).
        /// RFC 7253 defines recommended values of 8, 12, and 16 bytes.
        /// Defaults to 16 (TAGLEN = 128 bits).
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="iv" /> length does not equal the cipher block size, or
        ///   <paramref name="tagLen" /> is outside the range [1, block size].
        /// </exception>
        public OcbModeTransform(IBlockCipher cipher, byte[] iv, int tagLen = 16)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (iv is null) throw new ArgumentNullException(nameof(iv));
            if (iv.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).", nameof(iv));
            if (tagLen < 1 || tagLen > cipher.BlockSize)
                throw new ArgumentException(
                    $"Tag length ({tagLen}) must be between 1 and the cipher block size ({cipher.BlockSize}).", nameof(tagLen));

            this.nonce = new byte[NonceLengthBytes];
            iv.AsSpan(0, NonceLengthBytes).CopyTo(this.nonce);
            this.tagLen = tagLen;

            int blockSize = cipher.BlockSize;

            // RFC 7253 §2.1 — Key-dependent constants derived once per key.
            // L_* = ENCIPHER(K, zeros(128))
            this.lStar = new byte[blockSize];
            cipher.Encrypt(new byte[blockSize], this.lStar);

            // L_$ = double(L_*)
            this.lDollar = GfDouble(this.lStar);

            // L[i] = double(L[i-1]),  L[0] = double(L_$)
            this.lArray = new byte[MaxLValues][];
            this.lArray[0] = GfDouble(this.lDollar);
            for (int i = 1; i < MaxLValues; i++)
                this.lArray[i] = GfDouble(this.lArray[i - 1]);
        }

        /// <inheritdoc />
        public int TagSize => this.tagLen;

        /// <inheritdoc />
        public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
        {
            if (this.aadProcessed)
                throw new InvalidOperationException("AssociatedData has already been processed.");
            this.aad = associatedData.ToArray();
            this.aadProcessed = true;
        }

        // ── RFC 7253 §2.6  OCB-ENCRYPT ────────────────────────────────────────────────────────────
        //
        // Input:  K (key), N (nonce), A (associated data), P (plaintext)
        // Output: C (ciphertext) || T (tag)
        //
        // Pseudocode (verbatim from RFC 7253 §2.6):
        //
        //   Offset_0 = ENCIPHER(K, Nonce) as in §2.4              -- computed by ComputeInitialOffset()
        //   Checksum_0 = zeros(128)
        //
        //   for i = 1 to m                                        -- m full 128-bit blocks
        //     Offset_i   = Offset_{i-1} xor L_{ntz(i)}
        //     C_i        = ENCIPHER(K, P_i xor Offset_i) xor Offset_i
        //     Checksum_i = Checksum_{i-1} xor P_i
        //   end for
        //
        //   if bitlen(P_*) > 0 then                              -- partial last block
        //     Offset_* = Offset_m xor L_*
        //     Pad      = ENCIPHER(K, Offset_*)
        //     C_*      = P_* xor Pad[1..bitlen(P_*)]
        //     Checksum_* = Checksum_m xor (P_* || 1 || zeros(127-bitlen(P_*)))
        //     T = ENCIPHER(K, L_$ xor Offset_* xor Checksum_*) xor HASH(K, A)
        //   else                                                  -- block-aligned (or empty) plaintext
        //     T = ENCIPHER(K, L_$ xor Offset_m xor Checksum_m) xor HASH(K, A)
        //   end if
        //
        //   return C = (C_1 || ... || C_m || C_*),  T
        // ──────────────────────────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
        {
            int required = plaintext.Length + TagSize;
            if (output.Length < required)
                throw new ArgumentException($"Output must be at least {required} bytes.", nameof(output));
            EnsureAadProcessed();

            int blockSize = this.cipher.BlockSize;

            // Offset_0  (§2.4 nonce processing — see ComputeInitialOffset)
            byte[] offset = ComputeInitialOffset();

            // Checksum_0 = zeros(128)
            byte[] checksum = new byte[blockSize];
            byte[] block = new byte[blockSize];

            // Partition P into (P_1, ..., P_m, P_*).
            // m = number of complete blocks; when P is empty m == 0, no loop runs.
            int m = (plaintext.Length + blockSize - 1) / blockSize;

            // ── Full blocks  P_1 .. P_{m-1}  (the last full block is handled below) ──────────────
            for (int blockIdx = 1; blockIdx <= m - 1; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;

                // Offset_i = Offset_{i-1} xor L_{ntz(i)}
                Xor(offset, this.lArray[Ntz(blockIdx)], offset);

                // C_i = ENCIPHER(K, P_i xor Offset_i) xor Offset_i
                plaintext.Slice(src, blockSize).CopyTo(block);
                Xor(block, offset, block);                // block = P_i xor Offset_i
                this.cipher.Encrypt(block, block);        // block = ENCIPHER(K, P_i xor Offset_i)
                Xor(block, offset, block);                // block = C_i
                block.CopyTo(output.Slice(src));

                // Checksum_i = Checksum_{i-1} xor P_i
                Xor(checksum, plaintext.Slice(src, blockSize), checksum);
            }

            // ── Last block or partial block ────────────────────────────────────────────────────────
            if (plaintext.Length > 0)
            {
                int lastSrc = (m - 1) * blockSize;
                int lastLen = plaintext.Length - lastSrc;

                if (lastLen == blockSize)
                {
                    // Full last block P_m — same loop body as above but using ntz(m).
                    // Offset_m = Offset_{m-1} xor L_{ntz(m)}
                    Xor(offset, this.lArray[Ntz(m)], offset);

                    // C_m = ENCIPHER(K, P_m xor Offset_m) xor Offset_m
                    plaintext.Slice(lastSrc, blockSize).CopyTo(block);
                    Xor(block, offset, block);
                    this.cipher.Encrypt(block, block);
                    Xor(block, offset, block);
                    block.CopyTo(output.Slice(lastSrc));

                    // Checksum_m = Checksum_{m-1} xor P_m
                    Xor(checksum, plaintext.Slice(lastSrc, blockSize), checksum);
                }
                else
                {
                    // Partial last block P_* (bitlen(P_*) > 0).
                    // Offset_* = Offset_m xor L_*
                    Xor(offset, this.lStar, offset);

                    // Pad = ENCIPHER(K, Offset_*)
                    byte[] pad = new byte[blockSize];
                    this.cipher.Encrypt(offset, pad);

                    // C_* = P_* xor Pad[1..bitlen(P_*)]
                    for (int i = 0; i < lastLen; i++)
                        output[lastSrc + i] = (byte)(plaintext[lastSrc + i] ^ pad[i]);

                    // Checksum_* = Checksum_m xor (P_* || 1 || zeros(127-bitlen(P_*)))
                    byte[] padBlock = new byte[blockSize];
                    plaintext.Slice(lastSrc, lastLen).CopyTo(padBlock);
                    padBlock[lastLen] = 0x80;             // append the mandatory 1-bit
                    Xor(checksum, padBlock, checksum);
                }
            }
            // When P is empty: Offset_m = Offset_0 and Checksum_m = zeros(128) — no adjustment needed.

            // ── Authentication tag ─────────────────────────────────────────────────────────────────
            // T = ENCIPHER(K, L_$ xor Offset_m xor Checksum_m) xor HASH(K, A)
            byte[] tagInput = new byte[blockSize];
            Xor(this.lDollar, offset, tagInput);          // L_$ xor Offset_m
            Xor(tagInput, checksum, tagInput);             // xor Checksum_m
            this.cipher.Encrypt(tagInput, tagInput);       // ENCIPHER(K, …)
            byte[] hashResult = ComputeHash(this.aad.AsSpan()); // HASH(K, A)
            Xor(tagInput, hashResult, tagInput);           // xor HASH(K, A) → T

            // Output: C || T
            tagInput.AsSpan(0, this.tagLen).CopyTo(output.Slice(plaintext.Length));

            return required;
        }

        // ── RFC 7253 §2.6  OCB-DECRYPT ────────────────────────────────────────────────────────────
        //
        // Input:  K (key), N (nonce), A (associated data), C (ciphertext), T' (received tag)
        // Output: P (plaintext)  or  FAIL
        //
        // Pseudocode (verbatim from RFC 7253 §2.6):
        //
        //   Offset_0  = ENCIPHER(K, Nonce) as in §2.4             -- computed by ComputeInitialOffset()
        //   Checksum_0 = zeros(128)
        //
        //   for i = 1 to m                                        -- m full 128-bit blocks
        //     Offset_i   = Offset_{i-1} xor L_{ntz(i)}
        //     P_i        = DECIPHER(K, C_i xor Offset_i) xor Offset_i
        //     Checksum_i = Checksum_{i-1} xor P_i
        //   end for
        //
        //   if bitlen(C_*) > 0 then                              -- partial last block
        //     Offset_* = Offset_m xor L_*
        //     Pad      = ENCIPHER(K, Offset_*)                   -- keystream; always ENCIPHER
        //     P_*      = C_* xor Pad[1..bitlen(C_*)]
        //     Checksum_* = Checksum_m xor (P_* || 1 || zeros(127-bitlen(P_*)))
        //     T = ENCIPHER(K, L_$ xor Offset_* xor Checksum_*) xor HASH(K, A)
        //   else                                                  -- block-aligned (or empty) ciphertext
        //     T = ENCIPHER(K, L_$ xor Offset_m xor Checksum_m) xor HASH(K, A)
        //   end if
        //
        //   if T = T' then return P = (P_1 || ... || P_m || P_*)
        //   else        return FAIL
        // ──────────────────────────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
        {
            if (ciphertextWithTag.Length < TagSize)
                throw new ArgumentException($"Input must be at least {TagSize} bytes.", nameof(ciphertextWithTag));
            int plaintextLength = ciphertextWithTag.Length - TagSize;
            if (output.Length < plaintextLength)
                throw new ArgumentException($"Output must be at least {plaintextLength} bytes.", nameof(output));
            EnsureAadProcessed();

            // Split input into C (ciphertext bytes) and T' (received tag).
            ReadOnlySpan<byte> ciphertext = ciphertextWithTag.Slice(0, plaintextLength);
            ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength);

            int blockSize = this.cipher.BlockSize;

            // Offset_0  (§2.4 nonce processing — see ComputeInitialOffset)
            byte[] offset = ComputeInitialOffset();

            // Checksum_0 = zeros(128)
            byte[] checksum = new byte[blockSize];
            byte[] block = new byte[blockSize];

            // Partition C into (C_1, ..., C_m, C_*).
            int m = (plaintextLength + blockSize - 1) / blockSize;

            // ── Full blocks  C_1 .. C_{m-1} ───────────────────────────────────────────────────────
            for (int blockIdx = 1; blockIdx <= m - 1; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;

                // Offset_i = Offset_{i-1} xor L_{ntz(i)}
                Xor(offset, this.lArray[Ntz(blockIdx)], offset);

                // P_i = DECIPHER(K, C_i xor Offset_i) xor Offset_i
                ciphertext.Slice(src, blockSize).CopyTo(block);
                Xor(block, offset, block);                // block = C_i xor Offset_i
                this.cipher.Decrypt(block, block);        // block = DECIPHER(K, C_i xor Offset_i)
                Xor(block, offset, block);                // block = P_i
                block.CopyTo(output.Slice(src));

                // Checksum_i = Checksum_{i-1} xor P_i
                Xor(checksum, output.Slice(src, blockSize), checksum);
            }

            // ── Last block or partial block ────────────────────────────────────────────────────────
            if (plaintextLength > 0)
            {
                int lastSrc = (m - 1) * blockSize;
                int lastLen = plaintextLength - lastSrc;

                if (lastLen == blockSize)
                {
                    // Full last block C_m.
                    // Offset_m = Offset_{m-1} xor L_{ntz(m)}
                    Xor(offset, this.lArray[Ntz(m)], offset);

                    // P_m = DECIPHER(K, C_m xor Offset_m) xor Offset_m
                    ciphertext.Slice(lastSrc, blockSize).CopyTo(block);
                    Xor(block, offset, block);
                    this.cipher.Decrypt(block, block);
                    Xor(block, offset, block);
                    block.CopyTo(output.Slice(lastSrc));

                    // Checksum_m = Checksum_{m-1} xor P_m
                    Xor(checksum, output.Slice(lastSrc, blockSize), checksum);
                }
                else
                {
                    // Partial last block C_* (bitlen(C_*) > 0).
                    // Offset_* = Offset_m xor L_*
                    Xor(offset, this.lStar, offset);

                    // Pad = ENCIPHER(K, Offset_*)  — always ENCIPHER even during decryption
                    byte[] pad = new byte[blockSize];
                    this.cipher.Encrypt(offset, pad);

                    // P_* = C_* xor Pad[1..bitlen(C_*)]
                    for (int i = 0; i < lastLen; i++)
                        output[lastSrc + i] = (byte)(ciphertext[lastSrc + i] ^ pad[i]);

                    // Checksum_* = Checksum_m xor (P_* || 1 || zeros(127-bitlen(P_*)))
                    byte[] padBlock = new byte[blockSize];
                    output.Slice(lastSrc, lastLen).CopyTo(padBlock);
                    padBlock[lastLen] = 0x80;             // append the mandatory 1-bit
                    Xor(checksum, padBlock, checksum);
                }
            }
            // When C is empty: Offset_m = Offset_0 and Checksum_m = zeros(128) — no adjustment needed.

            // ── Recompute tag and verify ───────────────────────────────────────────────────────────
            // T = ENCIPHER(K, L_$ xor Offset_m xor Checksum_m) xor HASH(K, A)
            byte[] tagInput = new byte[blockSize];
            Xor(this.lDollar, offset, tagInput);          // L_$ xor Offset_m
            Xor(tagInput, checksum, tagInput);             // xor Checksum_m
            this.cipher.Encrypt(tagInput, tagInput);       // ENCIPHER(K, …)
            byte[] hashResult = ComputeHash(this.aad.AsSpan()); // HASH(K, A)
            Xor(tagInput, hashResult, tagInput);           // xor HASH(K, A) → T

            // if T = T' then return P, else return FAIL
            if (!CryptographicOperations.FixedTimeEquals(tagInput.AsSpan(0, this.tagLen), receivedTag))
            {
                CryptographicOperations.ZeroMemory(output.Slice(0, plaintextLength));
                throw new CryptographicException("OCB authentication tag verification failed.");
            }
            return plaintextLength;
        }

        // ── Private helpers ────────────────────────────────────────────────────────────────────────

        private void EnsureAadProcessed()
        {
            if (!this.aadProcessed) { this.aad = Array.Empty<byte>(); this.aadProcessed = true; }
        }

        // ── RFC 7253 §2.4  Nonce Processing ───────────────────────────────────────────────────────
        //
        // Pseudocode (RFC 7253 §2.4):
        //
        //   Nonce   = num2str(TAGLEN mod 128, 7) || zeros(120-bitlen(N)) || 1 || N
        //   bottom  = str2num(Nonce[123..128])             -- low 6 bits of the last nonce byte
        //   Ktop    = ENCIPHER(K, Nonce[1..122] || zeros(6))
        //   Stretch = Ktop || (Ktop[1..64] xor Ktop[9..72])
        //   Offset_0 = Stretch[1+bottom..128+bottom]
        // ──────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the initial offset Offset_0 using the RFC 7253 §2.4 K_top stretch.
        /// Supports a 12-byte (96-bit) nonce with TAGLEN = 128.
        /// </summary>
        private byte[] ComputeInitialOffset()
        {
            int blockSize = this.cipher.BlockSize;

            // Nonce = num2str(TAGLEN mod 128, 7) || zeros(120-96) || 1 || N
            // RFC 7253 §2.4: the first 7 bits encode (TAGLEN mod 128) in big-endian order,
            // followed by zeros and the mandatory '1' separator at bit 32, then N[0..11].
            //
            // For TAGLEN=128: num2str(0,  7) = 0000000 → byte0 = 0x00
            // For TAGLEN=96:  num2str(96, 7) = 1100000 → byte0 = 0xC0
            // For TAGLEN=64:  num2str(64, 7) = 1000000 → byte0 = 0x80
            //
            // The 7-bit value occupies the MSBits of byte 0 (bits 1–7 in 1-based notation),
            // with bit 8 being the first zero of the zeros(24) run — so we shift left by 1.
            byte[] nonceWord = new byte[blockSize];
            nonceWord[0] = (byte)((this.tagLen * 8 % 128) << 1); // num2str(TAGLEN mod 128, 7) → byte0
            nonceWord[3] = 0x01;                                  // the mandatory '1' separator bit
            this.nonce.CopyTo(nonceWord, 4);

            // bottom = str2num(Nonce[123..128])  — low 6 bits of byte 15
            int bottom = nonceWord[blockSize - 1] & 0x3F;

            // Ktop = ENCIPHER(K, Nonce[1..122] || zeros(6))  — clear the bottom 6 bits
            byte[] ktopInput = (byte[])nonceWord.Clone();
            ktopInput[blockSize - 1] &= 0xC0;
            byte[] ktop = new byte[blockSize];
            this.cipher.Encrypt(ktopInput, ktop);

            // Stretch = Ktop || (Ktop[1..64] xor Ktop[9..72])
            // Ktop[1..64] = bytes 0-7;  Ktop[9..72] = bytes 1-8  →  adjacent-byte XOR.
            byte[] stretch = new byte[blockSize + blockSize / 2];
            ktop.CopyTo(stretch, 0);
            for (int i = 0; i < blockSize / 2; i++)
                stretch[blockSize + i] = (byte)(ktop[i] ^ ktop[i + 1]);

            // Offset_0 = Stretch[1+bottom..128+bottom]  — extract a 128-bit window at bit `bottom`.
            byte[] offset = new byte[blockSize];
            int byteOffset = bottom / 8;
            int bitOffset = bottom % 8;
            if (bitOffset == 0)
            {
                // Window falls on a byte boundary — simple copy.
                stretch.AsSpan(byteOffset, blockSize).CopyTo(offset);
            }
            else
            {
                // Window straddles byte boundaries — shift each output byte from two adjacent input bytes.
                for (int i = 0; i < blockSize; i++)
                    offset[i] = (byte)((stretch[byteOffset + i] << bitOffset) |
                                       (stretch[byteOffset + i + 1] >> (8 - bitOffset)));
            }
            return offset;
        }

        // ── RFC 7253 §4  HASH(K, A) ───────────────────────────────────────────────────────────────
        //
        // Pseudocode (RFC 7253 §4):
        //
        //   (A_1, ..., A_m, A_*) = partition(A, 128)
        //   Offset_0 = zeros(128)
        //   Sum_0    = zeros(128)
        //
        //   for i = 1 to m
        //     Offset_i = Offset_{i-1} xor L_{ntz(i)}
        //     Sum_i    = Sum_{i-1} xor ENCIPHER(K, A_i xor Offset_i)
        //   end for
        //
        //   if bitlen(A_*) > 0 then
        //     Offset_* = Offset_m xor L_*
        //     CipherInput = (A_* || 1 || zeros(127-bitlen(A_*))) xor Offset_*
        //     Sum = Sum_m xor ENCIPHER(K, CipherInput)
        //   else
        //     Sum = Sum_m
        //   end if
        //
        //   return Sum
        // ──────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes HASH(K, A) — the OCB3 authentication of associated data per RFC 7253 §4.
        /// </summary>
        private byte[] ComputeHash(ReadOnlySpan<byte> aad)
        {
            int blockSize = this.cipher.BlockSize;

            // Sum_0 = zeros(128)
            byte[] sum = new byte[blockSize];
            if (aad.Length == 0) return sum;    // HASH of empty string is zeros

            // Offset_0 = zeros(128)
            byte[] offsetHash = new byte[blockSize];
            byte[] block = new byte[blockSize];

            // Partition A into (A_1, ..., A_m, A_*). m counts all blocks including any partial last.
            int m = (aad.Length + blockSize - 1) / blockSize;

            for (int blockIdx = 1; blockIdx <= m; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;
                int blockLen = Math.Min(blockSize, aad.Length - src);
                bool full = blockLen == blockSize;

                if (full)
                {
                    // Full block A_i.
                    // Offset_i = Offset_{i-1} xor L_{ntz(i)}
                    Xor(offsetHash, this.lArray[Ntz(blockIdx)], offsetHash);

                    // Sum_i = Sum_{i-1} xor ENCIPHER(K, A_i xor Offset_i)
                    aad.Slice(src, blockSize).CopyTo(block);
                    Xor(block, offsetHash, block);        // A_i xor Offset_i
                    this.cipher.Encrypt(block, block);    // ENCIPHER(K, …)
                    Xor(sum, block, sum);                 // Sum_i
                }
                else
                {
                    // Partial last block A_* (bitlen(A_*) > 0).
                    // Offset_* = Offset_m xor L_*
                    Xor(offsetHash, this.lStar, offsetHash);

                    // CipherInput = (A_* || 1 || zeros(127-bitlen(A_*))) xor Offset_*
                    byte[] padBlock = new byte[blockSize];
                    aad.Slice(src, blockLen).CopyTo(padBlock);
                    padBlock[blockLen] = 0x80;            // append the mandatory 1-bit
                    Xor(padBlock, offsetHash, padBlock);  // xor Offset_*

                    // Sum = Sum_m xor ENCIPHER(K, CipherInput)
                    this.cipher.Encrypt(padBlock, padBlock);
                    Xor(sum, padBlock, sum);
                }
            }
            return sum;
        }

        /// <summary>
        /// Multiplies <paramref name="x" /> by α in GF(2^128) with big-endian bit order and
        /// polynomial x^128 + x^7 + x^2 + x + 1 (reduction via XOR with 0x87 in the LSByte).
        /// This implements the <c>double()</c> function from RFC 7253 §2.1.
        /// </summary>
        private static byte[] GfDouble(byte[] x)
        {
            byte[] result = new byte[x.Length];
            bool msb = (x[0] & 0x80) != 0;               // save the bit that will be shifted out
            for (int i = 0; i < x.Length - 1; i++)
                result[i] = (byte)((x[i] << 1) | (x[i + 1] >> 7)); // left-shift with carry
            result[x.Length - 1] = (byte)(x[x.Length - 1] << 1);
            if (msb) result[x.Length - 1] ^= 0x87;       // reduce mod the field polynomial
            return result;
        }

        private static void Xor(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> result)
        {
            for (int i = 0; i < result.Length; i++) result[i] = (byte)(a[i] ^ b[i]);
        }

        /// <summary>
        /// Returns the number of trailing zero bits (ntz) of <paramref name="n" />.
        /// Used by RFC 7253 to select L[i] for each block index.
        /// </summary>
        private static int Ntz(int n)
        {
            if (n == 0) return 32;
            int count = 0;
            while ((n & 1) == 0) { n >>= 1; count++; }
            return count;
        }
    }
}
