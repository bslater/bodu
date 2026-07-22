// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerDefaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Specifies a set of default settings a <see cref="DelimitedSerializerOptions" /> can be initialized from.
/// </summary>
public enum DelimitedSerializerDefaults
{
    /// <summary>
    /// General-purpose defaults: property names are used verbatim and matched case-sensitively.
    /// </summary>
    General = 0,

    /// <summary>
    /// Web defaults: <c>snake_case</c> column names, matched case-insensitively.
    /// </summary>
    Web,
}
