// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowingNonCryptographicHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// A test-oriented non-cryptographic hash algorithm whose
/// <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" /> always throws, used to drive the
/// exception-safe <c>catch { return false; }</c> branches of <c>TryVerifyHash</c> and
/// <c>TryVerifyHashAsync</c> overloads where the input is supplied directly (rather than via a stream that can
/// itself be made to fault).
/// </summary>
/// <remarks>
/// <see cref="Reset" /> and <see cref="GetCurrentHashCore" /> are deliberately non-throwing so the failure point
/// is consistently <see cref="Append(ReadOnlySpan{byte})" />, which is the first call sites that operate on user
/// input.
/// </remarks>
public sealed class ThrowingNonCryptographicHashAlgorithm
    : NonCryptographicHashAlgorithm
{

    /// <summary>
    /// Initialises a new instance of <see cref="ThrowingNonCryptographicHashAlgorithm" /> with a 4-byte output
    /// matching <see cref="MonitoringNonCryptographicHashAlgorithm" />.
    /// </summary>
    public ThrowingNonCryptographicHashAlgorithm()
        : base(hashLengthInBytes: sizeof(uint))
    {
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source) =>
        throw new InvalidOperationException("Simulated algorithm failure during Append.");

    /// <inheritdoc />
    public override void Reset()
    {
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
    }

}
