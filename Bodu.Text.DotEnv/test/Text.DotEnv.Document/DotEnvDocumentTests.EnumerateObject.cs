// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvDocumentTests.EnumerateObject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Document;

/// <summary>
/// Contains the <see cref="DotEnvElement.EnumerateObject" /> tests over the root of a parsed
/// <see cref="DotEnvDocument" />.
/// </summary>
public partial class DotEnvDocumentTests
{
    /// <summary>
    /// Verifies that enumerating a document whose key is defined twice yields both definitions in source order, even
    /// though property lookup resolves the key to the last one.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenKeyRepeated_ShouldYieldEveryEntryInSourceOrder()
    {
        using DotEnvDocument document = DotEnvDocument.Parse("MULTI1=foo\nOTHER=x\nMULTI1=bar\n");

        var entries = new List<string>();
        foreach (DotEnvProperty property in document.RootElement.EnumerateObject())
            entries.Add($"{property.Name}={property.Value.GetString()}");

        CollectionAssert.AreEqual(new List<string> { "MULTI1=foo", "OTHER=x", "MULTI1=bar" }, entries, string.Join(" | ", entries));
    }
}
