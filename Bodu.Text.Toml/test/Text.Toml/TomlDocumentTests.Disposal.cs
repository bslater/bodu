// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentTests.Disposal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that every <see cref="TomlElement" /> accessor throws <see cref="ObjectDisposedException" /> once the
/// owning <see cref="TomlDocument" /> has been disposed, including access through an enumerator created before
/// disposal.
/// </summary>
public partial class TomlDocumentTests
{
    /// <summary>
    /// Verifies that each scalar accessor throws <see cref="ObjectDisposedException" /> after the owning document is
    /// disposed, sweeping all eight scalar kinds.
    /// </summary>
    /// <param name="key">The root member whose matching accessor is invoked.</param>
    [TestMethod]
    [DataRow("String")]
    [DataRow("Integer")]
    [DataRow("Float")]
    [DataRow("Boolean")]
    [DataRow("OffsetDateTime")]
    [DataRow("LocalDateTime")]
    [DataRow("LocalDate")]
    [DataRow("LocalTime")]
    public void ScalarAccessors_WhenDocumentDisposed_ShouldThrowObjectDisposedException(string key)
    {
        var document = TomlDocument.Parse(AllKindsToml);
        TomlElement element = document.RootElement.GetProperty(key);
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            Invoke(element, "Get" + MatchingAccessorSuffix(key));
        });
    }

    /// <summary>
    /// Maps a kind name from <see cref="AllKindsToml" /> to the suffix of its matching scalar accessor.
    /// </summary>
    /// <param name="key">The kind name.</param>
    /// <returns>The accessor-name suffix following the <c>Get</c> prefix.</returns>
    private static string MatchingAccessorSuffix(string key) =>
        key switch
        {
            "Integer" => "Int64",
            "Float" => "Double",
            "OffsetDateTime" => "DateTimeOffset",
            "LocalDateTime" => "DateTime",
            "LocalDate" => "DateOnly",
            "LocalTime" => "TimeOnly",
            _ => key,
        };

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetArrayLength" /> throws <see cref="ObjectDisposedException" /> after the
    /// owning document is disposed.
    /// </summary>
    [TestMethod]
    public void GetArrayLength_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = [1]\n");
        TomlElement element = document.RootElement.GetProperty("a");
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.GetArrayLength();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.EnumerateArray" /> throws <see cref="ObjectDisposedException" /> after the
    /// owning document is disposed.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = [1]\n");
        TomlElement element = document.RootElement.GetProperty("a");
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.EnumerateArray();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.EnumerateObject" /> throws <see cref="ObjectDisposedException" /> after
    /// the owning document is disposed.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = 1\n");
        TomlElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.EnumerateObject();
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.GetProperty(string)" /> throws <see cref="ObjectDisposedException" />
    /// after the owning document is disposed.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = 1\n");
        TomlElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.GetProperty("a");
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.TryGetProperty(string, out TomlElement)" /> throws
    /// <see cref="ObjectDisposedException" /> after the owning document is disposed.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = 1\n");
        TomlElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.TryGetProperty("a", out _);
        });
    }

    /// <summary>
    /// Verifies that the positional indexer throws <see cref="ObjectDisposedException" /> after the owning document is
    /// disposed.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = [1]\n");
        TomlElement element = document.RootElement.GetProperty("a");
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element[0];
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.ToString" /> throws <see cref="ObjectDisposedException" /> after the
    /// owning document is disposed, because it reads the element's kind.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = 1\n");
        TomlElement element = document.RootElement.GetProperty("a");
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.ToString();
        });
    }

    /// <summary>
    /// Verifies that an array enumerator created before disposal throws <see cref="ObjectDisposedException" /> when it
    /// is advanced after disposal, because element walking reads the document's row store.
    /// </summary>
    [TestMethod]
    public void MoveNext_WhenDocumentDisposedAfterArrayEnumeratorCreated_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = [1]\n");
        TomlElement.ArrayEnumerator enumerator = document.RootElement.GetProperty("a").EnumerateArray();
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that an object enumerator created before disposal throws <see cref="ObjectDisposedException" /> when
    /// it is advanced after disposal, because pair walking reads the document index.
    /// </summary>
    [TestMethod]
    public void MoveNext_WhenDocumentDisposedAfterObjectEnumeratorCreated_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("a = 1\n");
        TomlElement.ObjectEnumerator enumerator = document.RootElement.EnumerateObject();
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }
}
