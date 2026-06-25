// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellDecodeContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Formats.Excel.Contracts;

/// <summary>
/// Defines the shared cell-decode contract verified across every BIFF8 decode surface: a derived class supplies a
/// concrete decode path through <see cref="DecodeCells(ExcelCellDecodeKat)" /> and inherits the known-answer assertions.
/// </summary>
/// <remarks>
/// Two surfaces decode the identical worksheet records — the forward-only <see cref="ExcelWorksheetReader" /> and the
/// materialized <see cref="ExcelWorksheet" /> obtained from <see cref="ExcelBinaryWorkbook" />. Both inherit this base
/// and run the same <see cref="ExcelCellDecodeVectors" /> rows, proving the streaming and materialized paths agree on
/// every vector.
/// </remarks>
public abstract class ExcelCellDecodeContractTests
{
    /// <summary>
    /// Decodes the records of a known-answer row into a position-keyed map of cells, using the surface under test.
    /// </summary>
    /// <param name="kat">The known-answer row whose records are decoded.</param>
    /// <returns>A dictionary mapping each populated cell's (row, column) to its decoded value.</returns>
    protected abstract IReadOnlyDictionary<(int Row, int Column), ExcelCell> DecodeCells(ExcelCellDecodeKat kat);

    /// <summary>
    /// Verifies that decoding a known-answer row produces exactly the expected cells, each carrying the expected kind
    /// and value.
    /// </summary>
    /// <param name="kat">The known-answer row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ExcelCellDecodeVectors.Cells),
        typeof(ExcelCellDecodeVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_WhenKnownAnswer_ShouldProduceExpectedCells(ExcelCellDecodeKat kat)
    {
        IReadOnlyDictionary<(int Row, int Column), ExcelCell> actual = DecodeCells(kat);

        Assert.HasCount(kat.Expected.Count, actual, $"KAT '{kat.Name}': unexpected cell count.");
        foreach (ExcelExpectedCell expected in kat.Expected)
        {
            Assert.IsTrue(
                actual.TryGetValue((expected.Row, expected.Column), out ExcelCell cell),
                $"KAT '{kat.Name}': missing cell at ({expected.Row}, {expected.Column}).");
            Assert.AreEqual(expected.Kind, cell.Kind, $"KAT '{kat.Name}': wrong kind at ({expected.Row}, {expected.Column}).");
            AssertValue(kat, expected, cell);
        }
    }

    /// <summary>
    /// Asserts that a decoded cell's value matches the expectation for its kind.
    /// </summary>
    /// <param name="kat">The known-answer row, used for failure context.</param>
    /// <param name="expected">The expected cell.</param>
    /// <param name="cell">The decoded cell.</param>
    private static void AssertValue(ExcelCellDecodeKat kat, ExcelExpectedCell expected, ExcelCell cell)
    {
        string at = $"KAT '{kat.Name}': wrong value at ({expected.Row}, {expected.Column}).";
        switch (expected.Kind)
        {
            case ExcelCellKind.Number:
                Assert.AreEqual((double)expected.Value!, cell.NumberValue!.Value, 0.0, at);
                break;

            case ExcelCellKind.Boolean:
                Assert.AreEqual((bool)expected.Value!, cell.BooleanValue!.Value, at);
                break;

            case ExcelCellKind.String:
                Assert.AreEqual((string?)expected.Value, cell.StringValue, at);
                break;

            case ExcelCellKind.Error:
                Assert.AreEqual((ExcelErrorCode)expected.Value!, cell.ErrorValue!.Value, at);
                break;

            default:
                Assert.Fail($"KAT '{kat.Name}': unsupported expected kind '{expected.Kind}'.");
                break;
        }
    }
}
