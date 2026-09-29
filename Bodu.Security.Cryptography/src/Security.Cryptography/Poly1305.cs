// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes the message authentication code (MAC) for the input data using the <c>Poly1305</c> algorithm. This
/// implementation enforces one-time key usage and produces a fixed 16-byte (128-bit) tag from a 256-bit key, as
/// specified in RFC 8439.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Poly1305" /> is a cryptographic MAC function that operates by interpreting input data as a polynomial
/// over a finite field, evaluated using a secret 128-bit key <c>r</c>, and combined with an additional 128-bit key
/// <c>s</c> to produce the final output tag. The keys are derived from a single 256-bit (32-byte) input, where the
/// first half is clamped to create <c>r</c> and the second half serves as the nonce-derived offset <c>s</c>.
/// </para>
/// <para>
/// The algorithm is designed for speed and simplicity, particularly on systems without specialized cryptographic
/// hardware. It is most commonly used in conjunction with a stream cipher (e.g., ChaCha20) in AEAD constructions such
/// as <c>ChaCha20-Poly1305</c>. This implementation adheres to the
/// <a href="https://datatracker.ietf.org/doc/html/rfc8439">RFC 8439</a> specification and computes a 16-byte tag for
/// each input message, ensuring authenticity and integrity.
/// </para>
/// <para>
/// Internally, the input is split into 16-byte blocks, each treated as a 130-bit number (with an additional high bit if
/// full-sized), and accumulated using modular arithmetic modulo <c>2¹³⁰ - 5</c>. After processing all blocks, the
/// accumulator is finalized by adding the second half of the key <c>s</c> and serializing the result as the final MAC
/// tag. The arithmetic runs on three 64-bit limbs with 128-bit products, straight from the caller's buffers, and takes
/// the same time for every message of a given length. On x64 processors with AVX2, runs of 512 bytes or more are
/// absorbed four or eight blocks at a time in vector registers, over 26-bit limbs, to the same result.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Tag size: 128 bits (16 bytes), fixed.</description>
/// </item>
/// <item>
/// <description>
/// Key size: 256 bits (32 bytes) — first half is clamped to <c>r</c>, second half is the nonce-derived <c>s</c>.
/// </description>
/// </item>
/// <item>
/// <description>Block size: 16 bytes; arithmetic modulo <c>2¹³⁰ − 5</c>.</description>
/// </item>
/// <item>
/// <description>Specification: RFC 8439 (ChaCha20-Poly1305).</description>
/// </item>
/// <item>
/// <description><strong>Single-use:</strong> a key must authenticate exactly one message.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Poly1305.</strong> Reach for Poly1305 when implementing or extending a Poly1305-based AEAD
/// (ChaCha20-Poly1305 in TLS 1.3, SSH, WireGuard, Noise) or when authenticating a single message with a fresh
/// per-message key derived from a stream cipher. For multi-message keyed authentication use HMAC-SHA-256,
/// <see cref="Blake2b" />-MAC, or <see cref="SipHash64" /> / <see cref="SipHash128" /> — none of which share Poly1305's
/// one-time-key restriction.
/// </para>
/// <note type="important">This algorithm is a <b>one-time authenticator</b> and must <b>not</b> be used with the same
/// key for multiple messages. Reusing a key with different inputs <b>compromises</b> the cryptographic security and may
/// lead to forgery attacks.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // The 32-byte key MUST be unique per message — typically derived from a stream cipher's keystream.
/// byte[] oneTimeKey = DerivePoly1305KeyFromChaCha20(sessionKey, nonce);
/// using var poly = new Poly1305 { Key = oneTimeKey };
/// byte[] tag = poly.ComputeHash(message);
///]]>
/// </code>
/// </example>
public sealed class Poly1305
    : KeyedBlockHashAlgorithm
{
    /// <summary>Length of the Poly1305 key is 256 bits (32 bytes).</summary>
    public const int KeySize = 256;

    /// <summary>Length of the Poly1305 input block is 128 bits (16 bytes). Byte length is derived inline via <see cref="BlockSize" /> / 8 where needed.</summary>
    private new const int BlockSize = 128;

    /// <summary>The authenticator: the key schedule, the accumulator, and any partial block, used in place.</summary>
    private Poly1305Core _core;

    /// <summary>Indicates whether <c>ProcessFinalBlock</c> has run for the currently-assigned key.</summary>
    /// <remarks>
    /// Once set, the instance must not accept further blocks or finalize again until the caller explicitly assigns a
    /// fresh Key — Poly1305 is a one-time MAC and reusing a (key, accumulator) pair across messages enables forgery.
    /// </remarks>
    private bool _finalized;

    /// <summary>
    /// Initializes a new instance of the <see cref="Poly1305" /> class with no key set.
    /// </summary>
    /// <remarks>
    /// Unlike most keyed hash algorithms, <see cref="Poly1305" /> deliberately does not auto-generate a random key in
    /// its constructor. Poly1305 is a one-time authenticator and reusing the same key against multiple messages breaks
    /// its security guarantees; an instance with an unsolicited random key tempts callers to drive
    /// <see cref="HashAlgorithm.ComputeHash(byte[])" /> twice and silently produce a forgeable tag the second time.
    /// Callers must therefore set <see cref="KeyedBlockHashAlgorithm.Key" /> explicitly before computing a hash;
    /// failing to do so causes <see cref="HashAlgorithm.Initialize" /> to raise a <see cref="CryptographicException" />
    /// .
    /// </remarks>
    public Poly1305()
        : base(BlockSize, KeySize)
    {
        HashSizeValue = 128;
    }

    /// <summary>
    /// Gets a value indicating whether this transform instance can be reused after a hash operation is completed.
    /// </summary>
    /// <value>
    /// <see langword="false" /> for <see cref="Poly1305" />, which is a one-time authenticator that must not be reused
    /// with the same key.
    /// </value>
    /// <remarks>
    /// <para>
    /// <see cref="Poly1305" /> is a one-time message authentication code (MAC) algorithm. Reusing the same instance
    /// with the same key for multiple messages violates the security guarantees of the algorithm and may lead to
    /// forgery attacks.
    /// </para>
    /// <para>
    /// The framework-invoked automatic re-initialization that occurs at the end of
    /// <see cref="HashAlgorithm.ComputeHash(byte[])" /> and related APIs is tolerated so that callers can read
    /// <see cref="HashAlgorithm.Hash" /> without tripping the key-set guard. However, explicit reuse of a finalized
    /// instance — for example, a second <see cref="HashAlgorithm.ComputeHash(byte[])" /> call — is rejected by the
    /// inherited finalized-state guard on frameworks prior to .NET 6. A fresh instance should be created for each
    /// message.
    /// </para>
    /// </remarks>
    public override bool CanReuseTransform => false;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// Assigning a fresh key clears the one-time-use finalization guard so the same instance may authenticate a new
    /// message under the new key. Reusing the <em>same</em> key for a second message remains a contract violation and
    /// is rejected by <see cref="HashCore(ReadOnlySpan{byte})" /> / <see cref="ProcessFinalBlock" />.
    /// </remarks>
    public override byte[] Key
    {
        get => base.Key;
        set
        {
            base.Key = value;
            _finalized = false;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The published name is simply <c>"Poly1305"</c>; the 128-bit tag width is fixed by the specification.
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return "Poly1305";
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    /// <remarks>
    /// Ensures all internal secrets are overwritten with zeros before releasing resources.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            _core.Clear();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override int PadBlock(ReadOnlySpan<byte> block, ulong messageLength, Span<byte> destination) =>
        throw new NotSupportedException(CryptoResourceStrings.Op_NotSupported_Poly1305Padding);

    /// <inheritdoc />
    /// <remarks>
    /// The input goes straight to the authenticator, which holds a partial block itself, so the block-hash base's
    /// residual buffer and per-block dispatch are bypassed.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicException">
    /// The tag has already been produced under the current key; assign a fresh <see cref="Key" /> first.
    /// </exception>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        ThrowIfDisposed();
        ThrowIfFinalized();

        _core.Update(source);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The authenticator absorbs any partial last block itself, so this goes straight to
    /// <see cref="ProcessFinalBlock" />.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicException">
    /// The tag has already been produced under the current key; assign a fresh <see cref="Key" /> first.
    /// </exception>
    protected override byte[] HashFinal()
    {
        ThrowIfDisposed();

        return ProcessFinalBlock();
    }

    /// <inheritdoc />
    /// <exception cref="CryptographicException">
    /// The tag has already been produced under the current key; assign a fresh <see cref="Key" /> first.
    /// </exception>
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        ThrowIfFinalized();

        _core.Update(block);
    }

    /// <inheritdoc />
    /// <exception cref="CryptographicException">
    /// The tag has already been produced under the current key; assign a fresh <see cref="Key" /> first.
    /// </exception>
    protected override byte[] ProcessFinalBlock()
    {
        ThrowIfFinalized();

        // Finish clears the whole authenticator, key schedule and accumulator included, once the tag is written, so
        // no tag-producing secret stays in memory between here and the framework's automatic re-initialization at the
        // end of ComputeHash. Dispose still clears it as defence in depth.
        byte[] tag = new byte[Poly1305Core.TagBytes];
        _core.Finish(tag);

        // Mark the instance as finalized so subsequent HashCore / ProcessFinalBlock calls — and the next
        // ComputeHash after the framework's automatic re-initialization — are rejected. The guard is
        // unconditional across all target frameworks because Poly1305's one-time-key contract is not defended
        // by HashAlgorithm.State alone (Initialize resets State to 0 after every ComputeHash).
        _finalized = true;

        // Key material is no longer needed — zero it immediately to enforce Poly1305's one-time-use
        // guarantee and limit key exposure in memory. The buffer is retained (not nullified) so the
        // framework's automatic re-initialization at the end of ComputeHash does not trip the
        // key-set check in Initialize.
        if (KeyValue is not null)
            CryptographyHelper.Clear(KeyValue);

        return tag;
    }

    /// <inheritdoc />
    protected override bool ShouldPadFinalBlock() => false;

    /// <summary>
    /// Rebuilds the polynomial key schedule and resets the accumulator whenever the key is assigned or the instance is
    /// re-initialized.
    /// </summary>
    /// <remarks>
    /// Loads and clamps <c>r</c>, captures the second half of the key <c>s</c> used in the final tag calculation, and
    /// resets the accumulator so that the next hash computation starts from a clean state.
    /// </remarks>
    protected override void OnKeyChanged()
    {
        // OnKeyChanged is only invoked by the Key setter and Initialize, both of which guarantee
        // KeyValue is non-null and of the expected length before this point.
        _core.Initialize(KeyValue!);
    }

    /// <summary>
    /// Throws when the tag has already been produced under the current key.
    /// </summary>
    /// <exception cref="CryptographicException">The instance is finalized.</exception>
    private void ThrowIfFinalized()
    {
        if (_finalized)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AlreadyFinalized);
    }
}
