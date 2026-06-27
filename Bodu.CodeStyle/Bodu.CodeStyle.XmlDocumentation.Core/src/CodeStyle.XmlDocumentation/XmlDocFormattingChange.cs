// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormattingChange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Describes a single layout change applied by <see cref="XmlDocFormatter" />.
/// </summary>
public sealed class XmlDocFormattingChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocFormattingChange" /> class.
    /// </summary>
    /// <param name="tagName">
    /// The XML doc tag the change is attributed to, or <see langword="null" /> when the change applies to prose,
    /// prefix, or other content outside any tag scope (cross-cutting).
    /// </param>
    /// <param name="kind">The category of change applied.</param>
    /// <param name="description">A short, human-readable description of the change.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="description" /> is <see langword="null" />.
    /// </exception>
    public XmlDocFormattingChange(string? tagName, XmlDocFormatRangeKind kind, string description)
    {
        if (description is null) throw new ArgumentNullException(nameof(description));

        this.TagName = tagName;
        this.Kind = kind;
        this.Description = description;
    }

    /// <summary>
    /// Gets the XML doc tag the change is attributed to, when applicable.
    /// </summary>
    /// <value>
    /// The tag name (without angle brackets) such as <c>"summary"</c> or <c>"param"</c>, or <see langword="null" />
    /// when the change applies outside any tag scope (line prefix, indent, prose between tags).
    /// </value>
    public string? TagName { get; }

    /// <summary>
    /// Gets the category of change applied by the formatter.
    /// </summary>
    /// <value>The change-kind enumeration value.</value>
    public XmlDocFormatRangeKind Kind { get; }

    /// <summary>
    /// Gets a short, human-readable description of the change.
    /// </summary>
    /// <value>The change description in US English.</value>
    public string Description { get; }
}
