// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.NegativeVectors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> throws the expected exception type for
    /// every malformed-input vector and, where the vector identifies a resource key, that the exception message
    /// matches the canonical resx text.
    /// </summary>
    /// <param name="vector">A negative KAT vector sourced from
    /// <see cref="BencodeKnownAnswerVectors.Bep3NegativeVectors" />.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(BencodeKnownAnswerVectors.Bep3NegativeVectors), typeof(BencodeKnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Decode_ForNegativeVector_ShouldThrowExpectedException(BencodeNegativeDecodeVector vector)
    {
        Exception ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(vector.EncodedBytes);
        });

        if (vector.ExpectedResourceKey is not null)
        {
            var expectedMessage = FormatsResourceStrings.ResourceManager.GetString(vector.ExpectedResourceKey, System.Globalization.CultureInfo.InvariantCulture)
                ?? throw new InvalidOperationException($"Missing resource key {vector.ExpectedResourceKey}");

            if (vector.ExpectedResourceKey == nameof(FormatsResourceStrings.Format_Invalid_BencodeUnexpectedToken))
            {
                // Composite-format message — assert that the canonical prefix (everything before the first
                // placeholder) appears in the thrown message.
                var firstPlaceholder = expectedMessage.IndexOf('{', StringComparison.Ordinal);
                var canonicalPrefix = expectedMessage[..firstPlaceholder];
                StringAssert.StartsWith(ex.Message, canonicalPrefix);
            }
            else
            {
                Assert.AreEqual(expectedMessage, ex.Message);
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.TryDecode(ReadOnlySpan{byte}, out BencodedValue, out int)" /> returns
    /// <see langword="false" /> for every malformed-input vector and produces default outputs.
    /// </summary>
    /// <param name="vector">A negative KAT vector sourced from
    /// <see cref="BencodeKnownAnswerVectors.Bep3NegativeVectors" />.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(BencodeKnownAnswerVectors.Bep3NegativeVectors), typeof(BencodeKnownAnswerVectors), DynamicDataSourceType.Method)]
    public void TryDecode_ForNegativeVector_ShouldReturnFalseWithDefaults(BencodeNegativeDecodeVector vector)
    {
        // Skip the trailing-data vector — TryDecode consumes only the value prefix and reports success, while
        // Decode (the strict variant) rejects trailing bytes. The negative vector set is shared across both.
        if (vector.ExpectedResourceKey == nameof(FormatsResourceStrings.Format_Invalid_BencodeTrailingData))
            return;

        var result = Bencode.TryDecode(vector.EncodedBytes, out BencodedValue? value, out var consumed);

        Assert.IsFalse(result);
        Assert.IsNull(value);
        Assert.AreEqual(0, consumed);
    }

}
