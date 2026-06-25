// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Writer;

/// <summary>
/// Provides options that configure how <see cref="Utf8YamlWriter" /> emits YAML.
/// </summary>
public struct YamlWriterOptions
{
    private int _indentSize;

    /// <summary>
    /// Gets or sets the number of spaces used for each level of block indentation.
    /// </summary>
    /// <value>The indentation width. A value of zero or less selects the default of two.</value>
    public int IndentSize
    {
        readonly get => _indentSize;
        set => _indentSize = value;
    }

    /// <summary>
    /// Gets the effective indentation width, applying the default.
    /// </summary>
    /// <value>The indentation width used by the writer.</value>
    internal readonly int EffectiveIndentSize => _indentSize <= 0 ? 2 : _indentSize;
}
