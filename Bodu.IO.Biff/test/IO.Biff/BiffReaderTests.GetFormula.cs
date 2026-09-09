// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetFormula.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a numeric cached result decodes as a number along with flags and tokens.
    /// </summary>
    [TestMethod]
    public void GetFormula_WhenNumericResult_ShouldDecodeNumberFlagsAndTokens()
    {
        byte[] tokens = [0x1E, 0x02, 0x00];
        BiffReader reader = ReadTo8(BiffTestRecords.Formula(1, 2, BiffTestRecords.NumberResult(2.5), tokens, flags: 0x0003, xf: 9), BiffRecordType.Formula);

        BiffFormulaRecord formula = reader.GetFormula();

        Assert.AreEqual(1, formula.Row);
        Assert.AreEqual(2, formula.Column);
        Assert.AreEqual(9, formula.XfIndex);
        Assert.AreEqual(BiffCachedResultKind.Number, formula.CachedResultKind);
        Assert.AreEqual(2.5, formula.NumberValue);
        Assert.AreEqual(0x0003, formula.Flags);
        CollectionAssert.AreEqual(tokens, formula.Tokens.ToArray());
    }

    /// <summary>
    /// Verifies that each non-numeric cached-result marker maps to its kind and value.
    /// </summary>
    /// <param name="kindByte">The marker's leading byte.</param>
    /// <param name="value">The value byte.</param>
    /// <param name="expected">The expected kind.</param>
    [TestMethod]
    [DataRow((byte)0, (byte)0, BiffCachedResultKind.String)]
    [DataRow((byte)1, (byte)1, BiffCachedResultKind.Boolean)]
    [DataRow((byte)2, (byte)0x07, BiffCachedResultKind.Error)]
    [DataRow((byte)3, (byte)0, BiffCachedResultKind.Empty)]
    [DataRow((byte)9, (byte)0, BiffCachedResultKind.Empty)]
    public void GetFormula_WhenSpecialResult_ShouldDecodeKind(byte kindByte, byte value, BiffCachedResultKind expected)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Formula(0, 0, BiffTestRecords.SpecialResult(kindByte, value)), BiffRecordType.Formula);

        BiffFormulaRecord formula = reader.GetFormula();

        Assert.AreEqual(expected, formula.CachedResultKind);
        Assert.AreEqual(value, formula.RawValue);
        if (expected == BiffCachedResultKind.Boolean)
            Assert.IsTrue(formula.BooleanValue);
        if (expected == BiffCachedResultKind.Error)
            Assert.AreEqual(0x07, formula.ErrorCode);
    }

    /// <summary>
    /// Verifies that a record holding only the fourteen-byte result prefix decodes with empty tokens.
    /// </summary>
    [TestMethod]
    public void GetFormula_WhenRecordOmitsTokens_ShouldDecodeWithEmptyTokens()
    {
        byte[] payload = new byte[14];
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.Formula, payload), BiffRecordType.Formula);

        BiffFormulaRecord formula = reader.GetFormula();

        Assert.AreEqual(BiffCachedResultKind.Number, formula.CachedResultKind);
        Assert.AreEqual(0, formula.Flags);
        Assert.IsTrue(formula.Tokens.IsEmpty);
    }

    /// <summary>
    /// Verifies that a token length exceeding the record is rejected.
    /// </summary>
    [TestMethod]
    public void GetFormula_WhenTokenLengthOverruns_ShouldThrowBiffFormatException()
    {
        byte[] payload = new byte[22];
        payload[20] = 0x05;
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Record(BiffRecordType.Formula, payload));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.Formula);
            _ = reader.GetFormula();
        });
    }
}
