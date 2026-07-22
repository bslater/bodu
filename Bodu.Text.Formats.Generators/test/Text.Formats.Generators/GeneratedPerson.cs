// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratedPerson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;
using Bodu.Text.Serialization;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// A delimited record POCO whose <c>DelimitedFactory</c> is emitted by the source generator at build time, covering
/// string, numeric, nullable, enum, and renamed columns plus an ignored member.
/// </summary>
[DelimitedRecord]
public sealed partial class GeneratedPerson
{
    /// <summary>
    /// Gets or sets the person's name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the person's age.
    /// </summary>
    /// <value>The age.</value>
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets the email address, mapped to the <c>email_address</c> column.
    /// </summary>
    /// <value>The email address.</value>
    [PropertyName("email_address")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the optional score.
    /// </summary>
    /// <value>The score, or <see langword="null" />.</value>
    public double? Score { get; set; }

    /// <summary>
    /// Gets or sets the person's kind.
    /// </summary>
    /// <value>The kind.</value>
    public PersonKind Kind { get; set; }

    /// <summary>
    /// Gets or sets a member excluded from the factory.
    /// </summary>
    /// <value>The secret.</value>
    [Bodu.Text.Serialization.Ignore]
    public string? Secret { get; set; }
}
