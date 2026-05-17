// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDR.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes a 32-bit non-cryptographic hash using the BKDR polynomial rolling algorithm from Kernighan and Ritchie's
/// "The C Programming Language". This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte <c>c</c> the hash is updated as <c>hash = (hash * seed) + c</c>. The <see cref="Seed" />
/// multiplier must be one of the supported values (31, 131, 1313, 13131, 131313, 1313131, 13131313, 131313131,
/// 1313131313) and can be reassigned only while the algorithm has not yet consumed any input.
/// </para>
/// <para>
/// <strong>When to choose BKDR.</strong> BKDR is the polynomial rolling hash from K&amp;R's "The C Programming
/// Language" — a textbook choice for symbol tables, lexer keyword maps, and other small-string keying tasks. Pick it
/// when interoperating with code that already uses the K&amp;R formulation, or when the input is a short identifier and
/// avalanche quality matters less than predictable behavior. For modern hash-table workloads prefer
/// <see cref="Fnv1a32" /> (better avalanche on the same per-byte cost) or <see cref="MurmurHash3_32" /> (much better
/// distribution on inputs longer than ~16 bytes).
/// </para>
/// <para>
/// <strong>Output and lifecycle.</strong> Produces a 32-bit (4-byte) digest in little-endian byte order.
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> is non-destructive;
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset" /> returns the instance to its reconfigurable,
/// pre-input state. Instances are not thread-safe.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp"> using Bodu.IO.Hashing; using Bodu.IO.Hashing.Extensions; // Default seed (131); appropriate
/// for short identifier keys. var bkdr = new BKDR(); byte[] digest =
/// bkdr.ComputeHash(System.Text.Encoding.UTF8.GetBytes("Identifier")); </code>
/// </example>
/// </remarks>
public sealed class BKDR
    : NonCryptographicHashAlgorithm
{

    /// <summary>
    /// Represents the default seed value used by the <see cref="BKDR" /> hash algorithm.
    /// </summary>
    public const uint DefaultSeed = 131U;

    private const int HashLength = 4;

    private static readonly uint[] s_validSeedValues =
    {
        31U, 131U, 1313U, 13131U, 131313U, 1313131U, 13131313U, 131313131U, 1313131313U,
    };

    private uint _seed;
    private bool _started;
    private uint _workingHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="BKDR" /> class with the default seed of <c>131</c>.
    /// </summary>
    public BKDR()
        : this(DefaultSeed)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BKDR" /> class using the specified seed multiplier.
    /// </summary>
    /// <param name="seed">
    /// The seed multiplier applied to each byte. Must be one of the supported seed constants.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="seed" /> is not one of the supported values (31, 131, 1313, 13131, 131313, 1313131,
    /// 13131313, 131313131, 1313131313).
    /// </exception>
    public BKDR(uint seed)
        : base(HashLength)
    {
        ValidateSeed(seed);
        this._seed = seed;
        this._workingHash = seed;
    }

    /// <summary>
    /// Gets or sets the seed multiplier applied on each byte update.
    /// </summary>
    /// <value>The seed value. Must be one of the supported seed constants.</value>
    /// <exception cref="ArgumentException">
    /// The assigned value is not one of the supported seed values.
    /// </exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The algorithm has already consumed input and cannot be reconfigured until <see cref="Reset" /> is
    /// invoked.
    /// </exception>
    public uint Seed
    {
        get => this._seed;

        set
        {
            this.ThrowIfInvalidState();
            ValidateSeed(value);
            this._seed = value;
            this._workingHash = value;
        }
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
            return;

        var v = this._workingHash;
        var seed = this._seed;
        foreach (var b in source)
        {
            v = (v * seed) + b;
        }

        this._workingHash = v;
        this._started = true;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this._workingHash = this._seed;
        this._started = false;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, this._workingHash);

    private static void ValidateSeed(uint value)
    {
        if (Array.IndexOf(s_validSeedValues, value) == -1)
        {
            throw new ArgumentException(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    HashingResourceStrings.Arg_Invalid_BkdrSeedUnsupported,
                    value),
                nameof(value));
        }
    }

    /// <summary>
    /// Throws a <see cref="CryptographicUnexpectedOperationException" /> if the hash algorithm has already consumed
    /// input, indicating that the instance is in a non-configurable state.
    /// </summary>
    /// <remarks>
    /// Prevents reconfiguration of the seed once hashing has begun. The guard is cleared by <see cref="Reset" />.
    /// </remarks>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown when an attempt is made to modify the algorithm after it has begun consuming input.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState() =>
        HashingThrowHelper.ThrowIfAlgorithmAlreadyStarted(this._started);

}
