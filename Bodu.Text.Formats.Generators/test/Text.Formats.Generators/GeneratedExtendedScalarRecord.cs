// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GeneratedExtendedScalarRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

using Bodu.Text.Delimited;

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// A delimited record POCO whose <c>DelimitedFactory</c> is emitted by the source generator at build time, covering the
/// scalars beyond the common set that the reflection binder converts: <see cref="DateOnly" />, <see cref="TimeOnly" />,
/// <see cref="Uri" />, the 128-bit, half-precision and native-sized numbers and <see cref="BigInteger" />.
/// </summary>
[DelimitedRecord]
public sealed partial class GeneratedExtendedScalarRecord
{
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    /// <value>The date.</value>
    public DateOnly Day { get; set; }

    /// <summary>
    /// Gets or sets the time of day.
    /// </summary>
    /// <value>The time of day.</value>
    public TimeOnly Time { get; set; }

    /// <summary>
    /// Gets or sets the link.
    /// </summary>
    /// <value>The link, or <see langword="null" />.</value>
    public Uri? Link { get; set; }

    /// <summary>
    /// Gets or sets the signed 128-bit number.
    /// </summary>
    /// <value>The signed 128-bit number.</value>
    public Int128 Large { get; set; }

    /// <summary>
    /// Gets or sets the unsigned 128-bit number.
    /// </summary>
    /// <value>The unsigned 128-bit number.</value>
    public UInt128 Unsigned { get; set; }

    /// <summary>
    /// Gets or sets the half-precision number.
    /// </summary>
    /// <value>The half-precision number.</value>
    public Half Small { get; set; }

    /// <summary>
    /// Gets or sets the arbitrary-precision integer.
    /// </summary>
    /// <value>The arbitrary-precision integer.</value>
    public BigInteger Big { get; set; }

    /// <summary>
    /// Gets or sets the native-sized signed integer.
    /// </summary>
    /// <value>The native-sized signed integer.</value>
    public nint Handle { get; set; }

    /// <summary>
    /// Gets or sets the native-sized unsigned integer.
    /// </summary>
    /// <value>The native-sized unsigned integer.</value>
    public nuint Size { get; set; }

    /// <summary>
    /// Gets or sets the optional date.
    /// </summary>
    /// <value>The date, or <see langword="null" />.</value>
    public DateOnly? MaybeDay { get; set; }

    /// <summary>
    /// Gets or sets the optional signed 128-bit number.
    /// </summary>
    /// <value>The number, or <see langword="null" />.</value>
    public Int128? MaybeLarge { get; set; }
}
