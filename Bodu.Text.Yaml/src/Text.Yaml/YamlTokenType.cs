// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTokenType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Identifies the kind of token surfaced by <see cref="Bodu.Text.Yaml.Reader.Utf8YamlReader" /> as it traverses a YAML
/// document in document order.
/// </summary>
public enum YamlTokenType
{
    /// <summary>
    /// No token has been read. This is the state before the first read and after the final token.
    /// </summary>
    None = 0,

    /// <summary>
    /// The start of a mapping node. Subsequent tokens alternate property names and values until the end token.
    /// </summary>
    StartMapping = 1,

    /// <summary>
    /// The end of a mapping node.
    /// </summary>
    EndMapping = 2,

    /// <summary>
    /// The start of a sequence node. Subsequent tokens are its elements until the end token.
    /// </summary>
    StartSequence = 3,

    /// <summary>
    /// The end of a sequence node.
    /// </summary>
    EndSequence = 4,

    /// <summary>
    /// A mapping key. The key text is available through the reader's string accessor.
    /// </summary>
    PropertyName = 5,

    /// <summary>
    /// A null scalar value.
    /// </summary>
    Null = 6,

    /// <summary>
    /// A string scalar value.
    /// </summary>
    String = 7,

    /// <summary>
    /// An integer scalar value.
    /// </summary>
    Integer = 8,

    /// <summary>
    /// A floating-point scalar value.
    /// </summary>
    Float = 9,

    /// <summary>
    /// A boolean scalar value.
    /// </summary>
    Boolean = 10,
}
