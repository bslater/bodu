// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.DotEnv.Document;

namespace Bodu.Text.DotEnv.Document;

/// <summary>
/// Contains tests for the read-only <see cref="DotEnvDocument" /> / <see cref="DotEnvElement" /> DOM.
/// </summary>
[TestClass]
public partial class DotEnvDocumentTests
{
    /// <summary>
    /// Verifies that the root element is an object whose properties enumerate in source order.
    /// </summary>
    [TestMethod]
    public void RootElement_WhenEnumerated_ShouldYieldPropertiesInOrder()
    {
        using DotEnvDocument document = DotEnvDocument.Parse("A=1\nB=two\n"u8);

        var pairs = new List<(string, string)>();
        foreach (DotEnvProperty property in document.RootElement.EnumerateObject())
            pairs.Add((property.Name, property.Value.GetString()));

        CollectionAssert.AreEqual(new List<(string, string)> { ("A", "1"), ("B", "two") }, pairs);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvElement.GetProperty(string)" /> returns the string value for a present key.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenKeyPresent_ShouldReturnStringValue()
    {
        using DotEnvDocument document = DotEnvDocument.Parse("HOST=localhost\n"u8);

        Assert.AreEqual("localhost", document.RootElement.GetProperty("HOST").GetString());
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvElement.TryGetProperty(string, out DotEnvElement)" /> returns
    /// <see langword="false" /> for an absent key.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenKeyAbsent_ShouldReturnFalse()
    {
        using DotEnvDocument document = DotEnvDocument.Parse("A=1\n"u8);

        Assert.IsFalse(document.RootElement.TryGetProperty("MISSING", out _));
    }

    /// <summary>
    /// Verifies that accessing the root element after disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void RootElement_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        DotEnvDocument document = DotEnvDocument.Parse("A=1\n"u8);
        document.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = document.RootElement;
        });
    }
}
