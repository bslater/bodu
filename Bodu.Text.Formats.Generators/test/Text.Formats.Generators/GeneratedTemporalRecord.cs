// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratedTemporalRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// A delimited record POCO whose <c>DelimitedFactory</c> is emitted by the source generator at build time, covering
/// each temporal type the generator maps, as a value and as a nullable value.
/// </summary>
[DelimitedRecord]
public sealed partial class GeneratedTemporalRecord
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
    /// Gets or sets the optional instant.
    /// </summary>
    /// <value>The instant, or <see langword="null" />.</value>
    public DateTime? MaybeAt { get; set; }

    /// <summary>
    /// Gets or sets the optional instant with its offset.
    /// </summary>
    /// <value>The instant and offset, or <see langword="null" />.</value>
    public DateTimeOffset? MaybeAtOffset { get; set; }

    /// <summary>
    /// Gets or sets the optional duration.
    /// </summary>
    /// <value>The duration, or <see langword="null" />.</value>
    public TimeSpan? MaybeSpan { get; set; }
}
