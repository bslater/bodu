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
}
