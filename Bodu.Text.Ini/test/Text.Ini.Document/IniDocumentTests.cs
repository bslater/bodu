// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini.Document;
using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Document;

/// <summary>
/// Contains tests for the read-only <see cref="IniDocument" /> / <see cref="IniElement" /> DOM over the normalized
/// object-of-objects shape.
/// </summary>
[TestClass]
public class IniDocumentTests
{
    /// <summary>
    /// Verifies that the root element enumerates the hoisted global keys as strings followed by the sections as
    /// objects.
    /// </summary>
    [TestMethod]
    public void RootElement_WhenEnumerated_ShouldYieldGlobalsThenSections()
    {
        using IniDocument document = IniDocument.Parse("g=1\n[db]\nhost=x\n"u8);

        var properties = new List<(string Name, IniValueKind Kind)>();
        foreach (IniProperty property in document.RootElement.EnumerateObject())
            properties.Add((property.Name, property.Value.ValueKind));

        CollectionAssert.AreEqual(
            new List<(string, IniValueKind)> { ("g", IniValueKind.String), ("db", IniValueKind.Object) },
            properties);
    }

    /// <summary>
    /// Verifies that a section entry is reachable through nested <see cref="IniElement.GetProperty(string)" /> calls.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenNestedSectionKey_ShouldReturnValue()
    {
        using IniDocument document = IniDocument.Parse("[db]\nhost=localhost\n"u8);

        Assert.AreEqual("localhost", document.RootElement.GetProperty("db").GetProperty("host").GetString());
    }

    /// <summary>
    /// Verifies that a repeated section merges into one object by default, appending the later keys.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSections_ShouldMergeEntries()
    {
        using IniDocument document = IniDocument.Parse("[db]\nhost=x\n[db]\nport=5\n"u8);

        var keys = new List<string>();
        foreach (IniProperty property in document.RootElement.GetProperty("db").EnumerateObject())
            keys.Add(property.Name);

        CollectionAssert.AreEqual(new List<string> { "host", "port" }, keys);
    }

    /// <summary>
    /// Verifies that a repeated section name throws <see cref="IniFormatException" /> under
    /// <see cref="IniDuplicateSectionBehavior.Disallowed" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionDisallowed_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniDocument.Parse(
                "[db]\nhost=x\n[db]\nport=5\n"u8,
                IniReaderOptions.Default,
                new IniDocumentOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed });
        });
    }

    /// <summary>
    /// Verifies that a duplicate key keeps the last value by default, preserving the key's original position.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKey_ShouldKeepLastByDefault()
    {
        using IniDocument document = IniDocument.Parse("[s]\nk=1\nk=2\n"u8);

        Assert.AreEqual("2", document.RootElement.GetProperty("s").GetProperty("k").GetString());
    }

    /// <summary>
    /// Verifies that a global key colliding with a section of the same name throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGlobalKeyCollidesWithSection_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniDocument.Parse("db=x\n[db]\nhost=y\n"u8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IniElement.TryGetProperty(string, out IniElement)" /> returns
    /// <see langword="false" /> for an absent name.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenNameAbsent_ShouldReturnFalse()
    {
        using IniDocument document = IniDocument.Parse("[db]\nhost=x\n"u8);

        Assert.IsFalse(document.RootElement.TryGetProperty("missing", out _));
        Assert.IsFalse(document.RootElement.GetProperty("db").TryGetProperty("missing", out _));
    }

    /// <summary>
    /// Verifies that <see cref="IniElement.GetString" /> on an object element throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetString_WhenObjectElement_ShouldThrowInvalidOperationException()
    {
        using IniDocument document = IniDocument.Parse("[db]\nhost=x\n"u8);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = document.RootElement.GetString();
        });
    }

    /// <summary>
    /// Verifies that accessing the root element after disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void RootElement_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        IniDocument document = IniDocument.Parse("a=1\n"u8);
        document.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = document.RootElement;
        });
    }
}
