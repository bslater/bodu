// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Enumerates the token kinds a <see cref="Bodu.Text.Ini.Reader.Utf8IniReader" /> reports as it advances through an INI
/// document in source order.
/// </summary>
/// <remarks>
/// The source-order reader surfaces the document's physical structure: a <see cref="SectionHeader" /> begins a section,
/// each entry surfaces a <see cref="PropertyName" /> followed by a <see cref="String" /> value, and comment lines
/// appear as <see cref="Comment" /> tokens. Keys that precede the first section header belong to the global section.
/// </remarks>
public enum IniTokenType
{
    /// <summary>
    /// No token has been read, or the reader has reached the end of the document.
    /// </summary>
    None = 0,

    /// <summary>
    /// A section header (<c>[name]</c>).
    /// </summary>
    SectionHeader,

    /// <summary>
    /// A key name, emitted before its value.
    /// </summary>
    PropertyName,

    /// <summary>
    /// A string value.
    /// </summary>
    String,

    /// <summary>
    /// A comment line.
    /// </summary>
    Comment,
}
