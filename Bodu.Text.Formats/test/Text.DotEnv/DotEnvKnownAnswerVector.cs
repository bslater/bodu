// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvKnownAnswerVector.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Represents a positive Known Answer Test (KAT) vector for the DotEnv format. A vector pairs a raw source string
/// with the expected parsed state — an ordered sequence of key/value entries — so that data-driven tests can verify
/// <see cref="DotEnv" /> against the dotenv community specification and common-practice inputs.
/// </summary>
/// <param name="Description">A short human-readable label identifying the vector.</param>
/// <param name="Input">The raw dotenv source text to parse.</param>
/// <param name="ExpectedEntries">
/// The expected key/value pairs in source order. Each tuple holds the key and the resolved value that
/// <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> should produce after quote stripping, escape processing, and
/// whitespace trimming.
/// </param>
/// <param name="Options">
/// The parse options to apply. <see langword="null" /> indicates that <see cref="DotEnvParseOptions.Default" />
/// should be used.
/// </param>
/// <param name="Source">The citation for the vector's origin (specification clause, reference implementation, etc.).</param>
public sealed record DotEnvKnownAnswerVector(
    string Description,
    string Input,
    (string Key, string Value)[] ExpectedEntries,
    DotEnvParseOptions? Options = null,
    string? Source = null)
{
    /// <summary>
    /// Returns the human-readable label, used by MSTest's <c>[DynamicData]</c> infrastructure when generating
    /// test names so each row is identifiable in failure output.
    /// </summary>
    /// <returns>The description, optionally suffixed with the citation in square brackets.</returns>
    public override string ToString() =>
        Source is null ? Description : $"{Description} [{Source}]";
}
