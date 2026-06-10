// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatNullHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies how a serializer treats a property whose value is <see langword="null" /> when writing.
/// </summary>
/// <remarks>
/// Several formats — including TOML and Bencode — have no representation for a null value, so the default is to omit a
/// null-valued property entirely rather than emit a placeholder.
/// </remarks>
public enum FormatNullHandling
{
    /// <summary>
    /// Omit a property whose value is <see langword="null" /> from the output. This is the default.
    /// </summary>
    IgnoreOnWrite,

    /// <summary>
    /// Write a null value for the property. Supported only by formats that can represent null.
    /// </summary>
    Include,
}
