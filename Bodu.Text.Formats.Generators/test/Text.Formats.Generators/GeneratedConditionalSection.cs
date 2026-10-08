// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratedConditionalSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;
using Bodu.Text.Serialization;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// An INI section POCO whose <c>IniFactory</c> is emitted by the source generator at build time, covering members with
/// each write-time <see cref="IgnoreCondition" /> of their own and members without one, of value and reference types.
/// </summary>
[IniSection]
public sealed partial class GeneratedConditionalSection
{
    /// <summary>
    /// Gets or sets an optional number with no condition of its own.
    /// </summary>
    /// <value>The number, or <see langword="null" />.</value>
    public int? Plain { get; set; }

    /// <summary>
    /// Gets or sets an optional note with no condition of its own.
    /// </summary>
    /// <value>The note, or <see langword="null" />.</value>
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets an optional number that is written even when it is <see langword="null" />.
    /// </summary>
    /// <value>The number, or <see langword="null" />.</value>
    [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.Never)]
    public int? Kept { get; set; }

    /// <summary>
    /// Gets or sets a number that is left out when it is zero.
    /// </summary>
    /// <value>The number.</value>
    [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.WhenWritingDefault)]
    public int Zero { get; set; }

    /// <summary>
    /// Gets or sets an optional number that is left out when it is <see langword="null" /> or zero.
    /// </summary>
    /// <value>The number, or <see langword="null" />.</value>
    [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.WhenWritingDefault)]
    public int? MaybeZero { get; set; }

    /// <summary>
    /// Gets or sets an optional text that is left out only when it is <see langword="null" />, a text's default.
    /// </summary>
    /// <value>The text, or <see langword="null" />.</value>
    [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.WhenWritingDefault)]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets a number that a <see langword="null" /> condition never leaves out, since it cannot be
    /// <see langword="null" />.
    /// </summary>
    /// <value>The number.</value>
    [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.WhenWritingNull)]
    public int Port { get; set; }
}
