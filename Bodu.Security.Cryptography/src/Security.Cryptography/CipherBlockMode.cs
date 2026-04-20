// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CipherBlockMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Identifies the block cipher chaining modes used when encrypting or decrypting multi-block messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <img src="../images/diagrams/classic-modes.svg" alt="ECB, CBC, CFB, OFB, and CTR data-flow panels." />
    /// </para>
    /// <para>
    /// Each value selects a different strategy for combining block cipher operations with feedback or sequencing
    /// logic. The five panels above show the classic, non-authenticated modes:
    /// <list type="number">
    /// <item><description><b>ECB</b> — no feedback. Identical plaintext blocks produce identical ciphertext blocks.</description></item>
    /// <item><description><b>CBC</b> — previous ciphertext block XORed into the next plaintext before encryption; the first block uses the IV.</description></item>
    /// <item><description><b>CFB</b> — encrypted previous ciphertext (or IV) acts as keystream; self-synchronising.</description></item>
    /// <item><description><b>OFB</b> — encrypted previous keystream block feeds forward; plaintext is independent of the keystream chain.</description></item>
    /// <item><description><b>CTR</b> — successive counter values are encrypted to produce an independent, random-access keystream.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Modes differ in security properties, parallelism, and whether they require an initialisation vector
    /// or nonce. The remaining values (<see cref="XTS" />, <see cref="OCB" />, <see cref="EAX" />,
    /// <see cref="SIV" />) extend these building blocks with per-block tweaks or authenticated-encryption tags.
    /// Use <see cref="BlockCipherModeFactory.Create" /> to obtain an
    /// <see cref="IBlockCipherModeTransform" /> for a given value.
    /// </para>
    /// </remarks>
    /// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes (guide with per-mode worked examples)</seealso>
    /// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
    /// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
    public enum CipherBlockMode
    {
        /// <summary>
        /// Electronic Codebook (ECB) mode. Each block is encrypted independently with no feedback.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ECB is trivially parallelisable but leaks structural information: identical plaintext blocks always
        /// produce identical ciphertext blocks. It is insecure for virtually all real-world messages and should
        /// only be used as a primitive inside a higher-level construction.
        /// </para>
        /// <para>This mode does not require an initialisation vector.</para>
        /// </remarks>
        ECB,

        /// <summary>
        /// Cipher Block Chaining (CBC) mode. Each plaintext block is XORed with the previous ciphertext block
        /// before encryption.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CBC provides confidentiality by chaining ciphertext blocks, so identical plaintext blocks produce
        /// different ciphertexts (assuming different IVs). The first block uses an initialisation vector (IV)
        /// instead of a previous ciphertext block.
        /// </para>
        /// <para>
        /// This mode requires an IV equal in length to the cipher block size, which should be unpredictable for
        /// each message.
        /// </para>
        /// </remarks>
        CBC,

        /// <summary>
        /// Cipher Feedback (CFB) mode. Encrypts the previous ciphertext (or IV) to produce a keystream that is
        /// XORed with the current plaintext or ciphertext.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CFB turns a block cipher into a self-synchronising stream cipher. It can recover from bit errors after
        /// a few blocks and uses the cipher's encryption primitive for both encryption and decryption. The IV is
        /// used to seed the feedback register for the first block.
        /// </para>
        /// <para>This mode requires an IV equal in length to the cipher block size.</para>
        /// </remarks>
        CFB,

        /// <summary>
        /// Output Feedback (OFB) mode. Encrypts the previous output (or IV) to produce a keystream that is XORed
        /// with the plaintext or ciphertext.
        /// </summary>
        /// <remarks>
        /// <para>
        /// OFB is similar to CFB but feeds the previous keystream block back into the cipher rather than the
        /// ciphertext, making it immune to bit-flip propagation. It operates like a synchronous stream cipher.
        /// The IV seeds the initial feedback register.
        /// </para>
        /// <para>
        /// This mode requires an IV equal in length to the cipher block size, which must never be reused under
        /// the same key.
        /// </para>
        /// </remarks>
        OFB,

        /// <summary>
        /// Counter (CTR) mode. Encrypts successive counter values to produce a keystream that is XORed with
        /// plaintext or ciphertext.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CTR transforms a block cipher into a parallelisable stream cipher with random access. It requires a
        /// nonce (or initial counter value) equal in length to the cipher block size.
        /// </para>
        /// <para>
        /// Reusing a (key, nonce) pair across messages is catastrophic: the XOR of two ciphertexts encrypted
        /// with the same keystream recovers the XOR of the plaintexts. Callers must ensure that every counter
        /// value is used at most once per key.
        /// </para>
        /// </remarks>
        CTR,

        /// <summary>
        /// XEX-based Tweaked-Codebook mode with Ciphertext Stealing (XTS). Each block is independently
        /// encrypted with a per-block tweak derived from a sector number and updated via GF(2^n) multiplication.
        /// </summary>
        /// <remarks>
        /// <para>
        /// XTS is the standard mode for storage and disk encryption (IEEE 1619-2007). The tweak evolves
        /// deterministically across consecutive blocks in a sector:
        /// <list type="bullet">
        /// <item><description>Encrypt: C_i = E(P_i ⊕ T_i) ⊕ T_i</description></item>
        /// <item><description>Decrypt: P_i = D(C_i ⊕ T_i) ⊕ T_i</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// XTS requires two independent key streams (typically expressed as a doubled key) and an IV equal to
        /// the cipher block size representing the sector number. The decrypt direction uses the cipher's
        /// <em>decryption</em> primitive, unlike CFB/OFB/CTR.
        /// </para>
        /// </remarks>
        XTS,

        /// <summary>
        /// Offset Codebook mode 3 (OCB3 / RFC 7253). A single-pass authenticated encryption mode that processes
        /// each block with a ntz-derived offset, producing ciphertext and an integrity tag simultaneously.
        /// </summary>
        /// <remarks>
        /// <para>
        /// OCB derives a sequence of offsets from a nonce and pre-computed multiples of E(0...0):
        /// <list type="bullet">
        /// <item><description>Encrypt: C_i = E(P_i ⊕ Δ_i) ⊕ Δ_i</description></item>
        /// <item><description>Decrypt: P_i = D(C_i ⊕ Δ_i) ⊕ Δ_i</description></item>
        /// </list>
        /// where Δ_i = Δ_{i−1} ⊕ L[ntz(i)] and ntz(i) is the number of trailing zeros of i.
        /// </para>
        /// <para>
        /// The <see cref="OcbModeTransform" /> implementation provides the encryption/decryption transform
        /// component. Full AEAD authentication (associated data processing and tag generation/verification)
        /// requires the <c>IAeadBlockCipherModeTransform</c> interface extension.
        /// </para>
        /// </remarks>
        OCB,

        /// <summary>
        /// EAX mode (Bellare, Rogaway, Wagner). A two-pass authenticated encryption mode combining CTR
        /// encryption with OMAC-based authentication of the nonce, ciphertext, and optional associated data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// EAX encryption applies CTR mode using OMAC(nonce) as the counter start:
        /// <list type="bullet">
        /// <item><description>Keystream_i = E(counter_i), where counter increments each block.</description></item>
        /// <item><description>Both encrypt and decrypt XOR the input with the keystream (CTR property).</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The <see cref="EaxModeTransform" /> implementation provides the CTR encryption component using the
        /// nonce directly as the initial counter. Full AEAD authentication (OMAC nonce processing, OMAC
        /// ciphertext tag, and associated data) requires the <c>IAeadBlockCipherModeTransform</c> interface
        /// extension.
        /// </para>
        /// </remarks>
        EAX,

        /// <summary>
        /// Synthetic IV (SIV / RFC 5297). A misuse-resistant authenticated encryption mode that derives a
        /// deterministic IV from the plaintext and associated data via S2V, then applies CTR encryption seeded
        /// by that IV.
        /// </summary>
        /// <remarks>
        /// <para>
        /// SIV's determinism makes it resistant to nonce reuse: even if the same message is encrypted twice,
        /// the ciphertext is identical but no confidentiality is lost beyond confirming message equality. It is
        /// widely used for key wrapping and contexts where generating a unique nonce is unreliable.
        /// </para>
        /// <para>
        /// The CTR counter is initialised from the SIV with bits 31 and 63 cleared to prevent counter wrap.
        /// Both encrypt and decrypt use only the cipher's encryption primitive (CTR property).
        /// </para>
        /// <para>
        /// The <see cref="SivModeTransform" /> implementation accepts the pre-computed SIV directly as the IV.
        /// Full AEAD authentication (S2V computation from plaintext and associated data) requires the
        /// <c>IAeadBlockCipherModeTransform</c> interface extension.
        /// </para>
        /// </remarks>
        SIV,
    }
}
