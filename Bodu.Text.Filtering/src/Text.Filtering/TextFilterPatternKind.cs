// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterPatternKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Specifies the syntax used to interpret the text of a <see cref="TextFilterPattern" />.
/// </summary>
public enum TextFilterPatternKind
{
    /// <summary>
    /// The pattern is a glob-style wildcard matched against the whole value: <c>*</c> matches zero or more characters,
    /// <c>?</c> matches exactly one character, <c>[abc]</c> / <c>[a-z]</c> match one character from a set or range,
    /// <c>[!abc]</c> matches one character not in the set, <c>{a,b}</c> is an alternation expanded when the filter is
    /// built, and <c>\</c> escapes the following character.
    /// </summary>
    Wildcard = 0,

    /// <summary>
    /// The pattern is a .NET regular expression evaluated against the value.
    /// </summary>
    Regex = 1,
}
