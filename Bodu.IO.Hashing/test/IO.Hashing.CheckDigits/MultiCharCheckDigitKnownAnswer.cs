// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitKnownAnswer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Represents a single known-answer test vector for a <see cref="MultiCharCheckDigitAlgorithm" />, pairing a body
/// of alphanumeric characters with the expected multi-character check code produced by the algorithm under test.
/// </summary>
public sealed record MultiCharCheckDigitKnownAnswer : IKat
{

    /// <summary>
    /// Gets the body characters (without the trailing check code) that the algorithm is expected to process.
    /// </summary>
    /// <returns>A non-null string drawn from the algorithm's declared input alphabet.</returns>
    public required string Body { get; init; }

    /// <summary>
    /// Gets the expected check code produced by the algorithm for the <see cref="Body" />.
    /// </summary>
    /// <returns>A string of ASCII decimal digits whose length matches the algorithm's <c>CheckLength</c>.</returns>
    public required string ExpectedCheck { get; init; }
    /// <summary>
    /// Gets a short descriptive name used in test output to identify the vector.
    /// </summary>
    /// <returns>A non-empty descriptive label such as <c>"WikipediaIbanGB"</c>.</returns>
    public required string Name { get; init; }

}
