// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Enumerates the value kinds a DotEnv node or element can represent.
/// </summary>
/// <remarks>
/// DotEnv models only a flat object of string-valued keys, so the value model is limited to the document
/// <see cref="Object" /> and the <see cref="String" /> scalar. There is no array, number, boolean, or null kind.
/// </remarks>
public enum DotEnvValueKind
{
    /// <summary>
    /// An object mapping key names to string values.
    /// </summary>
    Object = 0,

    /// <summary>
    /// A string value.
    /// </summary>
    String,
}
