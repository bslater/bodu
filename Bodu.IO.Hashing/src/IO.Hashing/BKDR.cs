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
/// Computes a 32-bit non-cryptographic hash using the BKDR polynomial rolling algorithm from Kernighan and
/// Ritchie's "The C Programming Language". This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// For each input byte <c>c</c> the hash is updated as <c>hash = (hash * seed) + c</c>. The <see cref="Seed" />
/// multiplier must be one of the supported values (31, 131, 1313, 13131, 131313, 1313131, 13131313, 131313131,
/// 1313131313) and can be reassigned only while the algorithm has not yet consumed any input.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class BKDR
    : NonCryptographicHashAlgorithm
{
    /// <summary>
    /// Represents the default seed value used by the <see cref="BKDR" /> hash algorithm.
    /// </summary>
    public const uint DefaultSeed = 131U;

    private const int HashLength = 4;
    private const string ReconfigurationNotAllowed =
        "The algorithm is already in use and cannot be reconfigured after computation has started.";

    private static readonly uint[] ValidSeedValues =
    {
        31U, 131U, 1313U, 13131U, 131313U, 1313131U, 13131313U, 131313131U, 1313131313U,
    };

    private uint _seed;
    private uint _workingHash;
    private bool _started;

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
        _seed = seed;
        _workingHash = seed;
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
        get => _seed;

        set
        {
            ThrowIfInvalidState();
            ValidateSeed(value);
            _seed = value;
            _workingHash = value;
        }
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        if (source.Length == 0)
            return;

        uint v = _workingHash;
        uint seed = _seed;
        foreach (byte b in source)
        {
            v = (v * seed) + b;
        }

        _workingHash = v;
        _started = true;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _workingHash = _seed;
        _started = false;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt32BigEndian(destination, _workingHash);

    private static void ValidateSeed(uint value)
    {
        if (Array.IndexOf(ValidSeedValues, value) == -1)
        {
            throw new ArgumentException(
                $"The value {value} is not a supported BKDR seed.",
                nameof(value));
        }
    }

    /// <summary>
    /// Throws a <see cref="CryptographicUnexpectedOperationException" /> if the hash algorithm has already
    /// consumed input, indicating that the instance is in a non-configurable state.
    /// </summary>
    /// <remarks>
    /// Prevents reconfiguration of the seed once hashing has begun. The guard is cleared by
    /// <see cref="Reset" />.
    /// </remarks>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown when an attempt is made to modify the algorithm after it has begun consuming input.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfInvalidState()
    {
        if (_started)
            throw new CryptographicUnexpectedOperationException(ReconfigurationNotAllowed);
    }
}
