// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlMergeKeyBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Specifies how the reader treats the YAML merge key (<c>&lt;&lt;</c>).
/// </summary>
/// <remarks>
/// The merge key is a YAML 1.1 working-draft feature and is not part of the YAML 1.2 core schema. It is supported as an
/// opt-in compatibility convenience; the default is <see cref="Expand" /> to match common configuration usage, but it
/// can be disabled so that <c>&lt;&lt;</c> is treated as an ordinary key.
/// </remarks>
public enum YamlMergeKeyBehavior
{
    /// <summary>
    /// Expand a <c>&lt;&lt;</c> entry by merging the referenced mapping's keys, with existing keys taking precedence.
    /// This is the default.
    /// </summary>
    Expand = 0,

    /// <summary>
    /// Disable merge-key handling; a <c>&lt;&lt;</c> entry is retained as an ordinary mapping key.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// Retain a <c>&lt;&lt;</c> entry as an ordinary mapping key without expanding it. Equivalent to
    /// <see cref="Disabled" /> in the produced tree, expressing the intent to preserve rather than process the key.
    /// </summary>
    PreserveAsNormalKey = 2,
}
