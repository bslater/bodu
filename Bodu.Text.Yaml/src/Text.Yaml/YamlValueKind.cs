// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlValueKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Specifies the resolved kind of a YAML node, as surfaced by the document object models after implicit type resolution
/// has been applied to scalars.
/// </summary>
/// <remarks>
/// The kind reflects the resolved type of a node, not its source presentation. A scalar written as <c>"42"</c> in
/// double quotes resolves to <see cref="String" />, whereas the plain scalar <c>42</c> resolves to
/// <see cref="Integer" /> under the active <see cref="YamlSpecVersion" />. Collections are reported as
/// <see cref="Sequence" /> or <see cref="Mapping" /> regardless of whether they used block or flow style.
/// </remarks>
public enum YamlValueKind
{
    /// <summary>
    /// No value. Used as the default before a node has been read.
    /// </summary>
    None = 0,

    /// <summary>
    /// The YAML null value, written as <c>null</c>, <c>~</c>, or an empty scalar.
    /// </summary>
    Null = 1,

    /// <summary>
    /// A boolean value.
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// An integer value.
    /// </summary>
    Integer = 3,

    /// <summary>
    /// A floating-point value, including the infinity and not-a-number forms.
    /// </summary>
    Float = 4,

    /// <summary>
    /// A string value.
    /// </summary>
    String = 5,

    /// <summary>
    /// An ordered sequence of nodes, written in block or flow style.
    /// </summary>
    Sequence = 6,

    /// <summary>
    /// An unordered mapping of key nodes to value nodes, written in block or flow style.
    /// </summary>
    Mapping = 7,
}
