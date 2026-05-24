// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bernstein.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Daniel J. Bernstein's djb2 algorithm, optionally using the
/// XOR-modified variant. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The default algorithm computes <c>hash = (hash * 33) + c</c> for each input byte <c>c</c>. Setting
/// <see cref="UseModifiedAlgorithm" /> selects the XOR-modified form, <c>hash = (hash * 33) ^ c</c>, which may give
/// better distribution in some hash-table workloads.
/// </para>
/// <para>
/// Both <see cref="InitialValue" /> and <see cref="UseModifiedAlgorithm" /> are reconfigurable only while the algorithm
/// has not yet consumed input. <see cref="Reset" /> returns the instance to the reconfigurable state.
/// </para>
/// <para>
/// <strong>When to choose Bernstein.</strong> djb2 is the canonical "C-style" hash for short string keys — language
/// symbol tables, environment-variable maps, and small associative containers. Pick it when interoperating with code
/// that has standardized on djb2 (Perl, Python's older string hash, Tcl variable tables, etc.) or when the seed/variant
/// flexibility is useful. Empirically the XOR-modified form (<c>djb2a</c>, <see cref="UseModifiedAlgorithm" /> set to
/// <see langword="true" />) gives slightly better avalanche than the default additive form. For new code without an
/// interop constraint, <see cref="Fnv1a32" /> is a closely related but better-distributing default;
/// <see cref="MurmurHash3_32" /> is preferable for inputs longer than a few dozen bytes.
/// </para>
/// <para>
/// <strong>Output and lifecycle.</strong> Produces a 32-bit (4-byte) digest in little-endian byte order.
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> is non-destructive; instances are
/// not thread-safe.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// // Default djb2 with the canonical 5381 seed.
/// var djb2 = new Bernstein();
/// byte[] digest = djb2.ComputeHash(System.Text.Encoding.UTF8.GetBytes("symbol"));
///
/// // XOR-modified djb2a, generally better distribution.
/// var djb2a = new Bernstein { UseModifiedAlgorithm = true };
/// byte[] digestA = djb2a.ComputeHash(System.Text.Encoding.UTF8.GetBytes("symbol"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class Bernstein
    : NonCryptographicHashAlgorithm
{
    /// <summary>
    /// The default initial value used to seed the hash algorithm.
    /// </summary>
    public const uint DefaultInitialValue = 5381U;

    private const int HashLength = 4;

    private uint _initialValue;
    private bool _started;
    private bool _useModified;
    private uint _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bernstein" /> class with the canonical djb2 seed (
    /// <see cref="DefaultInitialValue" />) and the original addition form of the algorithm.
    /// </summary>
    public Bernstein()
        : this(DefaultInitialValue, useModifiedAlgorithm: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bernstein" /> class using the specified initial seed value and
    /// algorithm variant.
    /// </summary>
    /// <param name="initialValue">The initial seed applied to the running hash accumulator.</param>
    /// <param name="useModifiedAlgorithm">
    /// <see langword="true" /> to use the XOR-modified form <c>hash = (hash * 33) ^ c</c>; <see langword="false" /> to
    /// use the original <c>hash = (hash * 33) + c</c>.
    /// </param>
    public Bernstein(uint initialValue, bool useModifiedAlgorithm)
        : base(HashLength)
    {
        _initialValue = initialValue;
        _useModified = useModifiedAlgorithm;
        _workingHash = initialValue;
    }

    /// <summary>
    /// Gets or sets the initial seed value applied to the running hash accumulator.
    /// </summary>
    /// <value>The initial hash seed. Defaults to <see cref="DefaultInitialValue" />.</value>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The algorithm has already consumed input and cannot be reconfigured until <see cref="Reset" /> is invoked.
    /// </exception>
    public uint InitialValue
    {
        get => _initialValue;

        set
        {
            ThrowIfInvalidState();
            _initialValue = value;
            _workingHash = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the XOR-modified form of the algorithm is in use.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when each update performs <c>(hash * 33) ^ c</c>; <see langword="false" /> when it
    /// performs <c>(hash * 33) + c</c>.
    /// </value>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The algorithm has already consumed input and cannot be reconfigured until <see cref="Reset" /> is invoked.
    /// </exception>
    public bool UseModifiedAlgorithm
    {
        get => _useModified;

        set
        {
            ThrowIfInvalidState();
            _useModified = value;
        }
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
            return;

        if (_useModified)
            AppendModified(source);
        else
            AppendOriginal(source);

        _started = true;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _workingHash = _initialValue;
        _started = false;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, _workingHash);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendModified(ReadOnlySpan<byte> source)
    {
        var v = _workingHash;
        foreach (var b in source)
        {
            v = ((v << 5) + v) ^ b;
        }

        _workingHash = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendOriginal(ReadOnlySpan<byte> source)
    {
        var v = _workingHash;
        foreach (var b in source)
        {
            v = ((v << 5) + v) + b;
        }

        _workingHash = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState() =>
        HashingThrowHelper.ThrowIfAlgorithmAlreadyStarted(_started);
}
