// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvDocumentTests.TryGetProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Document;

/// <summary>
/// Contains the <see cref="DotEnvElement.TryGetProperty(string, out DotEnvElement)" /> tests over the root of a parsed
/// <see cref="DotEnvDocument" />.
/// </summary>
public partial class DotEnvDocumentTests
{
    /// <summary>
    /// Verifies that a key defined more than once resolves to the value of its last definition, wherever the other
    /// definitions are.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="expected">The value of the last definition.</param>
    [TestMethod]
    [DataRow("two definitions", "MULTI1=foo\nMULTI1=bar\n", "bar")]
    [DataRow("definitions with another key between them", "MULTI1=foo\nOTHER=x\nMULTI1=bar\n", "bar")]
    [DataRow("three definitions", "MULTI1=a\nMULTI1=b\nMULTI1=c\n", "c")]
    public void TryGetProperty_WhenKeyRepeated_ShouldReturnLastValue(string testName, string source, string expected)
    {
        _ = testName;
        using DotEnvDocument document = DotEnvDocument.Parse(source);

        bool found = document.RootElement.TryGetProperty("MULTI1", out DotEnvElement value);

        Assert.IsTrue(found);
        Assert.AreEqual(expected, value.GetString());
    }
}
