// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringFormattingOperation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Tests.KnownAnswers;

/// <summary>
/// Identifies the casing / formatting operation under test for a
/// <see cref="StringFormattingKnownAnswer" /> scenario.
/// </summary>
public enum StringFormattingOperation
{
    /// <summary>Capitalise the first letter of every word.</summary>
    TitleCase,

    /// <summary>Capitalise the first letter of every sentence; lower-case everything else.</summary>
    SentenceCase,

    /// <summary>Convert to <c>camelCase</c>.</summary>
    CamelCase,

    /// <summary>Convert to <c>PascalCase</c>.</summary>
    PascalCase,

    /// <summary>Convert to <c>snake_case</c>.</summary>
    SnakeCase,

    /// <summary>Convert to <c>kebab-case</c>.</summary>
    KebabCase,

    /// <summary>Convert to <c>Train-Case</c>.</summary>
    TrainCase,

    /// <summary>Convert to <c>CONSTANT_CASE</c>.</summary>
    ConstantCase,

    /// <summary>Convert to <c>dot.case</c>.</summary>
    DotCase,

    /// <summary>Convert to a URL-friendly slug.</summary>
    Slug,

    /// <summary>Collapse and trim whitespace.</summary>
    NormalizeWhitespace,
}
