// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
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
/// tag.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Tag size: 128 bits (16 bytes), fixed.
/// </description>
/// </item>
/// <item>
/// <description>
/// Key size: 256 bits (32 bytes) — first half is clamped to <c>r</c>, second half is the nonce-derived <c>s</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Block size: 16 bytes; arithmetic modulo <c>2¹³⁰ − 5</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Specification: RFC 8439 (ChaCha20-Poly1305).
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Single-use:</strong> a key must authenticate exactly one message.
/// </description>
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
    : KeyedBlockHashAlgorithm<Poly1305>
{
    /// <summary>
    /// Length of the Poly1305 key is 256 bits (32 bytes).
    /// </summary>
    public const int KeySize = 256;

    private const uint Mask26 = 0x3ffffff;

    /// <summary>
    /// Length of the Poly1305 input block is 128 bits (16 bytes). Byte length is derived inline via
    /// <see cref="BlockSize" /> / 8 where needed.
    /// </summary>
    private new const int BlockSize = 128;

    private readonly uint[] _acc = new uint[5];
    private readonly uint[] _key = new uint[4];
    private readonly uint[] _r = new uint[5];    // Polynomial key

    // Encrypted nonce
    private readonly uint[] _s = new uint[4];    // Precomputed 5 * r[1..4]

    // Tracks whether ProcessFinalBlock has run for the currently-assigned key. Once set, the instance must
    // not accept further blocks or finalize again until the caller explicitly assigns a fresh Key —
    // Poly1305 is a one-time MAC and reusing a (key, accumulator) pair across messages enables forgery.
    private bool _finalized;

    // Polynomial accumulator

    /// <summary>
    /// Initializes a new instance of the <see cref="Poly1305" /> class with no key set.
    /// </summary>
    /// <remarks>
    /// Unlike most keyed hash algorithms, <see cref="Poly1305" /> deliberately does not auto-generate a random key in
    /// its constructor. Poly1305 is a one-time authenticator and reusing the same key against multiple messages breaks
    /// its security guarantees; an instance with an unsolicited random key tempts callers to drive
    /// <see cref="HashAlgorithm.ComputeHash(byte[])" /> twice and silently produce a forgeable tag the second time.
    /// Callers must therefore set <see cref="KeyedBlockHashAlgorithm{T}.Key" /> explicitly before computing a hash;
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
    /// <returns>
    /// <see langword="false" /> for <see cref="Poly1305" />, which is a one-time authenticator that must not be reused
    /// with the same key.
    /// </returns>
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
    /// is rejected by <see cref="ProcessBlock" /> / <see cref="ProcessFinalBlock" />.
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
            CryptographyHelper.Clear(_acc);
            CryptographyHelper.Clear(_r);
            CryptographyHelper.Clear(_key);
            CryptographyHelper.Clear(_s);
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength) =>
        throw new NotSupportedException(CryptoResourceStrings.Op_NotSupported_Poly1305Padding);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.ReadabilityRules",
        "SA1107:Code should not contain multiple statements on one line",
        Justification = "Grouped limb and accumulator assignments intentionally mirror the Poly1305 5-limb arithmetic steps, keeping related state transitions on one line so the implementation remains aligned with the algorithm structure.")]
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        if (_finalized)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AlreadyFinalized);

        const int blockBytes = BlockSize / 8;

        // Copy input to 16-byte buffer and append a single '1' byte if block is short (RFC: padding = 1 byte then zeros)
        Span<byte> padded = stackalloc byte[blockBytes];
        block.CopyTo(padded);
        if (block.Length < blockBytes)
            padded[block.Length] = 1;

        // Load accumulator state
        ulong h0 = _acc[0], h1 = _acc[1], h2 = _acc[2], h3 = _acc[3], h4 = _acc[4];

        // Convert padded input block into 130-bit number split into 5 26-bit limbs (as per RFC)
        var t0 = BinaryPrimitives.ReadUInt64LittleEndian(padded);
        var t1 = BinaryPrimitives.ReadUInt64LittleEndian(padded[8..]);
        h0 += (uint)(t0 & 0x3ffffff);
        h1 += (uint)((t0 >> 26) & 0x3ffffff);
        h2 += (uint)(((t0 >> 52) | (t1 << 12)) & 0x3ffffff);
        h3 += (uint)((t1 >> 14) & 0x3ffffff);
        h4 += (uint)((t1 >> 40) & 0x3ffffff);

        // If full 16 bytes were present, set highest bit (equivalent to adding 2^128 per RFC)
        if (block.Length == blockBytes)
            h4 += 1 << 24;

        // Load r and perform 130-bit polynomial multiplication: accumulator * r
        ulong r0 = _r[0], r1 = _r[1], r2 = _r[2], r3 = _r[3], r4 = _r[4];

        // Compute limb products with optimized carry structure
        var t00 = (h0 * r0) + (h1 * _s[3]) + (h2 * _s[2]) + (h3 * _s[1]) + (h4 * _s[0]);
        var t01 = (h0 * r1) + (h1 * r0) + (h2 * _s[3]) + (h3 * _s[2]) + (h4 * _s[1]);
        var t02 = (h0 * r2) + (h1 * r1) + (h2 * r0) + (h3 * _s[3]) + (h4 * _s[2]);
        var t03 = (h0 * r3) + (h1 * r2) + (h2 * r1) + (h3 * r0) + (h4 * _s[3]);
        var t04 = (h0 * r4) + (h1 * r3) + (h2 * r2) + (h3 * r1) + (h4 * r0);

        // Perform carry propagation and modular reduction mod 2^130 - 5
        t01 += t00 >> 26; h0 = (uint)(t00 & Mask26);
        t02 += t01 >> 26; h1 = (uint)(t01 & Mask26);
        t03 += t02 >> 26; h2 = (uint)(t02 & Mask26);
        t04 += t03 >> 26; h3 = (uint)(t03 & Mask26);
        var carry = t04 >> 26; h4 = (uint)(t04 & Mask26);

        // Fold final carry into h0 (modulo 2^130 - 5 reduction)
        h0 += (uint)(carry * 5);
        carry = h0 >> 26; h0 &= Mask26;
        h1 += (uint)carry;

        // Save accumulator state
        _acc[0] = (uint)h0; _acc[1] = (uint)h1; _acc[2] = (uint)h2; _acc[3] = (uint)h3; _acc[4] = (uint)h4;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.ReadabilityRules",
        "SA1107:Code should not contain multiple statements on one line",
        Justification = "Grouped carry-propagation statements intentionally mirror the Poly1305 limb-normalization and reduction steps, keeping each carry and mask operation together as one logical arithmetic transition.")]
    protected override byte[] ProcessFinalBlock()
    {
        if (_finalized)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_AlreadyFinalized);

        // Final modular reduction: canonicalize accumulator to [0..2^130-5]
        uint h0 = _acc[0], h1 = _acc[1], h2 = _acc[2], h3 = _acc[3], h4 = _acc[4];

        // Propagate carries across limbs
        h1 += h0 >> 26; h0 &= Mask26;
        h2 += h1 >> 26; h1 &= Mask26;
        h3 += h2 >> 26; h2 &= Mask26;
        h4 += h3 >> 26; h3 &= Mask26;

        // Compute g = h + 5, then conditionally reduce modulo 2^130-5 if g >= 2^130
        h0 += 5;
        h1 += h0 >> 26; h0 &= Mask26;
        h2 += h1 >> 26; h1 &= Mask26;
        h3 += h2 >> 26; h2 &= Mask26;
        h4 += h3 >> 26; h3 &= Mask26;

        Span<byte> tag = stackalloc byte[16];

        // If h + 5 carried into bit 130, c starts at 0 and the reduced value is used.
        // Otherwise, c starts at -5, which cancels the test addition above.
        var c = ((int)(h4 >> 26) - 1) * 5L;

        c += (long)_key[0] + (h0 | (h1 << 26));
        BinaryPrimitives.WriteUInt32LittleEndian(tag[..], (uint)c);
        c >>= 32;

        c += (long)_key[1] + ((h1 >> 6) | (h2 << 20));
        BinaryPrimitives.WriteUInt32LittleEndian(tag[4..], (uint)c);
        c >>= 32;

        c += (long)_key[2] + ((h2 >> 12) | (h3 << 14));
        BinaryPrimitives.WriteUInt32LittleEndian(tag[8..], (uint)c);
        c >>= 32;

        c += (long)_key[3] + ((h3 >> 18) | (h4 << 8));
        BinaryPrimitives.WriteUInt32LittleEndian(tag[12..], (uint)c);

        // Mark the instance as finalized so subsequent ProcessBlock / ProcessFinalBlock calls — and the
        // ProcessBlock invoked by base.Initialize → OnKeyChanged → next ComputeHash — are rejected. The
        // guard is unconditional across all target frameworks because Poly1305's one-time-key contract is
        // not defended by HashAlgorithm.State alone (Initialize resets State to 0 after every ComputeHash).
        _finalized = true;

        // Key material is no longer needed — zero it immediately to enforce Poly1305's one-time-use
        // guarantee and limit key exposure in memory. The buffer is retained (not nullified) so the
        // framework's automatic re-initialization at the end of ComputeHash does not trip the
        // key-set check in Initialize.
        if (KeyValue is not null)
            CryptographyHelper.Clear(KeyValue);

        // Zero the derived key schedule and accumulator before returning so the tag-producing secret
        // material is not observable in memory between ProcessFinalBlock and the framework's automatic
        // re-initialization at the end of ComputeHash. Dispose still clears these as defence in depth.
        CryptographyHelper.Clear(_r);
        CryptographyHelper.Clear(_s);
        CryptographyHelper.Clear(_key);
        CryptographyHelper.Clear(_acc);

        return tag.ToArray();
    }

    /// <inheritdoc />
    protected override bool ShouldPadFinalBlock() => false;

    /// <summary>
    /// Rebuilds the polynomial key schedule and resets the accumulator whenever the key is assigned or the instance is
    /// re-initialized.
    /// </summary>
    /// <remarks>
    /// Loads and clamps <c>r</c>, precomputes the <c>5 * r[i]</c> helpers, and captures the second half of the key
    /// <c>s</c> used in the final tag calculation. The polynomial accumulator is reset so that the next hash
    /// computation starts from a clean state.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void OnKeyChanged()
    {
        // Reset accumulator so the next computation starts clean — correct whether this
        // hook runs in response to an explicit Key assignment, from Initialize, or from
        // the constructor's default-key setup.
        CryptographyHelper.Clear(_acc);

        // OnKeyChanged is only invoked by the Key setter and Initialize, both of which guarantee
        // KeyValue is non-null and of the expected length before this point.
        ReadOnlySpan<byte> key = KeyValue!;

        // Load and clamp the first 128 bits of the key as the polynomial 'r' key Clamp 'r' by setting/clearing specific bits to avoid
        // vulnerabilities as per RFC 8439, Section 2.5.1
        var t0 = BinaryPrimitives.ReadUInt32LittleEndian(key[..]);
        var t1 = BinaryPrimitives.ReadUInt32LittleEndian(key[4..]);
        var t2 = BinaryPrimitives.ReadUInt32LittleEndian(key[8..]);
        var t3 = BinaryPrimitives.ReadUInt32LittleEndian(key[12..]);

        // Split 128-bit r into 5 x 26-bit limbs with clamping (see RFC for bitmask values)
        _r[0] = t0 & 0x03FFFFFFU;
        _r[1] = ((t0 >> 26) | (t1 << 6)) & 0x03FFFF03U;
        _r[2] = ((t1 >> 20) | (t2 << 12)) & 0x03FFC0FFU;
        _r[3] = ((t2 >> 14) | (t3 << 18)) & 0x03F03FFFU;
        _r[4] = (t3 >> 8) & 0x000FFFFFU;

        // Precompute 5*r[i] values to optimize carry-reduction step later
        _s[0] = _r[1] * 5;
        _s[1] = _r[2] * 5;
        _s[2] = _r[3] * 5;
        _s[3] = _r[4] * 5;

        // Load the second half of the key (s), which will be added during final tag computation
        _key[0] = BinaryPrimitives.ReadUInt32LittleEndian(key[16..]);
        _key[1] = BinaryPrimitives.ReadUInt32LittleEndian(key[20..]);
        _key[2] = BinaryPrimitives.ReadUInt32LittleEndian(key[24..]);
        _key[3] = BinaryPrimitives.ReadUInt32LittleEndian(key[28..]);
    }
}
