// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellDecodeKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Formats.Excel.Contracts;

/// <summary>
/// A known-answer test row for worksheet cell decoding: a sequence of framed BIFF8 records and the set of cells the
/// reader is expected to surface from them.
/// </summary>
/// <param name="Name">The human-readable scenario label surfaced in test output.</param>
/// <param name="Records">The framed BIFF8 records that form the worksheet substream body, in record order.</param>
/// <param name="Expected">The cells the decoder is expected to produce, keyed by position.</param>
/// <remarks>
/// The same row drives both decode surfaces — the forward-only <see cref="ExcelWorksheetReader" /> and the materialized
/// <see cref="ExcelWorksheet" /> obtained from <see cref="ExcelBinaryWorkbook" /> — through
/// <see cref="ExcelCellDecodeContractTests" />, proving the two paths agree on every vector. Records are limited to
/// those that decode without a shared string table (inline labels, numbers, RK, MULRK, BOOLERR, and formula records) so
/// that the empty-table reader path and the workbook path produce identical results.
/// </remarks>
public sealed record ExcelCellDecodeKat(string Name, byte[][] Records, IReadOnlyList<ExcelExpectedCell> Expected)
    : IKat;
