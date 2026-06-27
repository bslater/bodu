// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableTests.Corpus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Encoding;

public sealed partial class QuotedPrintableTests
{
    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.Encode(ReadOnlySpan{byte}, QuotedPrintableEncodingOptions)" />
    /// reproduces every RFC 2045 §6.7 Known Answer Test vector in binary mode.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(QuotedPrintableKnownAnswerVectors.BinaryVectors), typeof(QuotedPrintableKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encode_ForBinaryKnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        string actual = QuotedPrintable.Encode(vector.DecodedBytes);

        Assert.AreEqual(vector.Encoded, actual);
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.Decode(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" /> recovers
    /// the original bytes for every RFC 2045 §6.7 Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(QuotedPrintableKnownAnswerVectors.BinaryVectors), typeof(QuotedPrintableKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_ForBinaryKnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        byte[] actual = QuotedPrintable.Decode(vector.Encoded.AsSpan());

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.Decode(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" /> throws
    /// the expected exception type for every malformed Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(QuotedPrintableKnownAnswerVectors.NegativeVectors), typeof(QuotedPrintableKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_ForMalformedKnownAnswerVector_ShouldThrowExactly(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = QuotedPrintable.Decode(vector.MalformedInput.AsSpan());
        }
        catch (Exception ex)
        {
            actual = ex;
        }

        Assert.IsNotNull(actual, $"No exception thrown for: {vector}");
        Assert.AreEqual(vector.ExpectedExceptionType, actual.GetType(), $"Wrong exception type for: {vector}.");
    }
}
