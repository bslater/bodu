// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSpecVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Identifies the version of the TOML specification the reader enforces. The version controls a small number of grammar
/// features that TOML v1.1.0 added relative to TOML v1.0.0.
/// </summary>
/// <remarks>
/// <para>
/// The default is <see cref="V1_0" />, which is the most widely deployed released specification. Selecting
/// <see cref="V1_1" /> additionally accepts the <c>\e</c> and <c>\xHH</c> string escapes, time values without seconds,
/// and multi-line and trailing-comma inline tables.
/// </para>
/// <para>
/// The version affects only parsing. The writer always emits output that is valid under both versions, so it does not
/// take a version.
/// </para>
/// </remarks>
public enum TomlSpecVersion
{
    /// <summary>
    /// The TOML v1.0.0 specification. Inline tables must be single-line and free of trailing commas, time values must
    /// include seconds, and the <c>\e</c> and <c>\xHH</c> escapes are rejected.
    /// </summary>
    V1_0 = 0,

    /// <summary>
    /// The TOML v1.1.0 specification. Adds the <c>\e</c> and <c>\xHH</c> escapes, optional seconds in time values, and
    /// multi-line and trailing-comma inline tables.
    /// </summary>
    V1_1 = 1,
}
