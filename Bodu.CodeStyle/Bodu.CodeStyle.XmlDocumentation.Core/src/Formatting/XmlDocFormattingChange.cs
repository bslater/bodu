// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormattingChange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    /// <param name="kind">The category of change applied.</param>
    /// <param name="description">A short, human-readable description of the change.</param>
    public XmlDocFormattingChange(XmlDocFormatRangeKind kind, string description)
    {
        if (description is null) throw new ArgumentNullException(nameof(description));

        this.Kind = kind;
        this.Description = description;
    }

    /// <summary>
    /// Gets the category of change applied by the formatter.
    /// </summary>
    /// <returns>The change-kind enumeration value.</returns>
    public XmlDocFormatRangeKind Kind { get; }

    /// <summary>
    /// Gets a short, human-readable description of the change.
    /// </summary>
    /// <returns>The change description in US English.</returns>
    public string Description { get; }
}
