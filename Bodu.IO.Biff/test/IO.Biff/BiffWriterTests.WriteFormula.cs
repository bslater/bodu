// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteFormula.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

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

    /// <summary>
    /// Verifies that tokens exactly filling the record are accepted and one more byte is rejected with the tokens
    /// parameter named, under each version.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteFormula_WhenTokensAtCapacity_ShouldAcceptAndRejectOneOver(BiffVersion version)
    {
        int max = BiffLimits.GetMaxPayloadLength(version) - 22;
        byte[] tokens = [.. Enumerable.Range(0, max).Select(i => (byte)i)];

        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteFormula(0, 0, 0, 1.0, tokens));
        BiffReader reader = Single(bytes, version);
        Assert.AreEqual(BiffLimits.GetMaxPayloadLength(version), reader.RecordLength);
        CollectionAssert.AreEqual(tokens, reader.GetFormula().Tokens.ToArray());

        byte[] tooMany = new byte[max + 1];
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit(version, (ref BiffWriter w) => w.WriteFormula(0, 0, 0, 1.0, tooMany)),
            "tokens");
    }

    /// <summary>
    /// Verifies that an undefined cached-result kind is rejected with the kind parameter named.
    /// </summary>
    [TestMethod]
    public void WriteFormula_WhenResultKindUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteFormula(0, 0, 0, (BiffCachedResultKind)99, 0, default)),
            "cachedResultKind");
    }

    /// <summary>
    /// Verifies that the value byte is written only for boolean and error results, so a string or empty result
    /// always decodes with a zero raw value.
    /// </summary>
    /// <param name="kind">The result kind.</param>
    [TestMethod]
    [DataRow(BiffCachedResultKind.String)]
    [DataRow(BiffCachedResultKind.Empty)]
    public void WriteFormula_WhenResultCarriesNoValue_ShouldIgnoreValueByte(BiffCachedResultKind kind)
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteFormula(0, 0, 0, kind, 0xAB, default));

        BiffFormulaRecord formula = Single(bytes).GetFormula();
        Assert.AreEqual(kind, formula.CachedResultKind);
        Assert.AreEqual(0, formula.RawValue);
    }

    /// <summary>
    /// Verifies that a special result with tokens and flags round-trips every field, and its result bytes match the
    /// documented marker layout.
    /// </summary>
    [TestMethod]
    public void WriteFormula_WhenSpecialResultWithTokensAndFlags_ShouldRoundTripAllFields()
    {
        byte[] tokens = [0x1E, 0x01, 0x00];
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteFormula(4, 5, 6, BiffCachedResultKind.Error, 0x2A, tokens, flags: 0x0009));

        BiffFormulaRecord formula = Single(bytes, BiffVersion.Biff5).GetFormula();
        Assert.AreEqual(4, formula.Row);
        Assert.AreEqual(5, formula.Column);
        Assert.AreEqual(6, formula.XfIndex);
        Assert.AreEqual(BiffCachedResultKind.Error, formula.CachedResultKind);
        Assert.AreEqual(0x2A, formula.ErrorCode);
        Assert.AreEqual(0x0009, formula.Flags);
        CollectionAssert.AreEqual(tokens, formula.Tokens.ToArray());
        CollectionAssert.AreEqual(BiffTestRecords.Formula(4, 5, BiffTestRecords.SpecialResult(2, 0x2A), tokens, 0x0009, 6), bytes);
    }
}
