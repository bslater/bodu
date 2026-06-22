// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaEra.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Identifies one of the date-range files in which the Reserve Bank of Australia publishes its historical daily
/// exchange rates.
/// </summary>
/// <remarks>
/// The RBA splits its historical daily exchange-rate data into a small set of multi-year files plus an open-ended
/// "current" file. Each era maps to one downloadable <c>.xls</c> file and the inclusive date range it covers. The
/// default catalogue is exposed through <see cref="Default" /> and can be overridden via the provider options if the
/// RBA changes its file names.
/// </remarks>
public sealed class RbaEra
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RbaEra" /> class.
    /// </summary>
    /// <param name="label">The era label, also used to form the file name.</param>
    /// <param name="start">The first calendar date the era covers.</param>
    /// <param name="end">
    /// The last calendar date the era covers, or <see langword="null" /> when the era is open-ended.
    /// </param>
    public RbaEra(string label, DateOnly start, DateOnly? end)
    {
        ThrowHelper.ThrowIfNull(label);

        Label = label;
        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the era label (for example, <c>2023-current</c>).
    /// </summary>
    /// <returns>The era label.</returns>
    public string Label { get; }

    /// <summary>
    /// Gets the file name of the era's workbook, relative to the provider's base URL.
    /// </summary>
    /// <returns>The workbook file name, formed by appending <c>.xls</c> to <see cref="Label" />.</returns>
    public string FileName => Label + ".xls";

    /// <summary>
    /// Gets the first calendar date the era covers.
    /// </summary>
    /// <returns>The inclusive start date.</returns>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets the last calendar date the era covers, or <see langword="null" /> when the era is open-ended.
    /// </summary>
    /// <returns>The inclusive end date, or <see langword="null" /> for the open-ended current era.</returns>
    public DateOnly? End { get; }

    /// <summary>
    /// Gets the default catalogue of RBA historical daily exchange-rate eras, from 1983 to the current file.
    /// </summary>
    /// <returns>The ordered, immutable default era catalogue.</returns>
    public static IReadOnlyList<RbaEra> Default { get; } =
    [
        new("1983-1986", new DateOnly(1983, 1, 1), new DateOnly(1986, 12, 31)),
        new("1987-1990", new DateOnly(1987, 1, 1), new DateOnly(1990, 12, 31)),
        new("1991-1994", new DateOnly(1991, 1, 1), new DateOnly(1994, 12, 31)),
        new("1995-1998", new DateOnly(1995, 1, 1), new DateOnly(1998, 12, 31)),
        new("1999-2002", new DateOnly(1999, 1, 1), new DateOnly(2002, 12, 31)),
        new("2003-2006", new DateOnly(2003, 1, 1), new DateOnly(2006, 12, 31)),
        new("2007-2009", new DateOnly(2007, 1, 1), new DateOnly(2009, 12, 31)),
        new("2010-2013", new DateOnly(2010, 1, 1), new DateOnly(2013, 12, 31)),
        new("2014-2017", new DateOnly(2014, 1, 1), new DateOnly(2017, 12, 31)),
        new("2018-2022", new DateOnly(2018, 1, 1), new DateOnly(2022, 12, 31)),
        new("2023-current", new DateOnly(2023, 1, 1), null),
    ];

    /// <summary>
    /// Determines whether the era covers the specified date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="date" /> falls within the era; otherwise <see langword="false" />.
    /// </returns>
    public bool Covers(DateOnly date) =>
        date >= Start && (End is null || date <= End.Value);

    /// <summary>
    /// Finds the era in <paramref name="eras" /> that covers the specified date.
    /// </summary>
    /// <param name="date">The date to resolve.</param>
    /// <param name="eras">The era catalogue to search.</param>
    /// <returns>The covering era, or <see langword="null" /> when no era covers <paramref name="date" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="eras" /> is <see langword="null" />.
    /// </exception>
    public static RbaEra? ForDate(DateOnly date, IReadOnlyList<RbaEra> eras)
    {
        ThrowHelper.ThrowIfNull(eras);

        for (int i = 0; i < eras.Count; i++)
        {
            if (eras[i].Covers(date))
                return eras[i];
        }

        return null;
    }
}
