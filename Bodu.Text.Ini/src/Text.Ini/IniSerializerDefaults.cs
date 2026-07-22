// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerDefaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Specifies a set of default settings an <see cref="IniSerializerOptions" /> can be initialized from.
/// </summary>
public enum IniSerializerDefaults
{
    /// <summary>
    /// General-purpose defaults: duplicate sections merge and the last duplicate key wins, matching the permissive
    /// Windows profile dialect.
    /// </summary>
    General = 0,

    /// <summary>
    /// Strict defaults: a duplicate section or duplicate key is rejected, matching Python's <c>configparser</c> strict
    /// mode.
    /// </summary>
    Strict,
}
