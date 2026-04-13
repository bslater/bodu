namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Identifies the standard block cipher chaining modes used when encrypting or decrypting multi-block messages.
    /// </summary>
    /// <remarks>
    /// Each value selects a different strategy for combining block cipher operations with feedback or sequencing logic. Modes differ in
    /// security properties, parallelism, and whether they require an initialisation vector or nonce. Use
    /// <see cref="BlockCipherModeFactory.Create" /> to obtain an <see cref="IBlockCipherModeTransform" /> for a given value.
    /// </remarks>
    public enum CipherBlockMode
    {
        /// <summary>
        /// Electronic Codebook (ECB) mode. Each block is encrypted independently with no feedback.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ECB is trivially parallelisable but leaks structural information: identical plaintext blocks always produce identical
        /// ciphertext blocks. It is insecure for virtually all real-world messages and should only be used as a primitive inside a
        /// higher-level construction.
        /// </para>
        /// <para>This mode does not require an initialisation vector.</para>
        /// </remarks>
        ECB,

        /// <summary>
        /// Cipher Block Chaining (CBC) mode. Each plaintext block is XORed with the previous ciphertext block before encryption.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CBC provides confidentiality by chaining ciphertext blocks, so identical plaintext blocks produce different ciphertexts
        /// (assuming different IVs). The first block uses an initialization vector (IV) instead of a previous ciphertext block.
        /// </para>
        /// <para>This mode requires an IV of block size length.</para>
        /// </remarks>
        CBC,

        /// <summary>
        /// Cipher Feedback (CFB) mode. Encrypts the previous ciphertext (or IV) to produce a keystream that is XORed with the current
        /// plaintext or ciphertext.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CFB turns a block cipher into a self-synchronizing stream cipher. It supports partial block encryption and can recover from bit
        /// errors after a few blocks. The IV is used to seed the encryption for the first block.
        /// </para>
        /// <para>This mode requires an IV of block size length.</para>
        /// </remarks>
        CFB,

        /// <summary>
        /// Output Feedback (OFB) mode. Encrypts the previous output (or IV) to produce a keystream that is XORed with the plaintext or ciphertext.
        /// </summary>
        /// <remarks>
        /// <para>
        /// OFB is similar to CFB but uses the previous keystream block rather than ciphertext, making it immune to bit-flip propagation. It
        /// operates like a synchronous stream cipher. The IV seeds the initial encryption.
        /// </para>
        /// <para>This mode requires an IV of block size length.</para>
        /// </remarks>
        OFB,

        /// <summary>
        /// Counter (CTR) mode. Encrypts successive counter values to produce a keystream that is XORed with plaintext or ciphertext.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CTR transforms a block cipher into a parallelisable stream cipher with random access. It requires a nonce (or initial counter
        /// value) equal in length to the cipher block size.
        /// </para>
        /// <para>
        /// Reusing a (key, nonce) pair across messages is catastrophic: the XOR of two ciphertexts encrypted with the same keystream
        /// recovers the XOR of the plaintexts. Callers must ensure that every counter value is used at most once per key.
        /// </para>
        /// </remarks>
        CTR
    }
}