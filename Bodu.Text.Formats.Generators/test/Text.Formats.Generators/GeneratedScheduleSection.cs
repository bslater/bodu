// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratedScheduleSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// An INI section POCO whose <c>IniFactory</c> is emitted by the source generator at build time, covering each temporal
/// type the generator maps, a nullable value and an enum, so that the INI factory can be held to INI's own binder.
/// </summary>
[IniSection]
public sealed partial class GeneratedScheduleSection
{
    /// <summary>
    /// Gets or sets the instant.
    /// </summary>
    /// <value>The instant.</value>
    public DateTime At { get; set; }

    /// <summary>
    /// Gets or sets the instant with its offset.
    /// </summary>
    /// <value>The instant and offset.</value>
    public DateTimeOffset AtOffset { get; set; }

    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    /// <value>The duration.</value>
    public TimeSpan Span { get; set; }

    /// <summary>
    /// Gets or sets the optional count.
    /// </summary>
    /// <value>The count, or <see langword="null" />.</value>
    public int? Count { get; set; }

    /// <summary>
    /// Gets or sets the kind.
    /// </summary>
    /// <value>The kind.</value>
    public PersonKind Kind { get; set; }
}
