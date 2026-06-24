// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmSpecification.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Describes the expected observable properties of a <see cref="MultiCharCheckDigitAlgorithm" /> implementation for use
/// in behavioural and known-answer tests.
/// </summary>
/// <remarks>
/// Instances are supplied by derived test classes via
/// <see cref="MultiCharCheckDigitAlgorithmTests{TTest, TAlgorithm}.GetSpecification" /> and drive assertions common
/// across every algorithm sharing the multi-character base.
/// </remarks>
public sealed record MultiCharCheckDigitAlgorithmSpecification
{

    /// <summary>
    /// Gets the expected <see cref="MultiCharCheckDigitAlgorithm.AlgorithmName" /> string.
    /// </summary>
    /// <value>The canonical algorithm name, e.g. <c>"ISO 7064 MOD 97-10"</c>.</value>
    public required string AlgorithmName { get; init; }

    /// <summary>
    /// Gets the expected <see cref="MultiCharCheckDigitAlgorithm.CheckLength" /> value.
    /// </summary>
    /// <value>The number of trailing check characters the algorithm emits.</value>
    public required int CheckLength { get; init; }

    /// <summary>
    /// Gets the check code expected for an empty body, or <see langword="null" /> to indicate that the algorithm throws
    /// on empty input.
    /// </summary>
    /// <value>A string of length <see cref="CheckLength" />, or <see langword="null" />.</value>
    public string? EmptyCheckDigits { get; init; }

    /// <summary>
    /// Gets the expected <see cref="MultiCharCheckDigitAlgorithm.InputAlphabet" /> value.
    /// </summary>
    /// <value>The declared input alphabet for the algorithm under test.</value>
    public required CheckDigitInputAlphabet InputAlphabet { get; init; }

    /// <summary>
    /// Gets the known-answer vectors to exercise.
    /// </summary>
    /// <value>A non-empty list of <see cref="MultiCharCheckDigitKnownAnswer" /> entries.</value>
    public required IReadOnlyList<MultiCharCheckDigitKnownAnswer> KnownAnswers { get; init; }

}
