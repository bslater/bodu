// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Enumerates the value kinds an INI node or element can represent.
/// </summary>
/// <remarks>
/// INI models a two-level object-of-objects: the document object maps section names to section objects, and each
/// section object maps key names to <see cref="String" /> values. There is no array, number, or boolean kind.
/// </remarks>
public enum IniValueKind
{
    /// <summary>
    /// An object — the document (section names) or a section (key names).
    /// </summary>
    Object = 0,

    /// <summary>
    /// A string value.
    /// </summary>
    String,
}
