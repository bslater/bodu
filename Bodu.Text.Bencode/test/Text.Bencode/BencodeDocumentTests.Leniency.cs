// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.Leniency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the opt-in dictionary-key leniency of <see cref="BencodeDocument" /> parsing:
/// <see cref="BencodeDocumentOptions.AllowUnsortedKeys" /> and
/// <see cref="BencodeDocumentOptions.AllowDuplicateKeys" />, including first-match name lookups and full enumeration
/// of duplicated keys.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that the strict default rejects unsorted keys, and that
    /// <see cref="BencodeDocumentOptions.AllowUnsortedKeys" /> accepts the same document with lookups resolving by
    /// raw key bytes regardless of stored order.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeysUnsorted_ShouldRequireAllowUnsortedKeys()
    {
        var data = Bytes("d1:bi1e1:ai2ee");

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            using var strict = BencodeDocument.Parse(data);
        });

        using var document = BencodeDocument.Parse(data, new BencodeDocumentOptions { AllowUnsortedKeys = true });

        Assert.AreEqual(1L, document.RootElement.GetProperty("b").GetInt64());
        Assert.AreEqual(2L, document.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>
    /// Verifies that with <see cref="BencodeDocumentOptions.AllowDuplicateKeys" /> a name lookup returns the first
    /// occurrence in document order while enumeration exposes every pair.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeysAllowed_ShouldLookUpFirstMatchAndEnumerateAll()
    {
        var data = Bytes("d1:ai1e1:ai2ee");
        using var document = BencodeDocument.Parse(data, new BencodeDocumentOptions { AllowDuplicateKeys = true });

        Assert.AreEqual(1L, document.RootElement.GetProperty("a").GetInt64());

        var pairCount = 0;
        foreach (BencodeProperty property in document.RootElement.EnumerateObject())
        {
            Assert.AreEqual("a", property.Name);
            pairCount++;
        }

        Assert.AreEqual(2, pairCount);
    }
}
