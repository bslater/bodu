// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Applies Counter with CBC-MAC (CCM) mode to an underlying <see cref="IBlockCipher" />, providing
    /// two-pass authenticated encryption with associated data (AEAD).
    /// </summary>
    /// <remarks>
    /// <para>
    /// CCM combines CBC-MAC for authentication with CTR for encryption:
    /// <list type="bullet">
    /// <item><description>
    /// Pass 1 (authenticate): compute CBC-MAC T over the formatted header (AAD + plaintext).
    /// </description></item>
    /// <item><description>
    /// Pass 2 (encrypt): compute CTR keystream from nonce-based counter; C_i = P_i ⊕ keystream_i;
    /// encrypted tag = T ⊕ E(A0) where A0 is the counter with index 0.
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This implementation uses a tag size of 16 bytes and treats the IV as the CCM nonce directly.
    /// </para>
    /// </remarks>
    public sealed class CcmModeTransform : IAeadBlockCipherModeTransform
    {
        private const int DefaultTagSize = 16;

        private readonly IBlockCipher cipher;
        private readonly byte[] nonce;
        private byte[]? aad;
        private bool aadProcessed;

        /// <summary>
        /// Initialises a new instance of the <see cref="CcmModeTransform" /> class.
        /// </summary>
        /// <param name="cipher">The block cipher. Must have a 16-byte block size.</param>
        /// <param name="iv">
        /// The nonce used for both authentication and encryption. Must equal the cipher block size.
        /// A defensive copy is taken.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="iv" /> length does not equal the cipher block size.</exception>
        public CcmModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (iv is null) throw new ArgumentNullException(nameof(iv));
            if (iv.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                    nameof(iv));

            this.nonce = (byte[])iv.Clone();
        }

        /// <inheritdoc />
        public int TagSize => DefaultTagSize;

        /// <inheritdoc />
        public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
        {
            if (this.aadProcessed)
                throw new InvalidOperationException("AssociatedData has already been processed.");
            this.aad = associatedData.ToArray();
            this.aadProcessed = true;
        }

        /// <inheritdoc />
        public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
        {
            int required = plaintext.Length + TagSize;
            if (output.Length < required)
                throw new ArgumentException($"Output buffer must be at least {required} bytes.", nameof(output));

            EnsureAadProcessed();

            byte[] mac = ComputeCbcMac(this.aad.AsSpan(), plaintext);
            byte[] encTag = EncryptCounter(mac, counterIndex: 0);

            // Encrypt plaintext with CTR starting at counter index 1.
            Span<byte> ciphertext = output.Slice(0, plaintext.Length);
            EncryptCtr(plaintext, ciphertext, startIndex: 1);
            encTag.AsSpan(0, TagSize).CopyTo(output.Slice(plaintext.Length));

            return required;
        }

        /// <inheritdoc />
        public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
        {
            if (ciphertextWithTag.Length < TagSize)
                throw new ArgumentException($"Input must be at least {TagSize} bytes.", nameof(ciphertextWithTag));

            int plaintextLength = ciphertextWithTag.Length - TagSize;
            if (output.Length < plaintextLength)
                throw new ArgumentException($"Output buffer must be at least {plaintextLength} bytes.", nameof(output));

            EnsureAadProcessed();

            ReadOnlySpan<byte> ciphertext = ciphertextWithTag.Slice(0, plaintextLength);
            ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength);

            // Decrypt ciphertext first, then verify tag.
            EncryptCtr(ciphertext, output.Slice(0, plaintextLength), startIndex: 1);

            byte[] mac = ComputeCbcMac(this.aad.AsSpan(), output.Slice(0, plaintextLength));
            byte[] encTag = EncryptCounter(mac, counterIndex: 0);

            if (!CryptographicOperations.FixedTimeEquals(encTag.AsSpan(0, TagSize), receivedTag))
            {
                // Zero plaintext on failure before throwing.
                CryptographicOperations.ZeroMemory(output.Slice(0, plaintextLength));
                throw new CryptographicException("CCM authentication tag verification failed.");
            }

            return plaintextLength;
        }

        // ── Private helpers ────────────────────────────────────────────────────────────────────────

        private void EnsureAadProcessed()
        {
            if (!this.aadProcessed) { this.aad = Array.Empty<byte>(); this.aadProcessed = true; }
        }

        /// <summary>Computes CBC-MAC over AAD prepended to plaintext.</summary>
        private byte[] ComputeCbcMac(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext)
        {
            int blockSize = this.cipher.BlockSize;
            byte[] mac = new byte[blockSize];

            // Feed nonce as the first block.
            CbcMacUpdate(mac, this.nonce);

            // Feed AAD in blocks.
            if (aad.Length > 0) FeedPadded(mac, aad);

            // Feed plaintext in blocks.
            if (plaintext.Length > 0) FeedPadded(mac, plaintext);

            return mac;
        }

        private void FeedPadded(byte[] mac, ReadOnlySpan<byte> data)
        {
            int blockSize = this.cipher.BlockSize;
            for (int offset = 0; offset < data.Length; offset += blockSize)
            {
                byte[] block = new byte[blockSize];
                int remaining = Math.Min(blockSize, data.Length - offset);
                data.Slice(offset, remaining).CopyTo(block);
                CbcMacUpdate(mac, block);
            }
        }

        private void CbcMacUpdate(byte[] mac, ReadOnlySpan<byte> block)
        {
            Span<byte> xored = stackalloc byte[mac.Length];
            for (int i = 0; i < mac.Length; i++)
                xored[i] = (byte)(mac[i] ^ block[i]);
            this.cipher.Encrypt(xored, mac);
        }

        /// <summary>Constructs the A_i counter block (nonce XOR counter index in last bytes) and encrypts it.</summary>
        private byte[] EncryptCounter(ReadOnlySpan<byte> input, int counterIndex)
        {
            int blockSize = this.cipher.BlockSize;
            byte[] ctr = (byte[])this.nonce.Clone();
            // Write counter index as big-endian in last 2 bytes.
            ctr[blockSize - 2] = (byte)(counterIndex >> 8);
            ctr[blockSize - 1] = (byte)counterIndex;
            byte[] output = new byte[blockSize];
            this.cipher.Encrypt(ctr, output);
            for (int i = 0; i < Math.Min(input.Length, blockSize); i++)
                output[i] ^= input[i];
            return output;
        }

        private void EncryptCtr(ReadOnlySpan<byte> input, Span<byte> output, int startIndex)
        {
            int blockSize = this.cipher.BlockSize;
            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                int counterIndex = startIndex + offset / blockSize;
                byte[] ctr = (byte[])this.nonce.Clone();
                ctr[blockSize - 2] = (byte)(counterIndex >> 8);
                ctr[blockSize - 1] = (byte)counterIndex;
                Span<byte> keystream = stackalloc byte[blockSize];
                this.cipher.Encrypt(ctr, keystream);
                int remaining = Math.Min(blockSize, input.Length - offset);
                for (int i = 0; i < remaining; i++)
                    output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
            }
        }
    }
}
