// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bernstein.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using Daniel J. Bernstein's djb2 algorithm, optionally using the
/// XOR-modified variant. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The default algorithm computes <c>hash = (hash * 33) + c</c> for each input byte <c>c</c>. Setting
/// <see cref="UseModifiedAlgorithm" /> via the constructor selects the XOR-modified form,
/// <c>hash = (hash * 33) ^ c</c>, which may give better distribution in some hash-table workloads.
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

    private readonly uint _initialValue;
    private readonly bool _useModified;
    private uint _workingHash;

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
        this._initialValue = initialValue;
        this._useModified = useModifiedAlgorithm;
        this._workingHash = initialValue;
    }

    /// <summary>
    /// Gets the initial seed value applied to the running hash accumulator.
    /// </summary>
    public uint InitialValue => this._initialValue;

    /// <summary>
    /// Gets a value indicating whether the XOR-modified form of the algorithm is in use.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when each update performs <c>(hash * 33) ^ c</c>; <see langword="false" /> when
    /// it performs <c>(hash * 33) + c</c>.
    /// </value>
    public bool UseModifiedAlgorithm => this._useModified;

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (this._useModified)
            this.AppendModified(source);
        else
            this.AppendOriginal(source);
    }

    /// <inheritdoc />
    public override void Reset() => this._workingHash = this._initialValue;

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, this._workingHash);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendOriginal(ReadOnlySpan<byte> source)
    {
        uint v = this._workingHash;
        foreach (byte b in source)
        {
            v = ((v << 5) + v) + b;
        }

        this._workingHash = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendModified(ReadOnlySpan<byte> source)
    {
        uint v = this._workingHash;
        foreach (byte b in source)
        {
            v = ((v << 5) + v) ^ b;
        }

        this._workingHash = v;
    }
}
