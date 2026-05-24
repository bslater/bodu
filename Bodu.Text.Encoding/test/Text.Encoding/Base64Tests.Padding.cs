// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Padding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that the Standard variant emits the expected number of trailing <c>=</c> characters for each input
    /// length mod 3.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expectedPaddingCount">The expected count of trailing <c>=</c>.</param>
    [DataTestMethod]
    [DataRow(1, 2)]
    [DataRow(2, 1)]
    [DataRow(3, 0)]
    [DataRow(4, 2)]
    [DataRow(5, 1)]
    [DataRow(6, 0)]
    public void Encode_WhenStandardVariant_ShouldEmitExpectedPaddingCount(int byteCount, int expectedPaddingCount)
    {
        var bytes = new byte[byteCount];
        var actual = Base64.Encode(bytes);

        var actualPadding = 0;
        for (var i = actual.Length - 1; i >= 0 && actual[i] == '='; i--)
        {
            actualPadding++;
        }

        Assert.AreEqual(expectedPaddingCount, actualPadding);
        Assert.AreEqual(0, actual.Length % 4, "Padded Base64 output length must be a multiple of 4.");
    }

    /// <summary>
    /// Verifies that the encoded length predicted by <see cref="Base64.GetEncodedLength" /> matches the actual length
    /// across input sizes, variants, and padding flags.
    /// </summary>
    /// <param name="variantValue">The variant under test.</param>
    /// <param name="omitPadding">Whether to omit padding.</param>
    [DataTestMethod]
    [DataRow(Base64Variant.Standard, false)]
    [DataRow(Base64Variant.Standard, true)]
    [DataRow(Base64Variant.UrlSafe, false)]
    [DataRow(Base64Variant.UrlSafe, true)]
    [DataRow(Base64Variant.Mime, false)]
    public void GetEncodedLength_ShouldMatchActualEncodedLengthAcrossInputs(Base64Variant variantValue, bool omitPadding)
    {
        BaseFormattingOptions options = omitPadding ? BaseFormattingOptions.OmitPadding : BaseFormattingOptions.None;

        for (var n = 0; n <= 80; n++)
        {
            var bytes = new byte[n];
            var predicted = Base64.GetEncodedLength(n, variantValue, options);
            var actual = Base64.Encode(bytes, variantValue, options).Length;

            Assert.AreEqual(predicted, actual,
                $"Mismatch for length={n}, variant={variantValue}, omitPadding={omitPadding}.");
        }
    }

}
