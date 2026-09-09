// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.Accessors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Supplies every record type that has a typed accessor.
    /// </summary>
    public static IEnumerable<object[]> AccessorRecordTypes
    {
        get
        {
            foreach (BiffRecordType type in AccessorTypes)
                yield return [type];
        }
    }

    /// <summary>
    /// Supplies every record type whose accessor depends on the established version.
    /// </summary>
    public static IEnumerable<object[]> VersionDependentRecordTypes
    {
        get
        {
            yield return [BiffRecordType.BoundSheet];
            yield return [BiffRecordType.Dimensions];
            yield return [BiffRecordType.Label];
            yield return [BiffRecordType.String];
            yield return [BiffRecordType.Format];
            yield return [BiffRecordType.Font];
            yield return [BiffRecordType.FilePass];
        }
    }

    /// <summary>The record types the codec names with a typed accessor.</summary>
    private static readonly BiffRecordType[] AccessorTypes =
    [
        BiffRecordType.Bof, BiffRecordType.BoundSheet, BiffRecordType.Dimensions, BiffRecordType.Row,
        BiffRecordType.Number, BiffRecordType.Rk, BiffRecordType.MulRk, BiffRecordType.Label, BiffRecordType.RString,
        BiffRecordType.LabelSst, BiffRecordType.BoolErr, BiffRecordType.Blank, BiffRecordType.MulBlank,
        BiffRecordType.Formula, BiffRecordType.String, BiffRecordType.Xf, BiffRecordType.Format, BiffRecordType.Font,
        BiffRecordType.CodePage, BiffRecordType.DateMode, BiffRecordType.FilePass, BiffRecordType.Sst,
    ];

    /// <summary>
    /// Invokes the typed accessor for the specified record type against the reader, whatever record is current.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="type">The record type whose accessor to invoke.</param>
    private static void InvokeAccessor(ref BiffReader reader, BiffRecordType type)
    {
        switch (type)
        {
            case BiffRecordType.Bof: _ = reader.GetBof(); break;
            case BiffRecordType.BoundSheet: _ = reader.GetBoundSheet(); break;
            case BiffRecordType.Dimensions: _ = reader.GetDimensions(); break;
            case BiffRecordType.Row: _ = reader.GetRow(); break;
            case BiffRecordType.Number: _ = reader.GetNumber(); break;
            case BiffRecordType.Rk: _ = reader.GetRk(); break;
            case BiffRecordType.MulRk: _ = reader.GetMulRk(); break;
            case BiffRecordType.Label: _ = reader.GetLabel(); break;
            case BiffRecordType.RString: _ = reader.GetRString(); break;
            case BiffRecordType.LabelSst: _ = reader.GetLabelSst(); break;
            case BiffRecordType.BoolErr: _ = reader.GetBoolErr(); break;
            case BiffRecordType.Blank: _ = reader.GetBlank(); break;
            case BiffRecordType.MulBlank: _ = reader.GetMulBlank(); break;
            case BiffRecordType.Formula: _ = reader.GetFormula(); break;
            case BiffRecordType.String: _ = reader.GetString(); break;
            case BiffRecordType.Xf: _ = reader.GetXf(); break;
            case BiffRecordType.Format: _ = reader.GetFormat(); break;
            case BiffRecordType.Font: _ = reader.GetFont(); break;
            case BiffRecordType.CodePage: _ = reader.GetCodePage(); break;
            case BiffRecordType.DateMode: _ = reader.GetDateMode(); break;
            case BiffRecordType.FilePass: _ = reader.GetFilePass(); break;
            case BiffRecordType.Sst: _ = reader.GetSstHeader(); break;
            default: Assert.Fail($"No accessor for {type}."); break;
        }
    }

    /// <summary>
    /// Verifies that every typed accessor rejects a reader with no current record with
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    /// <param name="type">The record type whose accessor is invoked.</param>
    [TestMethod]
    [DynamicData(nameof(AccessorRecordTypes))]
    public void Accessors_WhenNoRecordIsCurrent_ShouldThrowInvalidOperationException(BiffRecordType type)
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Bof8(), new BiffReaderOptions { Version = BiffVersion.Biff8 });
            InvokeAccessor(ref reader, type);
        });
    }

    /// <summary>
    /// Verifies that every typed accessor rejects a current record of a different type with
    /// <see cref="InvalidOperationException" /> naming both types.
    /// </summary>
    /// <param name="type">The record type whose accessor is invoked.</param>
    [TestMethod]
    [DynamicData(nameof(AccessorRecordTypes))]
    public void Accessors_WhenCurrentRecordIsDifferentType_ShouldThrowInvalidOperationException(BiffRecordType type)
    {
        // An unknown record is never the expected type, so the sweep is uniform across the accessors.
        byte[] stream = BiffTestRecords.Record(0x0FFE, new byte[32]);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(stream, new BiffReaderOptions { Version = BiffVersion.Biff8 });
            Assert.IsTrue(reader.Read());
            InvokeAccessor(ref reader, type);
        });

        Assert.IsTrue(ex.Message.Contains(type.ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that every version-dependent accessor refuses to decode before the version is established, even
    /// when the record is well formed.
    /// </summary>
    /// <param name="type">The record type whose accessor is invoked.</param>
    [TestMethod]
    [DynamicData(nameof(VersionDependentRecordTypes))]
    public void Accessors_WhenVersionUnknown_ShouldThrowInvalidOperationException(BiffRecordType type)
    {
        byte[] stream = BiffTestRecords.Record(type, new byte[32]);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(stream);
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(BiffVersion.Unknown, reader.Version);
            InvokeAccessor(ref reader, type);
        });
    }

    /// <summary>
    /// Verifies that the accessors whose layouts do not vary by version decode without a version having been
    /// established.
    /// </summary>
    [TestMethod]
    public void Accessors_WhenVersionUnknownAndLayoutIsFixed_ShouldDecode()
    {
        var reader = new BiffReader(BiffTestRecords.Stream(BiffTestRecords.Number(1, 2, 3.5), BiffTestRecords.Xf(1, 2, 3), BiffTestRecords.Row(1, 0, 2, 0, 0, 0)));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(3.5, reader.GetNumber().Value);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(2, reader.GetXf().FormatIndex);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.GetRow().Row);
        Assert.AreEqual(BiffVersion.Unknown, reader.Version);
    }

    /// <summary>
    /// Verifies that a typed accessor may be called repeatedly on the same record and returns the same decoding
    /// without advancing the reader.
    /// </summary>
    [TestMethod]
    public void Accessors_WhenCalledTwice_ShouldNotAdvance()
    {
        var reader = new BiffReader(BiffTestRecords.Stream(BiffTestRecords.Number(4, 5, 6.5), BiffTestRecords.Eof()));
        Assert.IsTrue(reader.Read());

        BiffNumberRecord first = reader.GetNumber();
        BiffNumberRecord second = reader.GetNumber();

        Assert.AreEqual(first, second);
        Assert.AreEqual(18, reader.BytesConsumed);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Eof, reader.RecordType);
    }
}
