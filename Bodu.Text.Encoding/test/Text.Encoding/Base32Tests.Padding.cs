// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Padding.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that the Standard variant emits the expected number of trailing <c>=</c> characters for each input
    /// length mod 5.
    /// </summary>
    /// <param name="byteCount">The input length.</param>
    /// <param name="expectedPaddingCount">The expected count of trailing <c>=</c>.</param>
    [DataTestMethod]
    [DataRow(1, 6)]
    [DataRow(2, 4)]
    [DataRow(3, 3)]
    [DataRow(4, 1)]
    [DataRow(5, 0)]
    [DataRow(6, 6)]
    [DataRow(10, 0)]
    public void Encode_WhenStandardVariant_ShouldEmitExpectedPaddingCount(int byteCount, int expectedPaddingCount)
    {
        var bytes = new byte[byteCount];
        var actual = Base32.Encode(bytes);

        var actualPadding = 0;
        for (var i = actual.Length - 1; i >= 0 && actual[i] == '='; i--)
        {
            actualPadding++;
        }

        Assert.AreEqual(expectedPaddingCount, actualPadding);
        Assert.AreEqual(0, actual.Length % 8, "Padded output length must be a multiple of 8.");
    }

    /// <summary>
    /// Verifies that the encoded length predicted by <see cref="Base32.GetEncodedLength" /> matches the actual length
    /// across input sizes and padding modes.
    /// </summary>
    /// <param name="omitPadding">Whether to omit padding.</param>
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetEncodedLength_ShouldMatchActualEncodedLengthAcrossInputs(bool omitPadding)
    {
        BaseFormattingOptions options = omitPadding ? BaseFormattingOptions.OmitPadding : BaseFormattingOptions.None;

        for (var n = 0; n <= 25; n++)
        {
            var bytes = new byte[n];
            var predicted = Base32.GetEncodedLength(n, Base32Variant.Standard, options);
            var actual = Base32.Encode(bytes, Base32Variant.Standard, options).Length;

            Assert.AreEqual(predicted, actual, $"Mismatch for length={n}, omitPadding={omitPadding}.");
        }
    }

}
