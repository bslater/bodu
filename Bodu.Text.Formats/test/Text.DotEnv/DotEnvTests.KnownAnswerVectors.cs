// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.KnownAnswerVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvTests
{

    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char}, DotEnvParseOptions)" /> produces a document whose
    /// entries match the expected key/value pairs for every dotenv community specification Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="DotEnvKnownAnswerVectors.SpecVectors" />.</param>
    [TestMethod]
    [DynamicData(nameof(DotEnvKnownAnswerVectors.SpecVectors), typeof(DotEnvKnownAnswerVectors))]
    public void Parse_ForSpecKnownAnswerVector_ShouldProduceExpectedDocument(DotEnvKnownAnswerVector vector)
    {
        DotEnvParseOptions options = vector.Options ?? DotEnvParseOptions.Default;

        DotEnvDocument doc = DotEnv.Parse(vector.Input, options);

        Assert.AreEqual(vector.ExpectedEntries.Length, doc.Entries.Count,
            $"Entry count mismatch for vector: {vector.Description}");

        for (var i = 0; i < vector.ExpectedEntries.Length; i++)
        {
            Assert.AreEqual(vector.ExpectedEntries[i].Key, doc.Entries[i].Key,
                $"Key mismatch at index {i} for vector: {vector.Description}");

            Assert.AreEqual(vector.ExpectedEntries[i].Value, doc.Entries[i].Value,
                $"Value mismatch at index {i} for vector: {vector.Description}");
        }
    }

    /// <summary>
    /// Verifies that a round-trip through <see cref="DotEnv.Parse(ReadOnlySpan{char}, DotEnvParseOptions)" /> and
    /// <see cref="DotEnv.Format(DotEnvDocument)" /> preserves all keys and values for every dotenv community
    /// specification Known Answer Test vector.
    /// </summary>
    /// <param name="vector">A KAT vector sourced from <see cref="DotEnvKnownAnswerVectors.SpecVectors" />.</param>
    [TestMethod]
    [DynamicData(nameof(DotEnvKnownAnswerVectors.SpecVectors), typeof(DotEnvKnownAnswerVectors))]
    public void Format_ForSpecKnownAnswerVector_ShouldRoundTripDocument(DotEnvKnownAnswerVector vector)
    {
        DotEnvParseOptions options = vector.Options ?? DotEnvParseOptions.Default;

        DotEnvDocument original = DotEnv.Parse(vector.Input, options);
        var formatted = DotEnv.Format(original);
        DotEnvDocument roundTripped = DotEnv.Parse(formatted, options);

        Assert.AreEqual(vector.ExpectedEntries.Length, roundTripped.Entries.Count,
            $"Round-trip entry count mismatch for vector: {vector.Description}");

        for (var i = 0; i < vector.ExpectedEntries.Length; i++)
        {
            Assert.AreEqual(vector.ExpectedEntries[i].Key, roundTripped.Entries[i].Key,
                $"Round-trip key mismatch at index {i} for vector: {vector.Description}");

            Assert.AreEqual(vector.ExpectedEntries[i].Value, roundTripped.Entries[i].Value,
                $"Round-trip value mismatch at index {i} for vector: {vector.Description}");
        }
    }

}
