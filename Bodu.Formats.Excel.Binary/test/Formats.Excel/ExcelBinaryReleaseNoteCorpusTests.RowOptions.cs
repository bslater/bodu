// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row runs with, and maps them onto the surfaces and the generators.
    /// </summary>
    /// <param name="ReadsRecords">
    /// <see langword="true" /> to render the record stream (the Records surface); <see langword="false" /> to render
    /// the workbook (the Workbook surface).
    /// </param>
    /// <param name="Version">The BIFF version the row names, or <see langword="null" /> when it names none.</param>
    /// <param name="Book">Whether the Workbook rendering opens with a <c>#book</c> line.</param>
    /// <param name="Formats">
    /// Whether each Workbook cell line carries its format index, format code and date flag.
    /// </param>
    /// <param name="DetectDates">The value of <see cref="ExcelBinaryReaderOptions.DetectDateFormats" />.</param>
    /// <remarks>
    /// A row with no <c>Version</c> option is laid out in BIFF8 and read with the version its first BOF record
    /// declares.
    /// </remarks>
    private sealed record RowOptions(bool ReadsRecords, BiffVersion? Version, bool Book, bool Formats, bool DetectDates)
    {
        /// <summary>
        /// Gets the version the generators lay the row's records out in.
        /// </summary>
        /// <value>The version the row names, or <see cref="BiffVersion.Biff8" /> when it names none.</value>
        public BiffVersion LayoutVersion =>
            Version ?? BiffVersion.Biff8;

        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">
        /// An option is malformed, unknown, or has a value it does not accept.
        /// </exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions(ReadsRecords: false, Version: null, Book: false, Formats: false, DetectDates: true);
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = (name, value) switch
                {
                    ("Surface", "Workbook") => options with { ReadsRecords = false },
                    ("Surface", "Records") => options with { ReadsRecords = true },
                    ("Version", "Biff5") => options with { Version = BiffVersion.Biff5 },
                    ("Version", "Biff8") => options with { Version = BiffVersion.Biff8 },
                    ("Book", "true") => options with { Book = true },
                    ("Book", "false") => options with { Book = false },
                    ("Formats", "true") => options with { Formats = true },
                    ("Formats", "false") => options with { Formats = false },
                    ("DetectDates", "true") => options with { DetectDates = true },
                    ("DetectDates", "false") => options with { DetectDates = false },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };
            }

            return options;
        }

        /// <summary>
        /// Creates the workbook options for the row.
        /// </summary>
        /// <returns>The workbook options.</returns>
        public ExcelBinaryReaderOptions ToWorkbookOptions() =>
            new()
            {
                DetectDateFormats = DetectDates,
            };

        /// <summary>
        /// Creates the record reader state for the row.
        /// </summary>
        /// <returns>
        /// A state whose <see cref="BiffReaderOptions.Version" /> is the version the row names, or the default state
        /// when it names none.
        /// </returns>
        public BiffReaderState ToReaderState() =>
            Version is { } version ? new BiffReaderState(new BiffReaderOptions { Version = version }) : default;
    }
}
