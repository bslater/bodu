// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedTests.KnownAnswerVectors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedTests
{
    /// <summary>
    /// Verifies that <see cref="Delimited.Parse(ReadOnlySpan{char}, DelimitedParseOptions)" /> produces the
    /// expected headers and field values for every RFC 4180 Known Answer Test vector, covering each normative rule
    /// in §2 and widely-adopted dialect variants.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="DelimitedKnownAnswerVectors.Rfc4180Vectors" />.</param>
    [TestMethod]
    [DynamicData(nameof(DelimitedKnownAnswerVectors.Rfc4180Vectors), typeof(DelimitedKnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Parse_ForRfc4180KnownAnswerVector_ShouldProduceExpectedDocument(DelimitedKnownAnswerVector vector)
    {
        DelimitedParseOptions options = vector.Options ?? DelimitedParseOptions.Default;

        DelimitedDocument doc = Delimited.Parse(vector.Input, options);

        CollectionAssert.AreEqual(vector.ExpectedHeaders, doc.Headers.ToArray());
        Assert.AreEqual(vector.ExpectedRows.Length, doc.Rows.Count);

        for (int r = 0; r < vector.ExpectedRows.Length; r++)
        {
            string[] expectedFields = vector.ExpectedRows[r];
            DelimitedRow row = doc.Rows[r];

            Assert.AreEqual(expectedFields.Length, row.Count);

            for (int f = 0; f < expectedFields.Length; f++)
                Assert.AreEqual(expectedFields[f], row[f]);
        }
    }

    /// <summary>
    /// Verifies that parsing, formatting, and re-parsing an RFC 4180 Known Answer Test vector produces a document
    /// with identical headers and field values, confirming that <see cref="Delimited.Format(DelimitedDocument)" />
    /// emits output that <see cref="Delimited.Parse(ReadOnlySpan{char})" /> can faithfully reconstruct.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="DelimitedKnownAnswerVectors.Rfc4180Vectors" />.</param>
    [TestMethod]
    [DynamicData(nameof(DelimitedKnownAnswerVectors.Rfc4180Vectors), typeof(DelimitedKnownAnswerVectors), DynamicDataSourceType.Method)]
    public void Format_ForRfc4180KnownAnswerVector_ShouldRoundTripDocument(DelimitedKnownAnswerVector vector)
    {
        DelimitedParseOptions options = vector.Options ?? DelimitedParseOptions.Default;

        DelimitedDocument original = Delimited.Parse(vector.Input, options);
        string formatted = Delimited.Format(original, options);
        DelimitedDocument roundTripped = Delimited.Parse(formatted, options);

        CollectionAssert.AreEqual(vector.ExpectedHeaders, roundTripped.Headers.ToArray());
        Assert.AreEqual(vector.ExpectedRows.Length, roundTripped.Rows.Count);

        for (int r = 0; r < vector.ExpectedRows.Length; r++)
        {
            string[] expectedFields = vector.ExpectedRows[r];
            DelimitedRow row = roundTripped.Rows[r];

            Assert.AreEqual(expectedFields.Length, row.Count);

            for (int f = 0; f < expectedFields.Length; f++)
                Assert.AreEqual(expectedFields[f], row[f]);
        }
    }
}
