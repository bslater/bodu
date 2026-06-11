// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerDefaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Specifies a base set of defaults applied when a <see cref="BencodeSerializerOptions" /> instance is created for a
/// particular usage scenario.
/// </summary>
public enum BencodeSerializerDefaults
{
    /// <summary>
    /// Applies general-purpose defaults: member names are used unchanged and property-name matching is performed with
    /// the default case sensitivity.
    /// </summary>
    General,

    /// <summary>
    /// Applies defaults suited to web scenarios: camel-case property naming and case-insensitive property-name
    /// matching.
    /// </summary>
    Web,
}
