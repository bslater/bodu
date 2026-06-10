// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSpecVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Toml.Syntax;

/// <summary>
/// Identifies the version of the TOML specification whose grammar the parser enforces.
/// </summary>
public enum TomlSpecVersion
{
    /// <summary>
    /// TOML v1.0.0. The strict default.
    /// </summary>
    V1_0 = 0,

    /// <summary>
    /// TOML v1.1.0, which additionally accepts the <c>\e</c> and <c>\xHH</c> escapes, time values without seconds, and
    /// multi-line and trailing-comma inline tables.
    /// </summary>
    V1_1 = 1,
}
