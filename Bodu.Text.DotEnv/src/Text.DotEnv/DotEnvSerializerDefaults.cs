// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerDefaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Specifies a set of default settings a <see cref="DotEnvSerializerOptions" /> can be initialized from.
/// </summary>
public enum DotEnvSerializerDefaults
{
    /// <summary>
    /// General-purpose defaults: property names are used verbatim and matched case-sensitively.
    /// </summary>
    General = 0,

    /// <summary>
    /// Web defaults: <c>SCREAMING_SNAKE_CASE</c> property names, matched case-insensitively.
    /// </summary>
    Web,
}
