// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.GetNameBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies <see cref="BencodeProperty.GetNameBytes" />: that it returns a dictionary key's exact bytes, including keys
/// that are not valid UTF-8 and so cannot be named exactly by <see cref="BencodeProperty.Name" />.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that two keys that are not valid UTF-8, which <see cref="BencodeProperty.Name" /> reads alike, each
    /// return their own bytes.
    /// </summary>
    [TestMethod]
    public void GetNameBytes_WhenKeysAreNotValidUtf8_ShouldReturnEachKeysExactBytes()
    {
        using var document = BencodeDocument.Parse(Bytes("d1:þi1e1:ÿi2ee"));

        BencodeProperty[] properties = [.. document.RootElement.EnumerateObject()];

        Assert.AreEqual(properties[0].Name, properties[1].Name);
        CollectionAssert.AreEqual(new byte[] { 0xFE }, properties[0].GetNameBytes());
        CollectionAssert.AreEqual(new byte[] { 0xFF }, properties[1].GetNameBytes());
    }

    /// <summary>
    /// Verifies that the returned array is a copy, so changing it leaves the document's key unchanged.
    /// </summary>
    [TestMethod]
    public void GetNameBytes_WhenResultIsModified_ShouldLeaveTheKeyUnchanged()
    {
        using var document = BencodeDocument.Parse(Bytes("d3:keyi1ee"));
        BencodeProperty property = document.RootElement.EnumerateObject().First();

        byte[] first = property.GetNameBytes();
        first[0] = (byte)'X';

        CollectionAssert.AreEqual(Bytes("key"), property.GetNameBytes());
    }

    /// <summary>
    /// Verifies that reading a property's key bytes after its document is disposed throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GetNameBytes_WhenDocumentIsDisposed_ShouldThrowObjectDisposedException()
    {
        var document = BencodeDocument.Parse(Bytes("d3:keyi1ee"));
        BencodeProperty property = document.RootElement.EnumerateObject().First();
        document.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = property.GetNameBytes();
        });
    }

    /// <summary>
    /// Verifies that reading the key bytes of a default <see cref="BencodeProperty" />, which belongs to no document,
    /// throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetNameBytes_WhenPropertyIsDefault_ShouldThrowInvalidOperationException()
    {
        BencodeProperty property = default;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = property.GetNameBytes();
        });
    }
}
