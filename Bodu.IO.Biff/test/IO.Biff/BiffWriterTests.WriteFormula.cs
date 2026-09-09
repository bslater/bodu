// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteFormula.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Verifies that a numeric formula result decodes back with its flags and tokens.
    /// </summary>
    [TestMethod]
    public void WriteFormula_WhenNumericResult_ShouldDecodeBack()
    {
        byte[] tokens = [0x1E, 0x2A, 0x00];
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteFormula(2, 3, 4, 42.0, tokens, flags: 0x0002));

        BiffFormulaRecord formula = Single(bytes).GetFormula();
        Assert.AreEqual(BiffCachedResultKind.Number, formula.CachedResultKind);
        Assert.AreEqual(42.0, formula.NumberValue);
        Assert.AreEqual(0x0002, formula.Flags);
        CollectionAssert.AreEqual(tokens, formula.Tokens.ToArray());
        CollectionAssert.AreEqual(BiffTestRecords.Formula(2, 3, BiffTestRecords.NumberResult(42.0), tokens, 0x0002, 4), bytes);
    }

    /// <summary>
    /// Verifies that each special cached-result kind decodes back with its value.
    /// </summary>
    /// <param name="kind">The result kind.</param>
    /// <param name="value">The value byte.</param>
    [TestMethod]
    [DataRow(BiffCachedResultKind.String, (byte)0)]
    [DataRow(BiffCachedResultKind.Boolean, (byte)1)]
    [DataRow(BiffCachedResultKind.Error, (byte)0x07)]
    [DataRow(BiffCachedResultKind.Empty, (byte)0)]
    public void WriteFormula_WhenSpecialResult_ShouldDecodeBack(BiffCachedResultKind kind, byte value)
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteFormula(0, 0, 0, kind, value, default));

        BiffFormulaRecord formula = Single(bytes).GetFormula();
        Assert.AreEqual(kind, formula.CachedResultKind);
        Assert.AreEqual(value, formula.RawValue);
        Assert.IsTrue(formula.Tokens.IsEmpty);
    }

    /// <summary>
    /// Verifies that the special-result overload rejects the numeric kind.
    /// </summary>
    [TestMethod]
    public void WriteFormula_WhenSpecialOverloadGivenNumber_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Emit8((ref BiffWriter w) => w.WriteFormula(0, 0, 0, BiffCachedResultKind.Number, 0, default)));
    }

    /// <summary>
    /// Verifies that a string formula result followed by a STRING record decodes back as a pair.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenFollowingFormula_ShouldDecodeBack()
    {
        byte[] bytes = Emit8((ref BiffWriter w) =>
        {
            w.WriteFormula(0, 0, 0, BiffCachedResultKind.String, 0, default);
            w.WriteString("résultat");
        });

        var reader = new BiffReader(bytes, new BiffReaderOptions { Version = BiffVersion.Biff8 });
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffCachedResultKind.String, reader.GetFormula().CachedResultKind);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual("résultat", reader.GetString().Text.GetString());
    }

    /// <summary>
    /// Verifies that tokens exceeding the record are rejected.
    /// </summary>
    [TestMethod]
    public void WriteFormula_WhenTokensTooLong_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Emit5((ref BiffWriter w) => w.WriteFormula(0, 0, 0, 1.0, new byte[BiffLimits.Biff5MaxPayloadLength])));
    }
}
