// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingTests.Corpus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Encoding;

public sealed partial class PercentEncodingTests
{
    /// <summary>
    /// Verifies that <see cref="PercentEncoding.Encode(ReadOnlySpan{byte}, PercentEncodingMode)" /> reproduces every
    /// RFC 3986 <see cref="PercentEncodingMode.UriComponent" /> Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(PercentEncodingKnownAnswerVectors.UriComponentVectors), typeof(PercentEncodingKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encode_ForUriComponentKnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        Assert.AreEqual(vector.Encoded, PercentEncoding.Encode(vector.DecodedBytes, PercentEncodingMode.UriComponent));
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.Decode(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" />
    /// recovers the original bytes for every RFC 3986 <see cref="PercentEncodingMode.UriComponent" /> vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(PercentEncodingKnownAnswerVectors.UriComponentVectors), typeof(PercentEncodingKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_ForUriComponentKnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        byte[] actual = PercentEncoding.Decode(vector.Encoded.AsSpan(), PercentEncodingMode.UriComponent);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.Encode(ReadOnlySpan{byte}, PercentEncodingMode)" /> reproduces every
    /// WHATWG <see cref="PercentEncodingMode.FormUrlEncoded" /> Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(PercentEncodingKnownAnswerVectors.FormUrlEncodedVectors), typeof(PercentEncodingKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encode_ForFormUrlEncodedKnownAnswerVector_ShouldMatch(EncodingKnownAnswerVector vector)
    {
        Assert.AreEqual(vector.Encoded, PercentEncoding.Encode(vector.DecodedBytes, PercentEncodingMode.FormUrlEncoded));
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.Decode(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" />
    /// recovers the original bytes for every WHATWG <see cref="PercentEncodingMode.FormUrlEncoded" /> vector.
    /// </summary>
    /// <param name="vector">A KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(PercentEncodingKnownAnswerVectors.FormUrlEncodedVectors), typeof(PercentEncodingKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_ForFormUrlEncodedKnownAnswerVector_ShouldRecoverBytes(EncodingKnownAnswerVector vector)
    {
        byte[] actual = PercentEncoding.Decode(vector.Encoded.AsSpan(), PercentEncodingMode.FormUrlEncoded);

        CollectionAssert.AreEqual(vector.DecodedBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="PercentEncoding.Decode(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" />
    /// throws the expected exception type for every malformed Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A negative KAT vector.</param>
    [TestMethod]
    [DynamicData(nameof(PercentEncodingKnownAnswerVectors.NegativeVectors), typeof(PercentEncodingKnownAnswerVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decode_ForMalformedKnownAnswerVector_ShouldThrowExactly(EncodingNegativeDecodeVector vector)
    {
        Exception? actual = null;
        try
        {
            _ = PercentEncoding.Decode(vector.MalformedInput.AsSpan());
        }
        catch (Exception ex)
        {
            actual = ex;
        }

        Assert.IsNotNull(actual, $"No exception thrown for: {vector}");
        Assert.AreEqual(vector.ExpectedExceptionType, actual.GetType(), $"Wrong exception type for: {vector}.");
    }
}
