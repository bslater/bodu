// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.FormattingOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that <see cref="BaseFormattingOptions.IncludePrefix" /> combined with
    /// <see cref="BaseFormattingOptions.InsertSpacing" /> emits the prefix once at the start with bytes spaced after.
    /// </summary>
    [TestMethod]
    public void Encode_WhenIncludePrefixAndInsertSpacing_ShouldEmitPrefixOnceFollowedBySpacedBytes()
    {
        var actual = Base16.Encode(
            CanonicalBytes,
            BaseFormattingOptions.IncludePrefix | BaseFormattingOptions.InsertSpacing);

        Assert.AreEqual("0xde ad be ef", actual);
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormattingOptions.InsertLineBreaks" /> combined with
    /// <see cref="BaseFormattingOptions.InsertSpacing" /> still respects the line interval and produces a
    /// space-separated, line-wrapped result.
    /// </summary>
    [TestMethod]
    public void Encode_WhenInsertLineBreaksAndInsertSpacing_ShouldWrapAtIntervalAndKeepSpacing()
    {
        var bytes = new byte[40];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }

        var actual = Base16.Encode(
            bytes,
            BaseFormattingOptions.InsertLineBreaks | BaseFormattingOptions.InsertSpacing);

        Assert.IsTrue(actual.Contains("\r\n"), "Output should contain a line-break sequence.");
        Assert.IsTrue(actual.Contains(' '), "Output should contain space-separated byte pairs.");
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormattingOptions.InsertSpacing" /> with
    /// <see cref="BaseFormattingOptions.UpperCase" /> separates upper case byte pairs with a single space.
    /// </summary>
    [TestMethod]
    public void Encode_WhenInsertSpacingAndUpperCase_ShouldInterleaveSingleSpaceBetweenBytes()
    {
        var actual = Base16.Encode(
            CanonicalBytes,
            BaseFormattingOptions.InsertSpacing | BaseFormattingOptions.UpperCase);

        Assert.AreEqual("DE AD BE EF", actual);
        Assert.IsFalse(actual.EndsWith(' '));
    }
    /// <summary>
    /// Verifies that <see cref="BaseFormattingOptions.UpperCase" /> combined with
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> yields a prefixed upper case string.
    /// </summary>
    [TestMethod]
    public void Encode_WhenUpperCaseAndIncludePrefix_ShouldEmitUpperCaseWithPrefix()
    {
        var actual = Base16.Encode(
            CanonicalBytes,
            BaseFormattingOptions.UpperCase | BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual("0xDEADBEEF", actual);
    }

    /// <summary>
    /// Verifies that the encoded length reported by <see cref="Base16.GetEncodedLength" /> matches the actual encoded
    /// string length for every combination of flags.
    /// </summary>
    /// <param name="flags">The combination of flags under test.</param>
    [DataTestMethod]
    [DataRow((byte)BaseFormattingOptions.None)]
    [DataRow((byte)BaseFormattingOptions.UpperCase)]
    [DataRow((byte)BaseFormattingOptions.IncludePrefix)]
    [DataRow((byte)BaseFormattingOptions.InsertSpacing)]
    [DataRow((byte)BaseFormattingOptions.InsertLineBreaks)]
    [DataRow((byte)(BaseFormattingOptions.IncludePrefix | BaseFormattingOptions.InsertSpacing))]
    [DataRow((byte)(BaseFormattingOptions.IncludePrefix | BaseFormattingOptions.InsertLineBreaks))]
    [DataRow((byte)(BaseFormattingOptions.InsertSpacing | BaseFormattingOptions.InsertLineBreaks))]
    [DataRow((byte)(BaseFormattingOptions.IncludePrefix | BaseFormattingOptions.InsertSpacing | BaseFormattingOptions.InsertLineBreaks))]
    public void GetEncodedLength_ShouldMatchActualEncodedLength(byte flags)
    {
        var options = (BaseFormattingOptions)flags;

        for (var byteCount = 0; byteCount <= 80; byteCount++)
        {
            var bytes = new byte[byteCount];
            for (var i = 0; i < byteCount; i++)
            {
                bytes[i] = (byte)i;
            }

            var predicted = Base16.GetEncodedLength(byteCount, options);
            var actual = Base16.Encode(bytes, options).Length;

            Assert.AreEqual(predicted, actual,
                $"Prediction mismatch for byteCount={byteCount}, options={options}.");
        }
    }

}
