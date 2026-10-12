// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestHeaderRows.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Selects the labelled header rows a synthetic F11.1 data sheet carries.
/// </summary>
/// <remarks>
/// Each row keeps its F11.1 position whether or not the others are written: the title in row 1, the description in
/// row 2, the frequency in row 3, the type in row 4, the units in row 5, the source in row 8, the publication date in
/// row 9 and the series identifier in row 10, each labelled in column 0.
/// </remarks>
[Flags]
internal enum RbaTestHeaderRows
{
    /// <summary>No header row.</summary>
    None = 0,

    /// <summary>The <c>Title</c> row, row 1.</summary>
    Title = 1 << 0,

    /// <summary>The <c>Description</c> row, row 2.</summary>
    Description = 1 << 1,

    /// <summary>The <c>Frequency</c> row, row 3.</summary>
    Frequency = 1 << 2,

    /// <summary>The <c>Type</c> row, row 4.</summary>
    Type = 1 << 3,

    /// <summary>The <c>Units</c> row, row 5.</summary>
    Units = 1 << 4,

    /// <summary>The <c>Source</c> row, row 8.</summary>
    Source = 1 << 5,

    /// <summary>The <c>Publication date</c> row, row 9.</summary>
    PublicationDate = 1 << 6,

    /// <summary>The <c>Series ID</c> row, row 10.</summary>
    SeriesId = 1 << 7,

    /// <summary>Every header row F11.1 carries.</summary>
    All = Title | Description | Frequency | Type | Units | Source | PublicationDate | SeriesId,
}
