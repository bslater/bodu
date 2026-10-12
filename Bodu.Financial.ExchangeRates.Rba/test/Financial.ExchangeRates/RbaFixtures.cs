// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Serves the synthetic workbook of each RBA era, which <see cref="RbaTestWorkbook" /> generates the first time a test
/// asks for it, to the <c>Bodu.Financial.ExchangeRates.Rba</c> test suite.
/// </summary>
/// <remarks>
/// Each workbook is named, as the RBA names its own, by <see cref="RbaEraWorkbook.FileName" />, and is built once and
/// shared. <see cref="GetSheet" /> returns the content it was built from, from which a test computes what the provider
/// should read.
/// </remarks>
internal static class RbaFixtures
{
    /// <summary>The file name of the sample workbook, the open-ended 2023-to-current era's.</summary>
    public const string Sample = "2023-current.xls";

    /// <summary>
    /// The content and bytes of each era's workbook, keyed by the era's file name and built on first use.
    /// </summary>
    private static readonly Dictionary<string, Lazy<(RbaTestSheet Sheet, byte[] Bytes)>> s_workbooks =
        RbaEraWorkbook.Default.ToDictionary(
            static era => era.FileName,
            static era => new Lazy<(RbaTestSheet Sheet, byte[] Bytes)>(() =>
            {
                RbaTestSheet sheet = RbaTestWorkbook.CreateSheet(era);
                return (sheet, RbaTestWorkbook.Build(sheet));
            }),
            StringComparer.Ordinal);

    /// <summary>
    /// Returns the content an era's workbook was built from.
    /// </summary>
    /// <param name="fileName">The workbook file name, for example <c>2023-current.xls</c>.</param>
    /// <returns>The content of the workbook.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no era of <see cref="RbaEraWorkbook.Default" /> has the file name.
    /// </exception>
    public static RbaTestSheet GetSheet(string fileName) =>
        GetWorkbook(fileName).Sheet;

    /// <summary>
    /// Reads the bytes of an era's workbook.
    /// </summary>
    /// <param name="fileName">The workbook file name, for example <c>2023-current.xls</c>.</param>
    /// <returns>A copy of the workbook's bytes, which the caller may keep or change.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no era of <see cref="RbaEraWorkbook.Default" /> has the file name.
    /// </exception>
    public static byte[] ReadBytes(string fileName) =>
        [.. GetWorkbook(fileName).Bytes];

    /// <summary>
    /// Opens an era's workbook as a seekable, read-only stream.
    /// </summary>
    /// <param name="fileName">The workbook file name, for example <c>2023-current.xls</c>.</param>
    /// <returns>A <see cref="MemoryStream" /> positioned at the start of the workbook.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no era of <see cref="RbaEraWorkbook.Default" /> has the file name.
    /// </exception>
    public static MemoryStream OpenStream(string fileName) =>
        new(GetWorkbook(fileName).Bytes, writable: false);

    /// <summary>
    /// Returns the content and bytes of an era's workbook, building them on first use.
    /// </summary>
    /// <param name="fileName">The workbook file name.</param>
    /// <returns>The content and bytes of the workbook.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no era of <see cref="RbaEraWorkbook.Default" /> has the file name.
    /// </exception>
    private static (RbaTestSheet Sheet, byte[] Bytes) GetWorkbook(string fileName) =>
        s_workbooks.TryGetValue(fileName, out Lazy<(RbaTestSheet Sheet, byte[] Bytes)>? workbook)
            ? workbook.Value
            : throw new InvalidOperationException($"No RBA era has the workbook '{fileName}'.");
}
