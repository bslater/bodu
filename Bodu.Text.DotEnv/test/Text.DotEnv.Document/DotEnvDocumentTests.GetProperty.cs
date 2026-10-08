// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvDocumentTests.GetProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Document;

/// <summary>
/// Contains the <see cref="DotEnvElement.GetProperty(string)" /> tests over the root of a parsed
/// <see cref="DotEnvDocument" />.
/// </summary>
public partial class DotEnvDocumentTests
{
    /// <summary>
    /// Verifies that a key defined twice resolves to the value of its last definition, as it does when the same text is
    /// parsed into a <c>DotEnvObject</c> or deserialized by <c>DotEnvSerializer</c>.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenKeyRepeated_ShouldReturnLastValue()
    {
        using DotEnvDocument document = DotEnvDocument.Parse("MULTI1=foo\nMULTI1=bar\n"u8);

        Assert.AreEqual("bar", document.RootElement.GetProperty("MULTI1").GetString());
    }
}
