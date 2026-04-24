// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bernstein.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// <see cref="UseModifiedAlgorithm" /> selects the XOR-modified form, <c>hash = (hash * 33) ^ c</c>, which
/// may give better distribution in some hash-table workloads.
/// </para>
/// <para>
/// Both <see cref="InitialValue" /> and <see cref="UseModifiedAlgorithm" /> are reconfigurable only while the
/// algorithm has not yet consumed input. <see cref="Reset" /> returns the instance to the reconfigurable
/// state.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Bernstein
    : NonCryptographicHashAlgorithm
{
    /// <summary>
    /// The default initial value used to seed the hash algorithm.
    /// </summary>
    public const uint DefaultInitialValue = 5381U;

    private const int HashLength = 4;
    private const string ReconfigurationNotAllowed =
        "The algorithm is already in use and cannot be reconfigured after computation has started.";

    private uint _initialValue;
    private bool _useModified;
    private uint _workingHash;
    private bool _started;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bernstein" /> class with the canonical djb2 seed
    /// (<see cref="DefaultInitialValue" />) and the original addition form of the algorithm.
    /// </summary>
    public Bernstein()
        : this(DefaultInitialValue, useModifiedAlgorithm: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Bernstein" /> class using the specified initial seed value
    /// and algorithm variant.
    /// </summary>
    /// <param name="initialValue">The initial seed applied to the running hash accumulator.</param>
    /// <param name="useModifiedAlgorithm">
    /// <see langword="true" /> to use the XOR-modified form <c>hash = (hash * 33) ^ c</c>;
    /// <see langword="false" /> to use the original <c>hash = (hash * 33) + c</c>.
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
    /// The algorithm has already consumed input and cannot be reconfigured until <see cref="Reset" /> is
    /// invoked.
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
    /// <see langword="true" /> when each update performs <c>(hash * 33) ^ c</c>; <see langword="false" /> when
    /// it performs <c>(hash * 33) + c</c>.
    /// </value>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The algorithm has already consumed input and cannot be reconfigured until <see cref="Reset" /> is
    /// invoked.
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
    private void AppendOriginal(ReadOnlySpan<byte> source)
    {
        uint v = _workingHash;
        foreach (byte b in source)
        {
            v = ((v << 5) + v) + b;
        }

        _workingHash = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendModified(ReadOnlySpan<byte> source)
    {
        uint v = _workingHash;
        foreach (byte b in source)
        {
            v = ((v << 5) + v) ^ b;
        }

        _workingHash = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (_started)
            throw new CryptographicUnexpectedOperationException(ReconfigurationNotAllowed);
    }
}
