// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SentenceCaseOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

/// <summary>
/// Tunes the behaviour of <see cref="StringExtensions.ToSentenceCase(string, SentenceCaseOptions)" /> beyond
/// the plain capitalise-first-letter-of-each-sentence default.
/// </summary>
[Flags]
public enum SentenceCaseOptions
{
    /// <summary>
    /// Default behaviour: capitalise the first letter of each sentence and lower-case every other letter.
    /// </summary>
    None = 0,

    /// <summary>
    /// Preserve fully-uppercase words as acronyms rather than down-casing them within a sentence.
    /// </summary>
    PreserveAcronyms = 1,
}
