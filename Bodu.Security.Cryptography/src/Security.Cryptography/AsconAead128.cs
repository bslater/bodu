// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides authenticated encryption with associated data (AEAD) using the <c>Ascon-AEAD128</c> algorithm as defined in
/// NIST SP 800-232. Accepts a 128-bit key and a 128-bit nonce and produces a 128-bit authentication tag. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Ascon-AEAD128 is a permutation-based AEAD scheme built on the 320-bit ASCON sponge. It uses a 128-bit (16-byte)
/// rate, Ascon-p12 for initialization and finalization, and Ascon-p8 during associated-data and ciphertext processing.
/// </para>
/// <para>
/// The algorithm proceeds through four phases:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Initialization</b>: the state is loaded as <c>[IV ‖ K₀ ‖ K₁ ‖ N₀ ‖ N₁]</c>, Ascon-p12 is applied, and the key is
/// XORed into state words 3 and 4.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Associated-data processing</b>: each 16-byte block is absorbed with Ascon-p8 between blocks. A domain-separation
/// constant (XOR of 1 into state word 4) is always applied after the AD phase, even when AD is empty.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Encryption / decryption</b>: plaintext or ciphertext is processed in 16-byte blocks, with Ascon-p8 applied
/// between blocks. The final (possibly partial) block is absorbed without a trailing permutation.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Finalization</b>: the key is injected into the state, Ascon-p12 is applied, and the 128-bit tag is extracted by
/// XORing the key into state words 3 and 4.
/// </description>
/// </item>
/// </list>
/// <para>
/// Each instance is single-use. A new instance must be created for every message. Reusing a nonce under the same key
/// completely breaks confidentiality and authenticity.
/// </para>
/// <para>
/// Call <see cref="ProcessAssociatedData" /> before <see cref="Encrypt" /> or <see cref="Decrypt" />. Pass an empty
/// span if there is no associated data.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Key size: 128 bits (16 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Nonce size: 128 bits (16 bytes), must be unique per key.
/// </description>
/// </item>
/// <item>
/// <description>
/// Tag size: 128 bits (16 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// State: 320-bit sponge; rate: 16 bytes; permutation Ascon-p12 (init/finalize) + Ascon-p8 (absorb).
/// </description>
/// </item>
/// <item>
/// <description>
/// Specification: NIST SP 800-232 (ASCON family).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Ascon-AEAD128.</strong> The right pick when NIST's lightweight-cryptography selection is
/// required, or for resource-constrained targets (microcontrollers, IoT) where the Ascon permutation's small state and
/// short round count are an advantage over GCM's GHASH multiplications. For general-purpose AEAD on commodity x86/ARM
/// hardware <see cref="GcmModeTransform" /> remains faster thanks to AES-NI/PCLMULQDQ; for nonce-misuse resistance
/// prefer <see cref="GcmSivModeTransform" /> or <see cref="SivModeTransform" />. Unlike the AES-based AEAD modes, Ascon
/// does not depend on a separate block cipher — pair it with the related <see cref="AsconHash256" /> /
/// <see cref="AsconHashA256" /> hashes or <see cref="AsconXof128" /> XOF when building a fully Ascon-based protocol.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp"> using Bodu.Security.Cryptography; using Bodu.Security.Cryptography.Extensions; // Most
/// callers should reach for the AeadBlockCipherModeTransformExtensions helpers, // which size the output buffer and
/// return a single freshly allocated array. using IAeadBlockCipherModeTransform enc = new AsconAead128(key, nonce);
/// byte[] sealed_ = enc.Encrypt(plaintext, associatedData: header); using IAeadBlockCipherModeTransform dec = new
/// AsconAead128(key, nonce); byte[] recovered = dec.Decrypt(sealed_, associatedData: header); </code>
/// </example>
/// <seealso href="https://doi.org/10.6028/NIST.SP.800-232">NIST SP 800-232 (ASCON)</seealso>
/// <seealso cref="IAeadBlockCipherModeTransform"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public sealed class AsconAead128
    : IAeadBlockCipherModeTransform, IDisposable
{
    /// <summary>
    /// Length of the Ascon-AEAD128 key is 128 bits (16 bytes).
    /// </summary>
    public const int KeySize = 128;

    /// <summary>
    /// Length of the Ascon-AEAD128 nonce is 128 bits (16 bytes).
    /// </summary>
    public const int NonceSize = 128;

    /// <summary>
    /// Length of the Ascon-AEAD128 authentication tag is 128 bits (16 bytes).
    /// </summary>
    internal const int TagSizeBits = 128;

    /// <summary>
    /// Length of the Ascon-AEAD128 sponge absorption rate is 128 bits (16 bytes).
    /// </summary>
    private const int RateSizeBits = 128;

    /// <summary>
    /// Byte length of <see cref="KeySize" /> (16 bytes). Derived helper used for span-length checks.
    /// </summary>
    internal const int KeyBytes = KeySize / 8;

    /// <summary>
    /// Byte length of <see cref="NonceSize" /> (16 bytes). Derived helper used for span-length checks.
    /// </summary>
    internal const int NonceBytes = NonceSize / 8;

    /// <summary>
    /// Byte length of <see cref="TagSizeBits" /> (16 bytes). Derived helper used for span sizing.
    /// </summary>
    internal const int TagBytes = TagSizeBits / 8;

    /// <summary>
    /// Byte length of <see cref="RateSizeBits" /> (16 bytes). Derived helper used for block iteration over the sponge
    /// rate.
    /// </summary>
    private const int Rate = RateSizeBits / 8;

    // IV word for Ascon-AEAD128 (NIST SP 800-232).
    // Encodes algorithm parameters: key=128 bits, rate=128 bits, pa=12, pb=8.
    // Source: NIST SP 800-232 / ascon-c constants.h (ASCON_AEAD128_IV).
    private const ulong IvWord = 0x80800c0800000000UL;

    private const int Pa = 12;
    private const int Pb = 8;

    private readonly KeyMaterial128 _key;
    private AsconState _state;

    private bool _aadProcessed;
    private bool _completed;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconAead128" /> class with the specified key and nonce.
    /// </summary>
    /// <param name="key">
    /// The 128-bit (16-byte) secret key. The key is read during construction and retained internally as key words until
    /// the instance is disposed. Must not be <see langword="null" />.
    /// </param>
    /// <param name="nonce">
    /// The 128-bit (16-byte) nonce. Must be unique for every message encrypted under the same key. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="nonce" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not exactly <see cref="KeyBytes" /> bytes, or <paramref name="nonce" /> is not
    /// exactly <see cref="NonceBytes" /> bytes.
    /// </exception>
    public AsconAead128(byte[] key, byte[] nonce)
        : this(
            key is null ? throw new ArgumentNullException(nameof(key)) : key.AsSpan(),
            nonce is null ? throw new ArgumentNullException(nameof(nonce)) : nonce.AsSpan())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsconAead128" /> class with the specified key and nonce spans.
    /// </summary>
    /// <param name="key">The 128-bit (16-byte) secret key.</param>
    /// <param name="nonce">
    /// The 128-bit (16-byte) nonce. Must be unique for every message encrypted under the same key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not exactly <see cref="KeyBytes" /> bytes, or <paramref name="nonce" /> is not
    /// exactly <see cref="NonceBytes" /> bytes.
    /// </exception>
    public AsconAead128(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(key, KeyBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(nonce, NonceBytes);

        this._key = new KeyMaterial128(key);

        var k0 = this._key.K0;
        var k1 = this._key.K1;

        // Initialization: [IV || K0 || K1 || N0 || N1] → p12 → S3 ^= K0, S4 ^= K1.
        this._state = new AsconState
        {
            S0 = IvWord,
            S1 = k0,
            S2 = k1,
            S3 = BinaryPrimitives.ReadUInt64LittleEndian(nonce),
            S4 = BinaryPrimitives.ReadUInt64LittleEndian(nonce[8..]),
        };

        this._state.Permute(Pa);
        this._state.S3 ^= k0;
        this._state.S4 ^= k1;
    }

    /// <inheritdoc />
    /// <value>Length of the Ascon-AEAD128 authentication tag is 128 bits (16 bytes).</value>
    public int TagSize => TagSizeBits;

    /// <summary>
    /// Absorbs associated data that will be authenticated but not encrypted.
    /// </summary>
    /// <param name="associatedData">
    /// The bytes to authenticate. May be empty to indicate no associated data. Must be called exactly once before
    /// <see cref="Encrypt" /> or <see cref="Decrypt" />.
    /// </param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">This method has already been called on this instance.</exception>
    public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();

        if (this._aadProcessed)
            throw new InvalidOperationException(CryptoResourceStrings.Crypt_Invalid_AssociatedDataAlreadyProcessed);

        if (!associatedData.IsEmpty)
        {
            var offset = 0;

            while (offset + Rate <= associatedData.Length)
            {
                this._state.AbsorbRate128(associatedData.Slice(offset, Rate));
                this._state.Permute(Pb);
                offset += Rate;
            }

            // Final partial block: Ascon padding — 0x01 at the first unused byte position.
            Span<byte> pad = stackalloc byte[Rate];
            var rem = associatedData.Length - offset;
            associatedData.Slice(offset, rem).CopyTo(pad);
            pad[rem] ^= 0x01;
            this._state.AbsorbRate128(pad);
            this._state.Permute(Pb);
        }

        // Domain separation: always applied after AD, even when AD is empty.
        this._state.S4 ^= 1UL;
        this._aadProcessed = true;
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> and appends the 16-byte authentication tag to <paramref name="output" />.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">
    /// Receives the ciphertext followed immediately by the 16-byte tag. Must be at least
    /// <c>plaintext.Length + <see cref="TagBytes"/></c> bytes long.
    /// </param>
    /// <returns>Total bytes written: <c>plaintext.Length + <see cref="TagBytes"/></c>.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="ProcessAssociatedData" /> has not been called.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="output" /> is too small.</exception>
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();
        this.ThrowIfAadNotProcessed();

        var required = plaintext.Length + TagBytes;
        if (output.Length < required)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_OutputBufferTooSmall, required),
                nameof(output));

        try
        {
            var inOff = 0;
            var outOff = 0;

            while (inOff + Rate <= plaintext.Length)
            {
                var p0 = BinaryPrimitives.ReadUInt64LittleEndian(plaintext[inOff..]);
                var p1 = BinaryPrimitives.ReadUInt64LittleEndian(plaintext[(inOff + 8)..]);

                BinaryPrimitives.WriteUInt64LittleEndian(output[outOff..], this._state.S0 ^ p0);
                BinaryPrimitives.WriteUInt64LittleEndian(output[(outOff + 8)..], this._state.S1 ^ p1);

                this._state.S0 ^= p0;
                this._state.S1 ^= p1;
                this._state.Permute(Pb);

                inOff += Rate;
                outOff += Rate;
            }

            var lastLen = plaintext.Length - inOff;

            Span<byte> padded = stackalloc byte[Rate];
            plaintext.Slice(inOff, lastLen).CopyTo(padded);
            padded[lastLen] ^= 0x01;

            var fp0 = BinaryPrimitives.ReadUInt64LittleEndian(padded);
            var fp1 = BinaryPrimitives.ReadUInt64LittleEndian(padded[8..]);

            Span<byte> cOut = stackalloc byte[Rate];
            BinaryPrimitives.WriteUInt64LittleEndian(cOut, this._state.S0 ^ fp0);
            BinaryPrimitives.WriteUInt64LittleEndian(cOut[8..], this._state.S1 ^ fp1);

            cOut[..lastLen].CopyTo(output[outOff..]);

            this._state.S0 ^= fp0;
            this._state.S1 ^= fp1;

            this.Finalize(output.Slice(outOff + lastLen, TagBytes));

            return required;
        }
        finally
        {
            this._completed = true;
        }
    }

    /// <summary>
    /// Decrypts <paramref name="ciphertextWithTag" /> and verifies the 16-byte authentication tag.
    /// </summary>
    /// <param name="ciphertextWithTag">
    /// The ciphertext followed immediately by the 16-byte authentication tag. Must be at least <see cref="TagBytes" />
    /// bytes long.
    /// </param>
    /// <param name="output">
    /// Receives the decrypted plaintext. Must be at least <c>ciphertextWithTag.Length - <see cref="TagBytes"/></c>
    /// bytes long.
    /// </param>
    /// <returns>Bytes written: <c>ciphertextWithTag.Length - <see cref="TagBytes"/></c>.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="ProcessAssociatedData" /> has not been called.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertextWithTag" /> is shorter than <see cref="TagBytes" /> bytes, or
    /// <paramref name="output" /> is too small.
    /// </exception>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
    {
        this.ThrowIfDisposed();
        this.ThrowIfCompleted();
        this.ThrowIfAadNotProcessed();

        if (ciphertextWithTag.Length < TagBytes)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_CiphertextTooShort, TagBytes),
                nameof(ciphertextWithTag));

        var ptLen = ciphertextWithTag.Length - TagBytes;
        if (output.Length < ptLen)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_OutputBufferTooSmall, ptLen),
                nameof(output));

        try
        {
            ReadOnlySpan<byte> ciphertext = ciphertextWithTag[..ptLen];
            ReadOnlySpan<byte> inTag = ciphertextWithTag[ptLen..];

            var cOff = 0;
            var pOff = 0;

            while (cOff + Rate <= ciphertext.Length)
            {
                var c0 = BinaryPrimitives.ReadUInt64LittleEndian(ciphertext[cOff..]);
                var c1 = BinaryPrimitives.ReadUInt64LittleEndian(ciphertext[(cOff + 8)..]);

                BinaryPrimitives.WriteUInt64LittleEndian(output[pOff..], this._state.S0 ^ c0);
                BinaryPrimitives.WriteUInt64LittleEndian(output[(pOff + 8)..], this._state.S1 ^ c1);

                this._state.S0 = c0;
                this._state.S1 = c1;
                this._state.Permute(Pb);

                cOff += Rate;
                pOff += Rate;
            }

            var lastLen = ciphertext.Length - cOff;

            Span<byte> stateBytes = stackalloc byte[Rate];
            BinaryPrimitives.WriteUInt64LittleEndian(stateBytes, this._state.S0);
            BinaryPrimitives.WriteUInt64LittleEndian(stateBytes[8..], this._state.S1);

            // Capture original ciphertext bytes before writing plaintext to output.
            // output and ciphertextWithTag may alias the same buffer, so ciphertext[cOff..]
            // would be overwritten by the XOR loop if we don't save it first.
            Span<byte> lastCt = stackalloc byte[lastLen == 0 ? 1 : lastLen];
            ciphertext.Slice(cOff, lastLen).CopyTo(lastCt);

            for (var i = 0; i < lastLen; i++)
                output[pOff + i] = (byte)(lastCt[i] ^ stateBytes[i]);

            lastCt[..lastLen].CopyTo(stateBytes);
            stateBytes[lastLen] ^= 0x01;

            this._state.S0 = BinaryPrimitives.ReadUInt64LittleEndian(stateBytes);
            this._state.S1 = BinaryPrimitives.ReadUInt64LittleEndian(stateBytes[8..]);

            Span<byte> expectedTag = stackalloc byte[TagBytes];
            this.Finalize(expectedTag);

            if (!CryptographicOperations.FixedTimeEquals(inTag, expectedTag))
            {
                CryptographicOperations.ZeroMemory(output[..ptLen]);
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AuthenticationTagMismatch);
            }

            return ptLen;
        }
        finally
        {
            this._completed = true;
        }
    }

    /// <summary>
    /// Releases the resources used by this instance and clears retained key material and sponge state from memory.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release managed resources; <see langword="false" /> to release unmanaged resources
    /// only.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (this._disposed) return;

        if (disposing)
        {
            CryptoHelpers.Clear(ref this._state);
            this._key.Clear();

            this._aadProcessed = false;
            this._completed = true;
        }

        this._disposed = true;
    }

    /// <summary>
    /// Applies the finalization phase: injects the key into words 1 and 2, applies Ascon-p12, and writes the tag from
    /// words 3 and 4 XORed with the key halves.
    /// </summary>
    /// <param name="tag">Destination span for the 16-byte authentication tag.</param>
    private void Finalize(Span<byte> tag)
    {
        var k0 = this._key.K0;
        var k1 = this._key.K1;

        this._state.S1 ^= k0;
        this._state.S2 ^= k1;
        this._state.Permute(Pa);

        BinaryPrimitives.WriteUInt64LittleEndian(tag, this._state.S3 ^ k0);
        BinaryPrimitives.WriteUInt64LittleEndian(tag[8..], this._state.S4 ^ k1);
    }

    /// <summary>
    /// Validates that <paramref name="value" /> is not <see langword="null" /> and returns it as a
    /// <see cref="ReadOnlySpan{T}" />, enabling null-safe constructor chaining from the <c>byte[]</c> overload.
    /// </summary>
    /// <param name="value">The byte array to validate.</param>
    /// <param name="paramName">The caller-visible parameter name reported in any exception.</param>
    /// <returns>A <see cref="ReadOnlySpan{Byte}" /> over <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    private static ReadOnlySpan<byte> ValidateNotNull(byte[] value, string paramName)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        return value.AsSpan();
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if this instance has already completed encryption or decryption.
    /// </summary>
    private void ThrowIfCompleted()
    {
        if (this._completed)
            throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_TransformAlreadyFinalized);
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException" /> if <see cref="ProcessAssociatedData" /> has not yet been called.
    /// </summary>
    private void ThrowIfAadNotProcessed()
    {
        if (!this._aadProcessed)
            throw new InvalidOperationException(CryptoResourceStrings.Crypt_Invalid_AssociatedDataNotProcessed);
    }

    /// <summary>
    /// Stores the retained 128-bit key material for an <see cref="AsconAead128" /> instance.
    /// </summary>
    /// <remarks>
    /// The holder reference is retained by the parent instance, while the contained key words remain clearable during
    /// disposal. This avoids storing the secret key in readonly scalar fields that cannot be zeroed.
    /// </remarks>
    private sealed class KeyMaterial128
    {
        private ulong _k0;
        private ulong _k1;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyMaterial128" /> class from the supplied key bytes.
        /// </summary>
        /// <param name="key">The 128-bit key bytes.</param>
        public KeyMaterial128(ReadOnlySpan<byte> key)
        {
            this._k0 = BinaryPrimitives.ReadUInt64LittleEndian(key);
            this._k1 = BinaryPrimitives.ReadUInt64LittleEndian(key[8..]);
        }

        /// <summary>
        /// Gets the first 64-bit key word.
        /// </summary>
        public ulong K0 => this._k0;

        /// <summary>
        /// Gets the second 64-bit key word.
        /// </summary>
        public ulong K1 => this._k1;

        /// <summary>
        /// Clears the retained key words from this holder.
        /// </summary>
        public void Clear()
        {
            CryptoHelpers.Clear(ref this._k0);
            CryptoHelpers.Clear(ref this._k1);
        }
    }
}
