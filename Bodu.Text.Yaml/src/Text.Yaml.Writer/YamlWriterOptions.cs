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
    /// <summary>The configured indentation width; zero or less selects the default.</summary>
    private int _indentSize;

    /// <summary>The configured maximum nesting depth; zero or less selects the default.</summary>
    private int _maxDepth;

    /// <summary>The configured line-break sequence, or <see langword="null" /> for the default.</summary>
    private string? _newLine;

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
    /// Gets or sets the maximum container nesting depth permitted while writing.
    /// </summary>
    /// <value>The maximum depth. A value of zero or less selects the default of 64.</value>
    public int MaxDepth
    {
        readonly get => _maxDepth;
        set => _maxDepth = value;
    }

    /// <summary>
    /// Gets or sets the line-break sequence written at the end of each line.
    /// </summary>
    /// <value>The newline string; the default is a single line feed (<c>\n</c>).</value>
    public string? NewLine
    {
        readonly get => _newLine;
        set => _newLine = value;
    }

    /// <summary>
    /// Gets the effective indentation width, applying the default.
    /// </summary>
    /// <value>The indentation width used by the writer.</value>
    internal readonly int EffectiveIndentSize => _indentSize <= 0 ? 2 : _indentSize;

    /// <summary>
    /// Gets the effective maximum write depth, applying the default and the implementation ceiling.
    /// </summary>
    /// <value>The clamped maximum nesting depth used by the writer.</value>
    internal readonly int EffectiveMaxDepth =>
        _maxDepth <= 0 ? YamlLimits.DefaultMaxDepth : Math.Min(_maxDepth, YamlLimits.AbsoluteMaxDepth);

    /// <summary>
    /// Gets the effective newline sequence, applying the default.
    /// </summary>
    /// <value>The newline string used by the writer.</value>
    internal readonly string EffectiveNewLine => _newLine ?? "\n";

    /// <summary>The largest indentation width the writer accepts.</summary>
    private const int MaxIndentSize = 16;

    /// <summary>
    /// Validates the configured options, rejecting an unsupported newline string or an out-of-range indentation width.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The newline is not <see langword="null" />, a line feed, or a carriage-return line feed.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The indentation width is greater than 16.</exception>
    internal readonly void Validate()
    {
        if (_newLine is not (null or "\n" or "\r\n"))
            throw new ArgumentException(YamlResourceStrings.Arg_Invalid_YamlWriterNewLine, nameof(NewLine));

        if (_indentSize > MaxIndentSize)
            throw new ArgumentOutOfRangeException(nameof(IndentSize), _indentSize, YamlResourceStrings.Arg_OutOfRange_YamlWriterIndentSize);
    }
}
