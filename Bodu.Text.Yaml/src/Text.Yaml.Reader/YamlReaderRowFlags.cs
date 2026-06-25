// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReaderRowFlags.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Records structural markers about how a parsed node row was produced, used during composition and when materializing
/// the document object model.
/// </summary>
[Flags]
internal enum YamlReaderRowFlags
{
    /// <summary>
    /// No marker.
    /// </summary>
    None = 0,

    /// <summary>
    /// The node was written in flow style (<c>[...]</c> or <c>{...}</c>) rather than block style.
    /// </summary>
    Flow = 1,

    /// <summary>
    /// The scalar carries backslash escape sequences that must be decoded when its string value is materialized.
    /// </summary>
    HasEscapes = 2,

    /// <summary>
    /// The node was produced by expanding a merge key (<c>&lt;&lt;</c>) and does not override existing keys.
    /// </summary>
    Merged = 4,

    /// <summary>
    /// The node carries an explicit anchor and is a candidate alias target.
    /// </summary>
    Anchored = 8,
}
