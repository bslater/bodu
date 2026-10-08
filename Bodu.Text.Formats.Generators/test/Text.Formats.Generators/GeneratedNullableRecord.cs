// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratedNullableRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// A delimited record POCO whose <c>DelimitedFactory</c> is emitted by the source generator at build time, covering a
/// nullable member of each value-type scalar kind the generator maps, beside an identifier column.
/// </summary>
[DelimitedRecord]
public sealed partial class GeneratedNullableRecord
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    /// <value>The identifier.</value>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the optional flag.
    /// </summary>
    /// <value>The flag, or <see langword="null" />.</value>
    public bool? Flag { get; set; }

    /// <summary>
    /// Gets or sets the optional letter.
    /// </summary>
    /// <value>The letter, or <see langword="null" />.</value>
    public char? Letter { get; set; }

    /// <summary>
    /// Gets or sets the optional count.
    /// </summary>
    /// <value>The count, or <see langword="null" />.</value>
    public int? Count { get; set; }

    /// <summary>
    /// Gets or sets the optional amount.
    /// </summary>
    /// <value>The amount, or <see langword="null" />.</value>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Gets or sets the optional key.
    /// </summary>
    /// <value>The key, or <see langword="null" />.</value>
    public Guid? Key { get; set; }

    /// <summary>
    /// Gets or sets the optional instant.
    /// </summary>
    /// <value>The instant, or <see langword="null" />.</value>
    public DateTime? At { get; set; }

    /// <summary>
    /// Gets or sets the optional instant with its offset.
    /// </summary>
    /// <value>The instant and offset, or <see langword="null" />.</value>
    public DateTimeOffset? AtOffset { get; set; }

    /// <summary>
    /// Gets or sets the optional duration.
    /// </summary>
    /// <value>The duration, or <see langword="null" />.</value>
    public TimeSpan? Span { get; set; }

    /// <summary>
    /// Gets or sets the optional kind.
    /// </summary>
    /// <value>The kind, or <see langword="null" />.</value>
    public PersonKind? Kind { get; set; }
}
