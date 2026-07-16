// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedDeferredFinalBlockHashAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the abstract base class for hash algorithms that support an optional secret key and defer compression of
/// the final block until <see cref="HashAlgorithm.HashFinal" /> is called, following the BLAKE-family
/// deferred-finalization pattern.
/// </summary>
/// <remarks>
/// <para>
/// This class extends <see cref="DeferredFinalBlockHashAlgorithm" /> with optional key-handling logic shared by
/// keyed BLAKE-family hashes such as <see cref="Blake2b" /> and <see cref="Blake2s" />. It centralizes defensive
/// copying, key-length validation, secure disposal of secret material, and the sealed <see cref="Initialize" />
/// override that orchestrates hash-state reset followed by key-block injection when a key is set.
/// </para>
/// <para>
/// The key is optional: assigning an empty array (or never assigning a key) places the instance in the standard unkeyed
/// digest mode. Assigning a non-empty array of at most <see cref="MaximumKeySize" /> / 8 bytes switches the instance to
/// the keyed MAC mode defined in RFC 7693 Section 2.8 — the key is zero-padded to the block size and prepended as the
/// first message block.
/// </para>
/// <para>
/// Derived classes must implement <see cref="InitializeHashState" /> to restore their algorithm-specific chaining
/// variables and encode the key length into the parameter block, and
/// <see cref="DeferredFinalBlockHashAlgorithm.ProcessBlock" /> /
/// <see cref="DeferredFinalBlockHashAlgorithm.ProcessFinalBlock" /> as required by the grandparent.
/// </para>
/// <para>
/// <strong>When to derive from this class.</strong> Pick <see cref="KeyedDeferredFinalBlockHashAlgorithm" /> for
/// BLAKE-family hashes that accept an optional, variable-length key per RFC 7693 §2.8 — the canonical users are
/// <see cref="Blake2b" /> and <see cref="Blake2s" />. For BLAKE-family hashes without a key (e.g. <see cref="Blake3" />
/// ) derive from <see cref="DeferredFinalBlockHashAlgorithm" />. For Merkle–Damgård keyed hashes with a fixed-length
/// key (Poly1305, SipHash) derive from <see cref="KeyedBlockHashAlgorithm" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // BLAKE2b in keyed MAC mode — the key is optional and may be up to MaximumKeySize bits.
/// byte[] key = new byte[32];
/// RandomNumberGenerator.Fill(key);
///
/// using var mac = new Blake2b { Key = key };
/// byte[] tag = mac.ComputeHash("message"u8.ToArray());
///
/// // Or use the same type unkeyed for a plain BLAKE2b digest.
/// using var digest = new Blake2b();
/// byte[] hash = digest.ComputeHash("message"u8.ToArray());
///]]>
/// </code>
/// </example>
/// <seealso cref="DeferredFinalBlockHashAlgorithm"/> <seealso cref="KeyedBlockHashAlgorithm"/>
public abstract class KeyedDeferredFinalBlockHashAlgorithm
    : DeferredFinalBlockHashAlgorithm
{
    /// <summary>Internal storage for the optional secret key. <see langword="null" /> when the instance operates in the unkeyed digest profile; otherwise a defensive copy of the caller-supplied key. Always assigned via defensive copy and cleared on disposal.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1306:Field names should begin with lower-case letter",
        Justification = "The field intentionally follows the protected field naming pattern used by HashAlgorithm, such as HashSizeValue, because it forms part of the inherited algorithm-state surface for derived cryptographic types.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Follows the BCL protected field naming convention (like KeyValue on HMAC) so that derived BLAKE-family keyed hash types can read and clear key material directly without virtual dispatch.")]
    protected byte[]? KeyValue;

    /// <summary>The maximum accepted key size, in bits. Supplied by the derived class via the constructor and used by the <see cref="Key" /> setter (after dividing by 8) to validate caller-supplied key material.</summary>
    private readonly int _maximumKeySize;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedDeferredFinalBlockHashAlgorithm" /> class with the
    /// specified input block size and maximum key size.
    /// </summary>
    /// <param name="blockSize">
    /// The fixed size, in bits, of each block consumed by the algorithm. Must be a positive multiple of 8.
    /// </param>
    /// <param name="maximumKeySize">The maximum accepted key size, in bits. Must be a positive multiple of 8.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize" /> or <paramref name="maximumKeySize" /> is less than or equal to zero.
    /// </exception>
    protected KeyedDeferredFinalBlockHashAlgorithm(int blockSize, int maximumKeySize)
        : base(blockSize)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(maximumKeySize, 0);
        _maximumKeySize = maximumKeySize;
    }

    /// <summary>
    /// Gets the maximum accepted key size, in bits, for this algorithm instance. Divide by 8 to obtain the equivalent
    /// byte length used when allocating or validating <see cref="Key" />.
    /// </summary>
    /// <value>The maximum number of bits accepted as a secret key.</value>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    public int MaximumKeySize
    {
        get
        {
            ThrowIfDisposed();
            return _maximumKeySize;
        }
    }

    /// <summary>
    /// Gets or sets the optional secret key used to compute a keyed MAC digest.
    /// </summary>
    /// <value>
    /// A byte array of 1 to <see cref="MaximumKeySize" /> / 8 bytes that enables keyed MAC mode, or an empty array when
    /// operating in the unkeyed digest profile. Both the getter and the setter operate on defensive copies.
    /// </value>
    /// <remarks>
    /// <para>
    /// When the key is non-empty, the instance operates in the keyed MAC mode defined in RFC 7693 Section 2.8. The key
    /// length is encoded into the parameter block by <see cref="InitializeHashState" />, and the key itself
    /// (zero-padded to the block size) is prepended as the first message block before any caller-supplied input is
    /// processed.
    /// </para>
    /// <para>
    /// Setting the key calls <see cref="HashAlgorithm.Initialize" /> so that the algorithm state is immediately rebuilt
    /// for the new key. Setting an empty array clears the key and reverts the instance to unkeyed digest mode.
    /// </para>
    /// <para>
    /// The property may only be changed before hashing has begun; once <see cref="HashAlgorithm.TransformBlock" /> or a
    /// <c>ComputeHash</c> overload has been called, the value is immutable until
    /// <see cref="HashAlgorithm.Initialize" /> is called.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// The assigned key is longer than <see cref="MaximumKeySize" /> / 8 bytes.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// A hash computation is already in progress.
    /// </exception>
    public byte[] Key
    {
        get
        {
            ThrowIfDisposed();
            return KeyValue?.Copy() ?? [];
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();
            ThrowHelper.ThrowIfNull(value);

            if (value.Length > _maximumKeySize / 8)
            {
                throw new CryptographicException(
                    string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_KeySize, value.Length * 8, $"0..{_maximumKeySize}"));
            }

            // Zero the previously held key before replacing it so stale key material does not linger on the heap.
            CryptographyHelper.ClearAndNullify(ref KeyValue);
            KeyValue = value.Length > 0 ? value.Copy() : null;
            Initialize();
        }
    }

    /// <summary>
    /// Resets the algorithm to its initial state. Clears the inherited residual buffer and counters via
    /// <c>base.Initialize()</c>, then rebuilds the algorithm-specific hash state through
    /// <see cref="InitializeHashState" /> and, when a key has been set, injects the key block as the first message
    /// block. Sealed so that the key-injection protocol cannot be accidentally bypassed by derived classes.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public sealed override void Initialize()
    {
        base.Initialize();
        InitializeHashState();

        if (KeyValue is not null)
            InjectKeyBlock();
    }

    /// <summary>
    /// Resets the algorithm-specific chaining variables to their initialization values and encodes any configuration
    /// parameters (such as digest length and key length) into the parameter block. Called by the sealed
    /// <see cref="Initialize" /> before key-block injection.
    /// </summary>
    /// <remarks>
    /// Implementations should read <see cref="KeyValue" /> to determine the key length (<c>kk</c>) for parameter-block
    /// encoding, as <see cref="KeyValue" /> is already updated to the new value before <see cref="Initialize" /> runs.
    /// </remarks>
    protected abstract void InitializeHashState();

    /// <inheritdoc />
    /// <remarks>
    /// Clears the retained key material held by <see cref="KeyValue" />. The inherited residual buffer and framework
    /// hash state are cleared by the base implementation when this method delegates to <c>base.Dispose(disposing)</c>.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CryptographyHelper.ClearAndNullify(ref KeyValue);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Zero-pads the key to the full block size and feeds it through the buffering infrastructure as the first message
    /// block, per RFC 7693 Section 2.8. Must only be called when <see cref="KeyValue" /> is non-<see langword="null" />
    /// .
    /// </summary>
    private void InjectKeyBlock()
    {
        Span<byte> keyBlock = stackalloc byte[BlockSize / 8];
        try
        {
            KeyValue!.AsSpan().CopyTo(keyBlock);
            HashCore(keyBlock);
        }
        finally
        {
            // Zero the padded key copy so a full copy of the key does not linger on the stack after injection.
            CryptographyHelper.Clear(keyBlock);
        }
    }
}
