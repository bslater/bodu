// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetBoundSheet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 BOUNDSHEET record decodes its offset, state, type, and compressed name.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenBiff8Compressed_ShouldDecode()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.BoundSheet8(0x1234, BiffSheetState.Hidden, BiffSheetType.Worksheet, "Data"), BiffRecordType.BoundSheet);

        BiffBoundSheetRecord sheet = reader.GetBoundSheet();

        Assert.AreEqual(0x1234u, sheet.StreamOffset);
        Assert.AreEqual(BiffSheetState.Hidden, sheet.State);
        Assert.AreEqual(BiffSheetType.Worksheet, sheet.SheetType);
        Assert.AreEqual("Data", sheet.Name.GetString());
        Assert.IsTrue(sheet.Name.IsUnicode);
        Assert.IsFalse(sheet.Name.IsHighByte);
    }

    /// <summary>
    /// Verifies that a BIFF8 BOUNDSHEET record with a 16-bit name decodes it as UTF-16.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenBiff8Wide_ShouldDecodeUtf16Name()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.BoundSheet8(0, BiffSheetState.Visible, BiffSheetType.Chart, "Résumé", wide: true), BiffRecordType.BoundSheet);

        BiffBoundSheetRecord sheet = reader.GetBoundSheet();

        Assert.AreEqual("Résumé", sheet.Name.GetString());
        Assert.IsTrue(sheet.Name.IsHighByte);
        Assert.AreEqual(BiffSheetType.Chart, sheet.SheetType);
    }

    /// <summary>
    /// Verifies that a BIFF5 BOUNDSHEET record decodes its byte-string name with the active code page.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenBiff5_ShouldDecodeByteStringName()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.BoundSheet5(0x40, BiffSheetState.VeryHidden, BiffSheetType.MacroSheet, [0x53, 0x68, 0xE9, 0x65, 0x74]), BiffRecordType.BoundSheet);

        BiffBoundSheetRecord sheet = reader.GetBoundSheet();

        Assert.AreEqual(0x40u, sheet.StreamOffset);
        Assert.AreEqual(BiffSheetState.VeryHidden, sheet.State);
        Assert.AreEqual(BiffSheetType.MacroSheet, sheet.SheetType);
        Assert.IsFalse(sheet.Name.IsUnicode);
        Assert.AreEqual("Shéet", sheet.Name.GetString());
    }

    /// <summary>
    /// Verifies that only the two low bits of the state byte select the visibility.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenStateByteHasHighBits_ShouldMaskToVisibility()
    {
        byte[] record = BiffTestRecords.BoundSheet8(0, (BiffSheetState)0xFD, BiffSheetType.Worksheet, "S");
        BiffReader reader = ReadTo8(record, BiffRecordType.BoundSheet);

        Assert.AreEqual(BiffSheetState.Hidden, reader.GetBoundSheet().State);
    }

    /// <summary>
    /// Verifies that an empty sheet name decodes as an empty string under both versions.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenNameIsEmpty_ShouldReturnEmptyString()
    {
        BiffReader biff8 = ReadTo8(BiffTestRecords.BoundSheet8(0, BiffSheetState.Visible, BiffSheetType.Worksheet, string.Empty), BiffRecordType.BoundSheet);
        BiffReader biff5 = ReadTo5(BiffTestRecords.BoundSheet5(0, BiffSheetState.Visible, BiffSheetType.Worksheet, []), BiffRecordType.BoundSheet);

        Assert.AreEqual(string.Empty, biff8.GetBoundSheet().Name.GetString());
        Assert.AreEqual(string.Empty, biff5.GetBoundSheet().Name.GetString());
    }

    /// <summary>
    /// Verifies that an unrecognized sheet-type byte is preserved rather than mapped or rejected.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenSheetTypeIsUnknown_ShouldPreserveRawValue()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.BoundSheet8(9, BiffSheetState.Visible, (BiffSheetType)0x40, "x"), BiffRecordType.BoundSheet);

        BiffBoundSheetRecord sheet = reader.GetBoundSheet();

        Assert.AreEqual((BiffSheetType)0x40, sheet.SheetType);
        Assert.AreEqual(9u, sheet.StreamOffset);
    }

    /// <summary>
    /// Verifies that a record whose name is declared longer than the payload holds is rejected.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenNameOverruns_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Record(BiffRecordType.BoundSheet, [0, 0, 0, 0, 0, 0, 0x05, 0x00, (byte)'a']));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.BoundSheet);
            _ = reader.GetBoundSheet();
        });
    }

    /// <summary>
    /// Verifies that a BIFF5 name is decoded with the code page in effect from the preceding CODEPAGE record.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenBiff5CodePageDeclared_ShouldDecodeNameWithIt()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.CodePage(437), BiffTestRecords.BoundSheet5(0, BiffSheetState.Visible, BiffSheetType.Worksheet, [0xE9]));
        BiffReader reader = ReadTo(stream, BiffRecordType.BoundSheet);

        Assert.AreEqual("Θ", reader.GetBoundSheet().Name.GetString());
    }

    /// <summary>
    /// Verifies that the largest stream offset the record can carry decodes without sign confusion.
    /// </summary>
    [TestMethod]
    public void GetBoundSheet_WhenOffsetIsMaximum_ShouldDecodeUnsigned()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.BoundSheet8(uint.MaxValue, BiffSheetState.Visible, BiffSheetType.Worksheet, "x"), BiffRecordType.BoundSheet);

        Assert.AreEqual(uint.MaxValue, reader.GetBoundSheet().StreamOffset);
    }
}
