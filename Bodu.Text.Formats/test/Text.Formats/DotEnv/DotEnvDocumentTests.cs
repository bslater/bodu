// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvDocumentTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.DotEnv;

/// <summary>
/// Unit tests for <see cref="DotEnvDocument" />.
/// </summary>
[TestClass]
public sealed class DotEnvDocumentTests
{

    // ------------------------------------------------- Indexer ---------------------------------------------------

    /// <summary>
    /// Verifies that the indexer returns the value for a key that is present in the document.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsPresent_ShouldReturnValue()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=hello");

        Assert.AreEqual("hello", doc["KEY"]);
    }

    /// <summary>
    /// Verifies that the indexer returns <see langword="null" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsAbsent_ShouldReturnNull()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=hello");

        Assert.IsNull(doc["MISSING"]);
    }

    /// <summary>
    /// Verifies that the indexer returns <see langword="null" /> when the supplied key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldReturnNull()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=hello");

        Assert.IsNull(doc[null!]);
    }

    // -------------------------------------------- TryGetValue(string) -------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.TryGetValue(string, out string)" /> returns
    /// <see langword="true" /> and the value for an existing key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsPresent_ShouldReturnTrueAndValue()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=world");

        bool result = doc.TryGetValue("KEY", out string? value);

        Assert.IsTrue(result);
        Assert.AreEqual("world", value);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.TryGetValue(string, out string)" /> returns
    /// <see langword="false" /> for an absent key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsAbsent_ShouldReturnFalse()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=world");

        bool result = doc.TryGetValue("MISSING", out string? value);

        Assert.IsFalse(result);
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.TryGetValue(string, out string)" /> returns
    /// <see langword="false" /> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsNull_ShouldReturnFalse()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=world");

        bool result = doc.TryGetValue(null!, out string? value);

        Assert.IsFalse(result);
        Assert.IsNull(value);
    }

    // -------------------------------------------- GetValue<T> ---------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.GetValue{T}(string)" /> throws <see cref="ArgumentNullException" />
    /// when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        DotEnvDocument doc = DotEnv.Parse("PORT=1234");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = doc.GetValue<int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.GetValue{T}(string)" /> throws <see cref="KeyNotFoundException" />
    /// when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyIsAbsent_ShouldThrowKeyNotFoundException()
    {
        DotEnvDocument doc = DotEnv.Parse("PORT=1234");

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = doc.GetValue<int>("MISSING");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.GetValue{T}(string)" /> returns the parsed value when the key is
    /// present and the value is parseable.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyIsPresentAndValueIsParseable_ShouldReturnValue()
    {
        DotEnvDocument doc = DotEnv.Parse("PORT=4321");

        int port = doc.GetValue<int>("PORT");

        Assert.AreEqual(4321, port);
    }

    // -------------------------------------------- TryGetValue<T> ------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.TryGetValue{T}(string, out T)" /> returns
    /// <see langword="false" /> and the default value when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetValueT_WhenKeyIsNull_ShouldReturnFalse()
    {
        DotEnvDocument doc = DotEnv.Parse("PORT=1234");

        bool result = doc.TryGetValue<int>(null!, out int value);

        Assert.IsFalse(result);
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.TryGetValue{T}(string, out T)" /> returns
    /// <see langword="false" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void TryGetValueT_WhenKeyIsAbsent_ShouldReturnFalse()
    {
        DotEnvDocument doc = DotEnv.Parse("PORT=1234");

        bool result = doc.TryGetValue<int>("MISSING", out int _);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.TryGetValue{T}(string, out T)" /> returns
    /// <see langword="true" /> and the parsed value when the key is present and the value is parseable.
    /// </summary>
    [TestMethod]
    public void TryGetValueT_WhenKeyIsPresentAndValueIsParseable_ShouldReturnTrueAndValue()
    {
        DotEnvDocument doc = DotEnv.Parse("PORT=5678");

        bool result = doc.TryGetValue<int>("PORT", out int port);

        Assert.IsTrue(result);
        Assert.AreEqual(5678, port);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.TryGetValue{T}(string, out T)" /> returns
    /// <see langword="false" /> when the value cannot be parsed as the requested type.
    /// </summary>
    [TestMethod]
    public void TryGetValueT_WhenValueIsNotParseable_ShouldReturnFalse()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=notanumber");

        bool result = doc.TryGetValue<int>("KEY", out int _);

        Assert.IsFalse(result);
    }

    // ----------------------------------------------- Entries ----------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.Entries" /> is empty for an empty source.
    /// </summary>
    [TestMethod]
    public void Entries_WhenSourceIsEmpty_ShouldBeEmpty()
    {
        DotEnvDocument doc = DotEnv.Parse(string.Empty);

        Assert.AreEqual(0, doc.Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvDocument.Entries" /> preserves source order.
    /// </summary>
    [TestMethod]
    public void Entries_ShouldPreserveSourceOrder()
    {
        DotEnvDocument doc = DotEnv.Parse("A=1\nB=2\nC=3");

        string[] keys = doc.Entries.Select(e => e.Key).ToArray();

        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, keys);
    }

}
