// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitKnownAnswer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitKnownAnswer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Represents a single known-answer test vector for a <see cref="CheckDigitAlgorithm" />, pairing a body of
/// decimal digits with the expected check digit produced by the algorithm under test.
/// </summary>
public sealed record CheckDigitKnownAnswer
{
    /// <summary>
    /// Gets a short descriptive name used in test output to identify the vector.
    /// </summary>
    /// <returns>A non-empty descriptive label such as <c>"WikipediaExample"</c>.</returns>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the body digits (without the check digit) that the algorithm is expected to process.
    /// </summary>
    /// <returns>A string consisting exclusively of ASCII decimal digits <c>'0'</c> to <c>'9'</c>.</returns>
    public required string Body { get; init; }

    /// <summary>
    /// Gets the expected check digit produced by the algorithm for the <see cref="Body" />.
    /// </summary>
    /// <returns>An ASCII character in the range <c>'0'</c> to <c>'9'</c>.</returns>
    public required char ExpectedCheck { get; init; }
}
