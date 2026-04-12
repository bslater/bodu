namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Specifies the standard block cipher chaining modes used to apply encryption to multi-block data.
    /// </summary>
    /// <remarks>
    /// Each cipher mode defines a different strategy for combining block cipher operations with feedback or sequencing logic. Modes differ
    /// in security characteristics, performance trade-offs, and requirements such as initialization vectors.
    /// </remarks>
    public enum CipherBlockMode
    {
        /// <summary>
        /// Electronic Codebook (ECB) mode. Each block is encrypted independently without feedback.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ECB is the simplest mode and allows parallel encryption/decryption of blocks. However, it is insecure for most applications
        /// because identical plaintext blocks yield identical ciphertext blocks, revealing patterns in the data.
        /// </para>
        /// <para>This mode does not require an initialization vector (IV).</para>
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
        /// Counter (CTR) mode. Uses a counter value that is encrypted to produce a keystream, which is XORed with plaintext or ciphertext.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CTR transforms a block cipher into a stream cipher by encrypting a monotonically incremented counter (usually seeded with an
        /// IV). It allows parallel encryption and decryption and is suitable for high-performance applications.
        /// </para>
        /// <para>This mode requires a nonce or IV of block size length to initialize the counter.</para>
        /// </remarks>
        CTR
    }
}