// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateWorkbookParserTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="RbaRateWorkbookParser" /> interprets the RBA workbook layout correctly, over workbooks
/// <see cref="RbaTestWorkbook" /> generates: the synthetic workbooks of the RBA eras, the 2023-to-current sample among
/// them, and smaller ones a test describes.
/// </summary>
[TestClass]
public partial class RbaRateWorkbookParserTests
{
    /// <summary>
    /// Parses the sample workbook, the synthetic workbook of the 2023-to-current era, with default options.
    /// </summary>
    /// <returns>The parsed table.</returns>
    private static RbaRateTable ParseSample() =>
        ParseFixture(RbaFixtures.Sample);

    /// <summary>
    /// Parses the synthetic workbook of an era with default options.
    /// </summary>
    /// <param name="fileName">The era's workbook file name, for example <c>2010-2013.xls</c>.</param>
    /// <returns>The parsed table.</returns>
    private static RbaRateTable ParseFixture(string fileName)
    {
        using MemoryStream stream = RbaFixtures.OpenStream(fileName);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        return RbaRateWorkbookParser.Parse(workbook, new RbaRateProviderOptions());
    }

    /// <summary>
    /// Builds the workbook a synthetic sheet describes and parses it with default options.
    /// </summary>
    /// <param name="sheet">The content of the workbook.</param>
    /// <returns>The parsed table.</returns>
    private static RbaRateTable Parse(RbaTestSheet sheet)
    {
        using MemoryStream stream = new(RbaTestWorkbook.Build(sheet), writable: false);
        using var workbook = ExcelBinaryWorkbook.OpenRead(stream, leaveOpen: true);
        return RbaRateWorkbookParser.Parse(workbook, new RbaRateProviderOptions());
    }

    /// <summary>
    /// Creates the content of a small F11.1 workbook: a US dollar column and a trade-weighted index column over three
    /// days, with every header row.
    /// </summary>
    /// <returns>The content of the workbook.</returns>
    private static RbaTestSheet CreateSheet()
    {
        DateOnly published = new(2024, 1, 4);

        return new RbaTestSheet
        {
            Banner = "F11.1  EXCHANGE RATES ",
            Dates = [new DateOnly(2024, 1, 2), new DateOnly(2024, 1, 3), published],
            Columns =
            [
                new RbaTestColumn
                {
                    Title = "A$1=USD",
                    Description = "AUD/USD Exchange Rate; see notes for further detail.",
                    Units = "USD",
                    Source = "WM/Reuters",
                    PublicationDate = published,
                    SeriesId = "FXRUSD",
                    Cells = [RbaTestCell.FromNumber(0.6812m), RbaTestCell.FromNumber(0.6735m), RbaTestCell.FromNumber(0.6704m)],
                },
                new RbaTestColumn
                {
                    Title = "Trade-weighted Index May 1970 = 100",
                    Description = "Australian Dollar Trade-weighted Index",
                    Units = "Index",
                    Source = "RBA",
                    PublicationDate = published,
                    SeriesId = "FXRTWI",
                    Decimals = 1,
                    Cells = [RbaTestCell.FromNumber(61.3m), RbaTestCell.FromNumber(61.1m), RbaTestCell.FromNumber(60.9m)],
                },
            ],
        };
    }
}
