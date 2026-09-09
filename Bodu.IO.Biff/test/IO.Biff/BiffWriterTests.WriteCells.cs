// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteCells.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Verifies that a NUMBER record is written in the fourteen-byte layout and decodes back.
    /// </summary>
    [TestMethod]
    public void WriteNumber_WhenWritten_ShouldDecodeBack()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteNumber(3, 4, 15, -2.5));

        BiffReader reader = Single(bytes);
        Assert.AreEqual(new BiffNumberRecord(3, 4, 15, -2.5), reader.GetNumber());
        CollectionAssert.AreEqual(BiffTestRecords.Number(3, 4, -2.5, 15), bytes);
    }

    /// <summary>
    /// Verifies that an RK record decodes back.
    /// </summary>
    [TestMethod]
    public void WriteRk_WhenWritten_ShouldDecodeBack()
    {
        Assert.IsTrue(BiffRk.TryEncode(12.34, out uint rk));
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteRk(1, 2, 3, rk));

        Assert.AreEqual(12.34, Single(bytes, BiffVersion.Biff5).GetRk().Value);
    }

    /// <summary>
    /// Verifies that a MULRK run decodes back with the declared last column.
    /// </summary>
    [TestMethod]
    public void WriteMulRk_WhenWritten_ShouldDecodeBack()
    {
        BiffRkCell[] cells = [new(1, 0x02), new(2, 0x06), new(3, 0x0A)];
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteMulRk(7, 4, cells));

        BiffMulRkRecord run = Single(bytes).GetMulRk();
        Assert.AreEqual(7, run.Row);
        Assert.AreEqual(4, run.FirstColumn);
        Assert.AreEqual(6, run.LastColumn);
        Assert.AreEqual(3, run.Count);
        Assert.AreEqual(2.0, run[2].Value);
        CollectionAssert.AreEqual(BiffTestRecords.MulRk(7, 4, (1, 0x02), (2, 0x06), (3, 0x0A)), bytes);
    }

    /// <summary>
    /// Verifies that an empty MULRK run is rejected.
    /// </summary>
    [TestMethod]
    public void WriteMulRk_WhenEmpty_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => Emit8((ref BiffWriter w) => w.WriteMulRk(0, 0, default)));
    }

    /// <summary>
    /// Verifies that BLANK and MULBLANK records decode back.
    /// </summary>
    [TestMethod]
    public void WriteBlank_WhenWritten_ShouldDecodeBackWithMulBlank()
    {
        byte[] bytes = Emit8((ref BiffWriter w) =>
        {
            w.WriteBlank(1, 1, 9);
            w.WriteMulBlank(2, 5, [10, 11]);
        });

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(new BiffBlankRecord(1, 1, 9), reader.GetBlank());
        Assert.IsTrue(reader.Read());
        BiffMulBlankRecord run = reader.GetMulBlank();
        Assert.AreEqual(2, run.Count);
        Assert.AreEqual(6, run.LastColumn);
        Assert.AreEqual(11, run[1]);
    }

    /// <summary>
    /// Verifies that boolean and error cells decode back through the BOOLERR record.
    /// </summary>
    [TestMethod]
    public void WriteBoolean_WhenWritten_ShouldDecodeBackWithError()
    {
        byte[] bytes = Emit8((ref BiffWriter w) =>
        {
            w.WriteBoolean(0, 0, 0, true);
            w.WriteError(0, 1, 0, 0x2A);
        });

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        BiffBoolErrRecord boolean = reader.GetBoolErr();
        Assert.IsFalse(boolean.IsError);
        Assert.IsTrue(boolean.BooleanValue);
        Assert.IsTrue(reader.Read());
        BiffBoolErrRecord error = reader.GetBoolErr();
        Assert.IsTrue(error.IsError);
        Assert.AreEqual(0x2A, error.ErrorCode);
    }

    /// <summary>
    /// Verifies that a row or column outside the 16-bit range is rejected by every cell writer.
    /// </summary>
    [TestMethod]
    public void WriteNumber_WhenRowOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteNumber(65536, 0, 0, 1)),
            "row");
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteBlank(0, -1, 0)),
            "column");
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteMulBlank(0, 65535, [1, 2])),
            "xfIndices");
    }

    /// <summary>
    /// Verifies that a ROW record decodes back.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenWritten_ShouldDecodeBack()
    {
        var row = new BiffRowRecord(5, 1, 4, 0x00FF, 0x00C0, 0x0015);
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteRow(row));

        Assert.AreEqual(row, Single(bytes).GetRow());
    }

    /// <summary>
    /// Verifies that a DIMENSIONS record is written in the version's layout and decodes back.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="expectedLength">The expected payload length.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5, 10)]
    [DataRow(BiffVersion.Biff8, 14)]
    public void WriteDimensions_WhenWritten_ShouldUseVersionLayout(BiffVersion version, int expectedLength)
    {
        var dimensions = new BiffDimensionsRecord(1, 100, 2, 8);
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteDimensions(dimensions));

        BiffReader reader = Single(bytes, version);
        Assert.AreEqual(expectedLength, reader.RecordLength);
        Assert.AreEqual(dimensions, reader.GetDimensions());
    }

    /// <summary>
    /// Verifies that a row beyond the 16-bit range is accepted under BIFF8 and rejected under BIFF5.
    /// </summary>
    [TestMethod]
    public void WriteDimensions_WhenRowExceeds16Bits_ShouldDependOnVersion()
    {
        var dimensions = new BiffDimensionsRecord(0, 70000, 0, 1);

        Assert.AreEqual(70000, Single(Emit8((ref BiffWriter w) => w.WriteDimensions(dimensions))).GetDimensions().LastRowExclusive);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Emit5((ref BiffWriter w) => w.WriteDimensions(dimensions)));
    }

    /// <summary>
    /// Verifies that an XF record is written at the version's length and decodes back.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="expectedLength">The expected payload length.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5, 16)]
    [DataRow(BiffVersion.Biff8, 20)]
    public void WriteXf_WhenWritten_ShouldUseVersionLength(BiffVersion version, int expectedLength)
    {
        var xf = new BiffXfRecord(2, 14, 0xFFF5);
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteXf(xf));

        BiffReader reader = Single(bytes, version);
        Assert.AreEqual(expectedLength, reader.RecordLength);
        Assert.AreEqual(xf, reader.GetXf());
    }

    /// <summary>
    /// Verifies the exact capacity of a MULRK run under each version: the largest run that fits is accepted and one
    /// more cell is rejected with the cells parameter named.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteMulRk_WhenRunAtCapacity_ShouldAcceptAndRejectOneOver(BiffVersion version)
    {
        int max = (BiffLimits.GetMaxPayloadLength(version) - 6) / BiffRkCell.Length;
        BiffRkCell[] cells = [.. Enumerable.Range(0, max).Select(i => new BiffRkCell((ushort)i, ((uint)i << 2) | 0x02))];

        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteMulRk(0, 0, cells));
        BiffMulRkRecord run = Single(bytes, version).GetMulRk();
        Assert.AreEqual(max, run.Count);
        Assert.AreEqual(max - 1, run.LastColumn);
        Assert.AreEqual(max - 1.0, run[max - 1].Value);

        BiffRkCell[] tooMany = [.. cells, new BiffRkCell(0, 0x02)];
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit(version, (ref BiffWriter w) => w.WriteMulRk(0, 0, tooMany)),
            "cells");
    }

    /// <summary>
    /// Verifies that a run of one cell is a valid MULRK record whose first and last columns coincide.
    /// </summary>
    [TestMethod]
    public void WriteMulRk_WhenSingleCell_ShouldWriteOneCellRun()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteMulRk(3, 250, [new BiffRkCell(7, 0x0A)]));

        BiffMulRkRecord run = Single(bytes).GetMulRk();
        Assert.AreEqual(1, run.Count);
        Assert.AreEqual(250, run.FirstColumn);
        Assert.AreEqual(250, run.LastColumn);
        Assert.AreEqual(2.0, run[0].Value);
        Assert.AreEqual(7, run[0].XfIndex);
    }

    /// <summary>
    /// Verifies that a run whose last column would pass the 16-bit limit is rejected even though its first column
    /// is valid.
    /// </summary>
    [TestMethod]
    public void WriteMulRk_WhenRunPassesLastColumn_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteMulRk(0, 65535, [new BiffRkCell(0, 0x02), new BiffRkCell(0, 0x02)])),
            "cells");
    }

    /// <summary>
    /// Verifies the exact capacity of a MULBLANK run under each version.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteMulBlank_WhenRunAtCapacity_ShouldAcceptAndRejectOneOver(BiffVersion version)
    {
        int max = (BiffLimits.GetMaxPayloadLength(version) - 6) / 2;
        ushort[] xfs = [.. Enumerable.Range(0, max).Select(i => (ushort)i)];

        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteMulBlank(0, 0, xfs));
        BiffMulBlankRecord run = Single(bytes, version).GetMulBlank();
        Assert.AreEqual(max, run.Count);
        Assert.AreEqual(max - 1, run[max - 1]);

        ushort[] tooMany = [.. xfs, 0];
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit(version, (ref BiffWriter w) => w.WriteMulBlank(0, 0, tooMany)),
            "xfIndices");
    }

    /// <summary>
    /// Verifies that an empty MULBLANK run is rejected.
    /// </summary>
    [TestMethod]
    public void WriteMulBlank_WhenEmpty_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => Emit8((ref BiffWriter w) => w.WriteMulBlank(0, 0, default)));
    }

    /// <summary>
    /// Verifies that the raw BOOLERR overload writes the value byte and kind flag verbatim.
    /// </summary>
    [TestMethod]
    public void WriteBoolErr_WhenRawValue_ShouldRoundTripVerbatim()
    {
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteBoolErr(1, 2, 3, 0x7F, isError: false));

        BiffBoolErrRecord cell = Single(bytes, BiffVersion.Biff5).GetBoolErr();
        Assert.AreEqual(new BiffBoolErrRecord(1, 2, 3, 0x7F, false), cell);
        CollectionAssert.AreEqual(BiffTestRecords.BoolErr(1, 2, 0x7F, false, xf: 3), bytes);
    }

    /// <summary>
    /// Verifies that a false boolean writes a zero value byte.
    /// </summary>
    [TestMethod]
    public void WriteBoolean_WhenFalse_ShouldWriteZero()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteBoolean(0, 0, 0, false));

        BiffBoolErrRecord cell = Single(bytes).GetBoolErr();
        Assert.IsFalse(cell.BooleanValue);
        Assert.AreEqual(0, cell.RawValue);
    }

    /// <summary>
    /// Verifies that every cell writer rejects a column outside the 16-bit range with the column parameter named.
    /// </summary>
    /// <param name="writer">The name of the writer under test.</param>
    [TestMethod]
    [DataRow("WriteNumber")]
    [DataRow("WriteRk")]
    [DataRow("WriteBlank")]
    [DataRow("WriteBoolean")]
    [DataRow("WriteError")]
    [DataRow("WriteLabel")]
    [DataRow("WriteLabelSst")]
    [DataRow("WriteFormula")]
    public void WriteCells_WhenColumnOutOfRange_ShouldThrowArgumentOutOfRangeException(string writer)
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) =>
            {
                switch (writer)
                {
                    case "WriteNumber": w.WriteNumber(0, 65536, 0, 1); break;
                    case "WriteRk": w.WriteRk(0, 65536, 0, 0x02); break;
                    case "WriteBlank": w.WriteBlank(0, 65536, 0); break;
                    case "WriteBoolean": w.WriteBoolean(0, 65536, 0, true); break;
                    case "WriteError": w.WriteError(0, 65536, 0, 0x07); break;
                    case "WriteLabel": w.WriteLabel(0, 65536, 0, "x"); break;
                    case "WriteLabelSst": w.WriteLabelSst(0, 65536, 0, 0); break;
                    default: w.WriteFormula(0, 65536, 0, 1.0, default); break;
                }
            }),
            "column");
    }

    /// <summary>
    /// Verifies that a ROW record whose fields exceed the 16-bit range is rejected with the record parameter named.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="lastColumnExclusive">One past the last column.</param>
    [TestMethod]
    [DataRow(65536, 1)]
    [DataRow(-1, 1)]
    [DataRow(0, 65536)]
    public void WriteRow_WhenFieldOutOfRange_ShouldThrowArgumentOutOfRangeException(int row, int lastColumnExclusive)
    {
        var record = new BiffRowRecord(row, 0, lastColumnExclusive, 0, 0, 0);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteRow(record)),
            "record");
    }

    /// <summary>
    /// Verifies that the derived ROW properties survive a round trip with every option bit set.
    /// </summary>
    [TestMethod]
    public void WriteRow_WhenAllOptionsSet_ShouldRoundTripDerivedProperties()
    {
        var row = new BiffRowRecord(65535, 65535, 65535, 0x7FFF, 0xFFFF, 0xFFFF);
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteRow(row));

        BiffRowRecord decoded = Single(bytes, BiffVersion.Biff5).GetRow();
        Assert.AreEqual(row, decoded);
        Assert.AreEqual(0x7FFF, decoded.Height);
        Assert.IsTrue(decoded.IsHidden);
        Assert.IsTrue(decoded.HasCustomHeight);
        Assert.IsTrue(decoded.HasFormat);
        Assert.AreEqual(7, decoded.OutlineLevel);
        Assert.AreEqual(0x0FFF, decoded.XfIndex);
    }

    /// <summary>
    /// Verifies that DIMENSIONS rejects a negative row and a column beyond the 16-bit range under both versions.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteDimensions_WhenFieldOutOfRange_ShouldThrowArgumentOutOfRangeException(BiffVersion version)
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Emit(version, (ref BiffWriter w) => w.WriteDimensions(new BiffDimensionsRecord(-1, 1, 0, 1))));
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Emit(version, (ref BiffWriter w) => w.WriteDimensions(new BiffDimensionsRecord(0, -1, 0, 1))));
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit(version, (ref BiffWriter w) => w.WriteDimensions(new BiffDimensionsRecord(0, 1, 0, 65536))),
            "record");
    }

    /// <summary>
    /// Verifies that an empty used range and the largest BIFF8 row extent round-trip.
    /// </summary>
    [TestMethod]
    public void WriteDimensions_WhenEmptyOrMaximumExtent_ShouldRoundTrip()
    {
        var empty = new BiffDimensionsRecord(0, 0, 0, 0);
        var largest = new BiffDimensionsRecord(0, int.MaxValue, 0, 65535);

        Assert.AreEqual(empty, Single(Emit8((ref BiffWriter w) => w.WriteDimensions(empty))).GetDimensions());
        Assert.AreEqual(largest, Single(Emit8((ref BiffWriter w) => w.WriteDimensions(largest))).GetDimensions());
        Assert.AreEqual(0, Single(Emit8((ref BiffWriter w) => w.WriteDimensions(empty))).GetDimensions().RowCount);
    }

    /// <summary>
    /// Verifies that the XF flag properties round-trip through the type field and the unwritten fields are zero.
    /// </summary>
    [TestMethod]
    public void WriteXf_WhenFlagsSet_ShouldRoundTripFlagsAndZeroTail()
    {
        var xf = new BiffXfRecord(1, 2, 0x0057);
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteXf(xf));

        BiffReader reader = Single(bytes);
        BiffXfRecord decoded = reader.GetXf();
        Assert.IsTrue(decoded.IsLocked);
        Assert.IsTrue(decoded.IsHidden);
        Assert.IsTrue(decoded.IsStyle);
        Assert.AreEqual(5, decoded.ParentStyleIndex);
        Assert.IsTrue(reader.ValueSpan.Slice(6).ToArray().All(b => b == 0));
    }

    /// <summary>
    /// Verifies that NUMBER preserves every double bit pattern including NaN and negative zero.
    /// </summary>
    /// <param name="value">The value.</param>
    [TestMethod]
    [DataRow(double.NaN)]
    [DataRow(-0.0)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(double.MinValue)]
    public void WriteNumber_WhenSpecialValue_ShouldPreserveBits(double value)
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteNumber(0, 0, 0, value));

        Assert.AreEqual(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(Single(bytes).GetNumber().Value));
    }

    /// <summary>
    /// Verifies that an RK value is written verbatim, including patterns whose flag bits select each form.
    /// </summary>
    /// <param name="rk">The raw RK value.</param>
    [TestMethod]
    [DataRow(0x00000000u)]
    [DataRow(0xFFFFFFFFu)]
    [DataRow(0x80000000u)]
    [DataRow(0x3FF00001u)]
    public void WriteRk_WhenAnyPattern_ShouldWriteVerbatim(uint rk)
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteRk(0, 0, 0, rk));

        Assert.AreEqual(rk, Single(bytes).GetRk().RawValue);
    }
}
