// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashKnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Declares the expected digests produced by an algorithm variant when hashing the common shared inputs, plus any
/// algorithm-specific extension vectors.
/// </summary>
/// <remarks>
/// <para>
/// Each typed slot (<see cref="Empty" />, <see cref="Abc" />, <see cref="QuickBrownFox" />, <see cref="Zeros16" />,
/// <see cref="Sequential0To255" />) carries the hex-encoded expected digest for the corresponding named input declared
/// in <see cref="NonCryptographicHashSharedInputs" />. A <see langword="null" /> slot signals that no published value
/// is available for the algorithm under test and the harness skips that particular vector.
/// </para>
/// <para>
/// Vectors that fall outside the shared-input set — for example, the RevEng catalogue <c>"123456789"</c> CRC check —
/// live on the <see cref="Additional" /> list and are driven by the same data-driven test harness, avoiding bespoke
/// <c>[DataRow]</c> definitions.
/// </para>
/// </remarks>
public sealed record NonCryptographicHashKnownAnswers
{

    /// <summary>
    /// Gets the expected hex digest for the ASCII input <c>"ABC"</c>, or <see langword="null" /> when unspecified.
    /// </summary>
    public string? Abc { get; init; }

    /// <summary>
    /// Gets algorithm-specific known-answer vectors that fall outside the shared-input set.
    /// </summary>
    /// <value>A list of named vectors. Defaults to an empty list.</value>
    public IReadOnlyList<NonCryptographicHashKnownAnswer> Additional { get; init; } = [];

    /// <summary>
    /// Gets the expected hex digest for an empty input, or <see langword="null" /> when unspecified.
    /// </summary>
    public string? Empty { get; init; }

    /// <summary>
    /// Gets the expected hex digest for the ASCII input <c>"The quick brown fox jumps over the lazy dog"</c>, or
    /// <see langword="null" /> when unspecified.
    /// </summary>
    public string? QuickBrownFox { get; init; }

    /// <summary>
    /// Gets the expected hex digest for the 255-byte sequence <c>0x00, 0x01, …, 0xFE</c>, or <see langword="null" />
    /// when unspecified.
    /// </summary>
    public string? Sequential0To255 { get; init; }

    /// <summary>
    /// Gets the expected hex digest for sixteen zero bytes, or <see langword="null" /> when unspecified.
    /// </summary>
    public string? Zeros16 { get; init; }

}
