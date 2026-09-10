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

    /// <summary>
    /// Verifies that a record too short to carry the token length, but long enough for the result, decodes with
    /// empty tokens whatever its exact length.
    /// </summary>
    /// <param name="length">The payload length.</param>
    [TestMethod]
    [DataRow(15)]
    [DataRow(20)]
    [DataRow(21)]
    public void GetFormula_WhenRecordEndsBeforeTokenLength_ShouldDecodeWithEmptyTokens(int length)
    {
        byte[] payload = new byte[length];
        payload[14] = 0x01;
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.Formula, payload), BiffRecordType.Formula);

        BiffFormulaRecord formula = reader.GetFormula();

        Assert.AreEqual(0, formula.Flags, "Flags are read only when the token length field is present too.");
        Assert.IsTrue(formula.Tokens.IsEmpty);
    }

    /// <summary>
    /// Verifies that a record declaring fewer token bytes than it holds exposes only the declared tokens.
    /// </summary>
    [TestMethod]
    public void GetFormula_WhenRecordHoldsMoreBytesThanDeclaredTokens_ShouldExposeDeclaredTokensOnly()
    {
        byte[] record = BiffTestRecords.Formula(0, 0, BiffTestRecords.NumberResult(1), [0xAA, 0xBB, 0xCC]);
        record[4 + 20] = 2;
        BiffReader reader = ReadTo8(record, BiffRecordType.Formula);

        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB }, reader.GetFormula().Tokens.ToArray());
    }

    /// <summary>
    /// Verifies that a numeric result whose trailing bytes are not both 0xFF is a number, including values whose
    /// high bytes happen to be 0xFF.
    /// </summary>
    [TestMethod]
    public void GetFormula_WhenResultTrailerIsNotMarker_ShouldDecodeAsNumber()
    {
        byte[] result = [0, 0, 0, 0, 0, 0, 0xFF, 0x7F];
        BiffReader reader = ReadTo8(BiffTestRecords.Formula(0, 0, result), BiffRecordType.Formula);

        BiffFormulaRecord formula = reader.GetFormula();

        Assert.AreEqual(BiffCachedResultKind.Number, formula.CachedResultKind);
        Assert.AreEqual(System.Buffers.Binary.BinaryPrimitives.ReadDoubleLittleEndian(result), formula.NumberValue);
    }

    /// <summary>
    /// Verifies that a numeric result of negative zero, NaN, or infinity round-trips through the eight-byte field.
    /// </summary>
    /// <param name="value">The cached number.</param>
    [TestMethod]
    [DataRow(-0.0)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(double.Epsilon)]
    public void GetFormula_WhenNumberIsSpecialValue_ShouldPreserveBits(double value)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Formula(0, 0, BiffTestRecords.NumberResult(value)), BiffRecordType.Formula);

        double decoded = reader.GetFormula().NumberValue;

        Assert.AreEqual(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(decoded));
    }

    /// <summary>
    /// Verifies that a boolean cached result with a value byte other than one is reported as true.
    /// </summary>
    [TestMethod]
    public void GetFormula_WhenBooleanByteIsNotOne_ShouldReportTrue()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Formula(0, 0, BiffTestRecords.SpecialResult(1, 0x7F)), BiffRecordType.Formula);

        Assert.IsTrue(reader.GetFormula().BooleanValue);
    }
}
