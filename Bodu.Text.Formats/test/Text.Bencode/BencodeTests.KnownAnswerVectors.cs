// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.KnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Parse(ReadOnlySpan{byte})" /> reproduces the canonical value for every
    /// BEP 3 Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="BencodeKnownAnswerVectors.Bep3PositiveVectors" />.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(BencodeKnownAnswerVectors.Bep3PositiveVectors), typeof(BencodeKnownAnswerVectors))]
    public void Decode_ForBep3KnownAnswerVector_ShouldReturnExpectedValue(BencodeKnownAnswerVector vector)
    {
        BencodedValue actual = Bencode.Parse(vector.EncodedBytes);

        AssertValuesEqual(vector.DecodedValue, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Parse(ReadOnlySpan{byte})" /> reproduces the canonical value for every
    /// curated edge-case KAT vector (Int64 boundaries, nested containers, raw byte payloads).
    /// </summary>
    /// <param name="vector">A KAT vector sourced from
    /// <see cref="BencodeKnownAnswerVectors.EdgeCasePositiveVectors" />.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(BencodeKnownAnswerVectors.EdgeCasePositiveVectors), typeof(BencodeKnownAnswerVectors))]
    public void Decode_ForEdgeCaseKnownAnswerVector_ShouldReturnExpectedValue(BencodeKnownAnswerVector vector)
    {
        BencodedValue actual = Bencode.Parse(vector.EncodedBytes);

        AssertValuesEqual(vector.DecodedValue, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Parse(ReadOnlySpan{byte})" /> reproduces the canonical value for every
    /// Wikipedia Bencode KAT vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="BencodeKnownAnswerVectors.WikipediaVectors" />.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(BencodeKnownAnswerVectors.WikipediaVectors), typeof(BencodeKnownAnswerVectors))]
    public void Decode_ForWikipediaKnownAnswerVector_ShouldReturnExpectedValue(BencodeKnownAnswerVector vector)
    {
        BencodedValue actual = Bencode.Parse(vector.EncodedBytes);

        AssertValuesEqual(vector.DecodedValue, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Format(BencodedValue)" /> reproduces the canonical bytes for every BEP 3
    /// Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="BencodeKnownAnswerVectors.Bep3PositiveVectors" />.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(BencodeKnownAnswerVectors.Bep3PositiveVectors), typeof(BencodeKnownAnswerVectors))]
    public void Encode_ForBep3KnownAnswerVector_ShouldProduceExpectedBytes(BencodeKnownAnswerVector vector)
    {
        var actual = Bencode.Format(vector.DecodedValue);

        CollectionAssert.AreEqual(vector.EncodedBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Format(BencodedValue)" /> reproduces the canonical bytes for every curated
    /// edge-case KAT vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from
    /// <see cref="BencodeKnownAnswerVectors.EdgeCasePositiveVectors" />.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(BencodeKnownAnswerVectors.EdgeCasePositiveVectors), typeof(BencodeKnownAnswerVectors))]
    public void Encode_ForEdgeCaseKnownAnswerVector_ShouldProduceExpectedBytes(BencodeKnownAnswerVector vector)
    {
        var actual = Bencode.Format(vector.DecodedValue);

        CollectionAssert.AreEqual(vector.EncodedBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Format(BencodedValue)" /> reproduces the canonical bytes for every
    /// Wikipedia Bencode KAT vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="BencodeKnownAnswerVectors.WikipediaVectors" />.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(BencodeKnownAnswerVectors.WikipediaVectors), typeof(BencodeKnownAnswerVectors))]
    public void Encode_ForWikipediaKnownAnswerVector_ShouldProduceExpectedBytes(BencodeKnownAnswerVector vector)
    {
        var actual = Bencode.Format(vector.DecodedValue);

        CollectionAssert.AreEqual(vector.EncodedBytes, actual);
    }

    /// <summary>
    /// Compares two <see cref="BencodedValue" /> instances structurally by recursing through lists and
    /// dictionaries. Bencode values do not implement structural equality on their own (only
    /// <see cref="BencodedInteger" /> and <see cref="BencodedString" /> do), so the test helper performs the
    /// recursion.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The decoded value.</param>
    private static void AssertValuesEqual(BencodedValue expected, BencodedValue actual)
    {
        Assert.AreEqual(expected.Kind, actual.Kind);

        switch (expected)
        {
            case BencodedInteger expectedInteger:
                Assert.AreEqual(expectedInteger.Value, ((BencodedInteger)actual).Value);
                return;

            case BencodedString expectedString:
                CollectionAssert.AreEqual(expectedString.Bytes.ToArray(), ((BencodedString)actual).Bytes.ToArray());
                return;

            case BencodedList expectedList:
                var actualList = (BencodedList)actual;
                Assert.AreEqual(expectedList.Count, actualList.Count);
                for (var i = 0; i < expectedList.Count; i++)
                    AssertValuesEqual(expectedList[i], actualList[i]);
                return;

            case BencodedDictionary expectedDict:
                var actualDict = (BencodedDictionary)actual;
                Assert.AreEqual(expectedDict.Count, actualDict.Count);
                using (IEnumerator<KeyValuePair<BencodedString, BencodedValue>> e = expectedDict.GetOrderedItems().GetEnumerator())
                using (IEnumerator<KeyValuePair<BencodedString, BencodedValue>> a = actualDict.GetOrderedItems().GetEnumerator())
                {
                    while (e.MoveNext())
                    {
                        Assert.IsTrue(a.MoveNext());
                        CollectionAssert.AreEqual(e.Current.Key.Bytes.ToArray(), a.Current.Key.Bytes.ToArray());
                        AssertValuesEqual(e.Current.Value, a.Current.Value);
                    }
                }

                return;

            default:
                Assert.Fail($"Unexpected value kind {expected.Kind}");
                return;
        }
    }

}
