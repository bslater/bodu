// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatResult.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Represents the outcome of a call to <see cref="XmlDocFormatter.FormatTrivia(string, XmlDocFormatContext, XmlDocFormatOptions)" />.
/// </summary>
public sealed class XmlDocFormatResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocFormatResult" /> class.
    /// </summary>
    /// <param name="changed">A value indicating whether the formatter produced different output than the input.</param>
    /// <param name="formattedText">The formatted documentation comment text.</param>
    /// <param name="changes">The ordered list of changes applied by the formatter.</param>
    public XmlDocFormatResult(bool changed, string formattedText, ImmutableArray<XmlDocFormattingChange> changes)
    {
        if (formattedText is null) throw new ArgumentNullException(nameof(formattedText));
        if (changes.IsDefault) throw new ArgumentException(XmlDocResourceStrings.Arg_Invalid_ChangesNotInitialised, nameof(changes));

        this.Changed = changed;
        this.FormattedText = formattedText;
        this.Changes = changes;
    }

    /// <summary>
    /// Gets a value indicating whether the formatter produced output that differs from its input.
    /// </summary>
    /// <returns><see langword="true" /> when the formatted text differs from the input; otherwise <see langword="false" />.</returns>
    public bool Changed { get; }

    /// <summary>
    /// Gets the formatted documentation comment text, including the documentation prefix and base indent on
    /// every line.
    /// </summary>
    /// <returns>The complete formatted trivia text.</returns>
    public string FormattedText { get; }

    /// <summary>
    /// Gets the ordered list of changes applied by the formatter.
    /// </summary>
    /// <returns>The immutable array of formatting changes; empty when <see cref="Changed" /> is <see langword="false" />.</returns>
    public ImmutableArray<XmlDocFormattingChange> Changes { get; }
}
