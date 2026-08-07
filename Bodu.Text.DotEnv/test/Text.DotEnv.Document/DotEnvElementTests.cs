// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvElementTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Document;

/// <summary>
/// Verifies <see cref="DotEnvElement" />, the read-only view over a parsed document.
/// </summary>
/// <remarks>
/// A DotEnv document is exactly two levels deep - a root object of string values - so every element is either the
/// root or a value, and each member is valid on only one of them. Asking a value for its properties, or the root for
/// its string, is a programming error rather than a miss, and these tests pin that split as well as the successful
/// paths.
/// </remarks>
[TestClass]
public class DotEnvElementTests
{
    /// <summary>The document text shared by these tests.</summary>
    private const string Source = "HOST=localhost\nPORT=8080\n";

    /// <summary>
    /// Verifies that the root reports itself as an object and each entry as a string, so a caller can dispatch on
    /// the kind rather than on position.
    /// </summary>
    [TestMethod]
    public void ValueKind_ShouldDistinguishTheRootFromItsValues()
    {
        using DotEnvDocument document = DotEnvDocument.Parse(Source);

        Assert.AreEqual(DotEnvValueKind.Object, document.RootElement.ValueKind);
        Assert.AreEqual(DotEnvValueKind.String, document.RootElement.GetProperty("HOST").ValueKind);
    }

    /// <summary>
    /// Verifies that a named entry resolves to its value.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenNamePresent_ShouldReturnTheValue()
    {
        using DotEnvDocument document = DotEnvDocument.Parse(Source);

        Assert.AreEqual("localhost", document.RootElement.GetProperty("HOST").GetString());
        Assert.AreEqual("8080", document.RootElement.GetProperty("PORT").GetString());
    }

    /// <summary>
    /// Verifies that an absent name throws rather than returning a default element, which would read as an entry
    /// whose value is empty.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenNameIsAbsent_ShouldThrowKeyNotFound()
    {
        using DotEnvDocument document = DotEnvDocument.Parse(Source);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() => _ = document.RootElement.GetProperty("MISSING"));
    }

    /// <summary>
    /// Verifies that the try-pattern reports presence without throwing, and rejects a null name as a programming
    /// error rather than treating it as a miss.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_ShouldReportPresence()
    {
        using DotEnvDocument document = DotEnvDocument.Parse(Source);

        Assert.IsTrue(document.RootElement.TryGetProperty("HOST", out DotEnvElement value));
        Assert.AreEqual("localhost", value.GetString());

        Assert.IsFalse(document.RootElement.TryGetProperty("MISSING", out _));

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = document.RootElement.TryGetProperty(null!, out _));
    }

    /// <summary>
    /// Verifies that asking a value for its properties is rejected: only the root is an object, and returning an
    /// empty result would hide the mistake.
    /// </summary>
    [TestMethod]
    public void PropertyAccess_WhenElementIsAValue_ShouldThrowInvalidOperationException()
    {
        using DotEnvDocument document = DotEnvDocument.Parse(Source);
        DotEnvElement value = document.RootElement.GetProperty("HOST");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = value.TryGetProperty("HOST", out _));
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = value.EnumerateObject());
    }

    /// <summary>
    /// Verifies that asking the root for its string is rejected, since the root is an object rather than a value.
    /// </summary>
    [TestMethod]
    public void GetString_WhenElementIsTheRoot_ShouldThrowInvalidOperationException()
    {
        using DotEnvDocument document = DotEnvDocument.Parse(Source);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = document.RootElement.GetString());
    }

    /// <summary>
    /// Verifies that enumerating the root yields every entry in source order, with the names and values the document
    /// carries.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_ShouldYieldEveryEntryInSourceOrder()
    {
        using DotEnvDocument document = DotEnvDocument.Parse(Source);

        var seen = new List<string>();
        foreach (DotEnvProperty property in document.RootElement.EnumerateObject())
            seen.Add($"{property.Name}={property.Value.GetString()}");

        CollectionAssert.AreEqual(new[] { "HOST=localhost", "PORT=8080" }, seen);
    }

    /// <summary>
    /// Verifies that an empty document enumerates to nothing rather than faulting.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenDocumentIsEmpty_ShouldYieldNothing()
    {
        using DotEnvDocument document = DotEnvDocument.Parse(string.Empty);

        Assert.AreEqual(DotEnvValueKind.Object, document.RootElement.ValueKind);
        Assert.IsFalse(document.RootElement.EnumerateObject().GetEnumerator().MoveNext());
    }
}
