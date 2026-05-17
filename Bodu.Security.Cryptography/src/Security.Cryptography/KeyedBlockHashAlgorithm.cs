// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedBlockHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the abstract base class for hash algorithms that require a secret key and process data in fixed-size blocks.
/// </summary>
/// <typeparam name="T">The concrete derived algorithm type. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="BlockHashAlgorithm{T}"/> with key-handling logic shared by keyed block hashes such as
/// <see cref="Poly1305"/> and <see cref="SipHash{T}"/>. It centralizes defensive copying, key-length validation, disposal of
/// secret material, and the hook (<see cref="OnKeyChanged"/>) used by derived classes to derive any key-dependent schedule or
/// internal state.
/// </para>
/// <para>
/// Derived classes supply the required key length via the <see cref="KeyedBlockHashAlgorithm{T}(int, int)"/> constructor. The
/// <see cref="Key"/> property setter validates the supplied byte array against that length, stores a defensive copy in
/// <see cref="KeyValue"/>, and then invokes <see cref="OnKeyChanged"/> so the derived algorithm can rebuild any key-dependent state.
/// </para>
/// <para>
/// <strong>When to derive from this class.</strong> Pick <see cref="KeyedBlockHashAlgorithm{T}"/> for keyed
/// hashes that follow the Merkle–Damgård pad-and-finalize pattern and require a fixed-length key —
/// <see cref="Poly1305"/> (32-byte key) and <see cref="SipHash{T}"/> (16-byte key) are the canonical users.
/// For BLAKE-family hashes that accept an <em>optional</em> variable-length key derive from
/// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}"/> instead. For unkeyed Merkle–Damgård hashes use
/// <see cref="BlockHashAlgorithm{T}"/> directly.
/// </para>
/// </remarks>
/// <seealso cref="BlockHashAlgorithm{T}"/>
/// <seealso cref="KeyedDeferredFinalBlockHashAlgorithm{T}"/>
public abstract class KeyedBlockHashAlgorithm<T>
    : BlockHashAlgorithm<T>
    where T : KeyedBlockHashAlgorithm<T>, new()
{
    /// <summary>
    /// Internal storage for the key used by the algorithm. Always assigned via defensive copy and cleared on disposal.
    /// </summary>
    /// <remarks>
    /// Declared <see cref="byte"/>[] nullable to honestly reflect that the field can be observed in three states:
    /// <see langword="null"/> on a freshly-constructed instance whose constructor has not yet seeded a key, after
    /// <see cref="Dispose(bool)"/> has cleared it, and between assignments. The <see cref="Key"/> getter and
    /// <see cref="Initialize"/> validation both treat a <see langword="null"/> value as a contract violation and
    /// throw a <see cref="CryptographicException"/>.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1306:Field names should begin with lower-case letter",
        Justification = "The field intentionally follows the protected field naming pattern used by HashAlgorithm, such as HashSizeValue, because it forms part of the inherited algorithm-state surface for derived cryptographic types.")]
    protected byte[]? KeyValue;

    /// <summary>
    /// Holds the required key size, in bits, that the derived algorithm accepts. Supplied via the constructor; aligns
    /// with the BCL convention used by <see cref="System.Security.Cryptography.SymmetricAlgorithm.KeySize"/>. Divide
    /// by 8 to obtain the equivalent byte length used when validating <see cref="Key"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1306:Field names should begin with lower-case letter",
        Justification = "The field intentionally follows the protected field naming pattern used by HashAlgorithm, such as HashSizeValue, because it forms part of the inherited algorithm-state surface for derived cryptographic types.")]
    protected readonly int KeySizeValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedBlockHashAlgorithm{T}"/> class with the specified input block size and
    /// required key size.
    /// </summary>
    /// <param name="blockSize">
    /// The fixed size of input blocks, in bits, that the algorithm processes. Must be a positive multiple of 8. This must
    /// match the internal block size used by the underlying hash structure.
    /// </param>
    /// <param name="keySize">
    /// The exact required key size, in bits, that the derived algorithm accepts. Must be a positive multiple of 8. Used
    /// by the <see cref="Key"/> setter (after dividing by 8) to validate caller-supplied key material.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="blockSize"/> or <paramref name="keySize"/> is less than or equal to zero.
    /// </exception>
    protected KeyedBlockHashAlgorithm(int blockSize, int keySize)
        : base(blockSize)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(keySize, 0);
        this.KeySizeValue = keySize;
    }

    /// <summary>
    /// Gets or sets the secret key used by the keyed hash algorithm to compute the message authentication code (MAC).
    /// </summary>
    /// <value>A byte array containing the key material. Both the getter and the setter operate on defensive copies.</value>
    /// <remarks>
    /// <para>
    /// The key must be set prior to calling any hashing methods such as <see cref="HashAlgorithm.ComputeHash(byte[])"/>. The setter
    /// stores a private copy of the supplied array, then invokes <see cref="OnKeyChanged"/> so derived classes can rebuild any
    /// key-dependent internal state (for example, a polynomial key schedule or pre-computed state vectors).
    /// </para>
    /// <para>
    /// The getter returns a defensive copy of the stored key so external callers cannot mutate the internal representation.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// A hash computation has already started, and the key may not be reassigned while the algorithm is in use.
    /// </exception>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">
    /// The length of the assigned key does not match the required key size for this algorithm.
    /// </exception>
    public virtual byte[] Key
    {
        get
        {
            this.ThrowIfDisposed();

            if (this.KeyValue is null)
                throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_KeyNotSet);

            return this.KeyValue.Copy();
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();
            ThrowHelper.ThrowIfNull(value);

            if (value.Length != this.KeySizeValue / 8)
                throw new CryptographicException(
                    string.Format(
                        CryptoResourceStrings.Crypt_Invalid_KeySize,
                        value.Length * 8,
                        this.KeySizeValue));

            // Defensive copy ensures external references cannot mutate the internal key.
            this.KeyValue = value.Copy();

            // Allow derived classes to rebuild any key-dependent schedule or state.
            this.OnKeyChanged();
        }
    }

    /// <summary>
    /// Resets the algorithm to its initial state, ready to accept fresh input. Derived classes should override
    /// <see cref="OnKeyChanged"/> — invoked automatically from here — to rebuild any key-dependent internal state.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicException">
    /// No key has been assigned, or the currently assigned key is not of the required length.
    /// </exception>
    public override void Initialize()
    {
        // base.Initialize() throws ObjectDisposedException on a disposed instance and clears the residual
        // buffer and counters. Derived classes follow the BCL convention of calling base.Initialize() and
        // then resetting their own state in their own Initialize override.
        base.Initialize();

        // A keyed MAC is unusable without a key. We refuse to silently regenerate one:
        // callers must explicitly set Key (or invoke GenerateKey on subclasses that
        // support it) before re-initialization.
        if (this.KeyValue is null || this.KeyValue.Length != this.KeySizeValue / 8)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_KeyNotSet);

        this.OnKeyChanged();
    }

    /// <summary>
    /// Called after <see cref="KeyValue"/> has been assigned or the algorithm has been re-initialized. Derived classes override
    /// this to rebuild any key-dependent internal state such as a round-key schedule, precomputed vectors, or accumulator reset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By contract, when this method runs <see cref="KeyValue"/> is guaranteed to be non-<see langword="null"/> and of byte length
    /// <see cref="KeySizeValue"/> / 8. It is invoked from the <see cref="Key"/> setter, from <see cref="Initialize"/>, and should
    /// be invoked by derived constructors after they have placed a default key into <see cref="KeyValue"/>.
    /// </para>
    /// <para>
    /// The default implementation is a no-op so that derived classes with no key-dependent precomputation pay no overhead.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void OnKeyChanged()
    {
    }

    /// <summary>
    /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.
    /// </param>
    /// <remarks>Ensures all internal secrets are overwritten with zeros before releasing resources.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (this.IsDisposed) return;

        if (disposing)
        {
            CryptoHelpers.ClearAndNullify(ref this.KeyValue);
        }

        base.Dispose(disposing);
    }
}
