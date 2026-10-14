// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SentenceCaseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Tunes the behavior of <see cref="StringExtensions.ToSentenceCase(string, SentenceCaseOptions)" /> beyond the plain
/// capitalize-first-letter-of-each-sentence default.
/// </summary>
[Flags]
public enum SentenceCaseOptions
{
    /// <summary>
    /// Default behavior: capitalize the first letter of each sentence and lower-case every other letter.
    /// </summary>
    None = 0,

    /// <summary>
    /// Preserve fully-uppercase words as acronyms rather than down-casing them within a sentence.
    /// </summary>
    PreserveAcronyms = 1,
}
