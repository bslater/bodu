// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.Disposal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that every <see cref="BencodeElement" /> accessor throws <see cref="ObjectDisposedException" /> once the
/// owning <see cref="BencodeDocument" /> has been disposed, including access through an enumerator that was created
/// before disposal.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetInt64" /> throws <see cref="ObjectDisposedException" /> after the
    /// owning document is disposed.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("i1e"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetString" /> throws <see cref="ObjectDisposedException" /> after the
    /// owning document is disposed.
    /// </summary>
    [TestMethod]
    public void GetString_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("4:spam"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.GetString();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetBytes" /> throws <see cref="ObjectDisposedException" /> after the
    /// owning document is disposed.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("4:spam"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.GetBytes();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetArrayLength" /> throws <see cref="ObjectDisposedException" /> after
    /// the owning document is disposed.
    /// </summary>
    [TestMethod]
    public void GetArrayLength_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("le"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.GetArrayLength();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.EnumerateArray" /> throws <see cref="ObjectDisposedException" /> after
    /// the owning document is disposed.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("le"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.EnumerateArray();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.EnumerateObject" /> throws <see cref="ObjectDisposedException" />
    /// after the owning document is disposed.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("de"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.EnumerateObject();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetProperty(string)" /> throws
    /// <see cref="ObjectDisposedException" /> after the owning document is disposed.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("d1:ai1ee"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.GetProperty("a");
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.TryGetProperty(string, out BencodeElement)" /> throws
    /// <see cref="ObjectDisposedException" /> after the owning document is disposed.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("d1:ai1ee"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.TryGetProperty("a", out _);
        });
    }

    /// <summary>
    /// Verifies that an element obtained from an array enumerator created before disposal throws
    /// <see cref="ObjectDisposedException" /> when its value is accessed after disposal.
    /// </summary>
    [TestMethod]
    public void Current_WhenDocumentDisposedAfterEnumeratorCreated_ShouldThrowOnElementAccess()
    {
        var document = BencodeDocument.Parse(Bytes("li1ee"));
        BencodeElement.ArrayEnumerator enumerator = document.RootElement.EnumerateArray();
        document.Dispose();

        Assert.IsTrue(enumerator.MoveNext());

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = enumerator.Current.ValueKind;
        });
    }
}
