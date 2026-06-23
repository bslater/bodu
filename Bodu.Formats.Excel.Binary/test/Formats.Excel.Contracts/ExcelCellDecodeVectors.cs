// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellDecodeVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Formats.Excel.Contracts;

/// <summary>
/// Supplies the shared catalogue of <see cref="ExcelCellDecodeKat" /> rows that <see cref="ExcelCellDecodeContractTests" />
/// drives through every cell-decode surface.
/// </summary>
/// <remarks>
/// The records are built with <see cref="Biff8TestWorkbook" /> and exercise the value-bearing record types — inline
/// <c>LABEL</c>, <c>NUMBER</c>, <c>RK</c>, <c>MULRK</c>, <c>BOOLERR</c>, and <c>FORMULA</c> (with each cached-result
/// kind) — without referencing a shared string table, so the same rows decode identically through the streaming reader
/// and the materialized workbook.
/// </remarks>
internal static class ExcelCellDecodeVectors
{
    /// <summary>
    /// Gets the cell-decode known-answer rows, each wrapped as a single-element <c>object[]</c> for <c>[DynamicData]</c>.
    /// </summary>
    /// <returns>A sequence of rows, each carrying one <see cref="ExcelCellDecodeKat" />.</returns>
    public static IEnumerable<object[]> Cells
    {
        get
        {
            foreach (ExcelCellDecodeKat kat in Catalogue)
                yield return [kat];
        }
    }

    /// <summary>The immutable catalogue of decode known-answer rows.</summary>
    private static IReadOnlyList<ExcelCellDecodeKat> Catalogue { get; } = Build();

    /// <summary>
    /// Builds the decode known-answer catalogue.
    /// </summary>
    /// <returns>The list of decode rows.</returns>
    private static IReadOnlyList<ExcelCellDecodeKat> Build()
    {
        // RK encodings: integer flag (bit 1) set, value held in the high 30 bits.
        uint rk5 = (5u << 2) | 0x02;
        uint rk7 = (7u << 2) | 0x02;
        uint rk9 = (9u << 2) | 0x02;

        return
        [
            new("number record",
                [Biff8TestWorkbook.Number(0, 0, 3.5)],
                [ExcelExpectedCell.Number(0, 0, 3.5)]),

            new("rk record",
                [Biff8TestWorkbook.Rk(0, 0, rk7)],
                [ExcelExpectedCell.Number(0, 0, 7.0)]),

            new("inline label string",
                [Biff8TestWorkbook.Label(2, 1, "Inline")],
                [ExcelExpectedCell.Text(2, 1, "Inline")]),

            new("mulrk expands to one cell per value",
                [Biff8TestWorkbook.MulRk(3, 1, (0, rk5), (0, rk9))],
                [ExcelExpectedCell.Number(3, 1, 5.0), ExcelExpectedCell.Number(3, 2, 9.0)]),

            new("boolerr boolean cell",
                [Biff8TestWorkbook.BoolErr(0, 0, 0x01, isError: false)],
                [ExcelExpectedCell.Boolean(0, 0, true)]),

            new("boolerr error cell",
                [Biff8TestWorkbook.BoolErr(0, 0, 0x07, isError: true)],
                [ExcelExpectedCell.Error(0, 0, ExcelErrorCode.DivideByZero)]),

            new("formula with numeric result",
                [Biff8TestWorkbook.Formula(2, 3, NumberResult(1234.5))],
                [ExcelExpectedCell.Number(2, 3, 1234.5)]),

            new("formula with boolean result",
                [Biff8TestWorkbook.Formula(0, 0, [0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF])],
                [ExcelExpectedCell.Boolean(0, 0, true)]),

            new("formula with error result",
                [Biff8TestWorkbook.Formula(1, 1, [0x02, 0x00, 0x2A, 0x00, 0x00, 0x00, 0xFF, 0xFF])],
                [ExcelExpectedCell.Error(1, 1, ExcelErrorCode.NotAvailable)]),

            new("formula with string result",
                [
                    Biff8TestWorkbook.Formula(4, 5, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF]),
                    Biff8TestWorkbook.CompressedString("Hello"),
                ],
                [ExcelExpectedCell.Text(4, 5, "Hello")]),

            new("formula string result with no string record keeps next record",
                [
                    Biff8TestWorkbook.Formula(0, 0, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF]),
                    Biff8TestWorkbook.Number(1, 0, 9.0),
                ],
                [ExcelExpectedCell.Text(0, 0, string.Empty), ExcelExpectedCell.Number(1, 0, 9.0)]),
        ];
    }

    /// <summary>
    /// Encodes a numeric cached formula result as its eight little-endian bytes.
    /// </summary>
    /// <param name="value">The numeric result.</param>
    /// <returns>The eight-byte cached result.</returns>
    private static byte[] NumberResult(double value)
    {
        byte[] result = new byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(result, value);

        return result;
    }
}
