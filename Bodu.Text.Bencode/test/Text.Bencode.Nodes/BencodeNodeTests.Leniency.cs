// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.Leniency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeNode.Parse(ReadOnlySpan{byte}, BencodeNodeOptions, BencodeDocumentOptions)" />
/// overload that applies dictionary-key leniency while building a node tree, including last-wins collapse of
/// duplicated keys into the dictionary-backed <see cref="BencodeObject" />.
/// </summary>
public partial class BencodeNodeTests
{
    /// <summary>
    /// Verifies that the strict overloads reject unsorted keys and that the document-options overload accepts them.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeysUnsorted_ShouldRequireAllowUnsortedKeys()
    {
        byte[] data = "d1:bi1e1:ai2ee"u8.ToArray();

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeNode.Parse(data);
        });

        var node = BencodeNode.Parse(data, default, new BencodeDocumentOptions { AllowUnsortedKeys = true });

        Assert.IsNotNull(node);
        Assert.AreEqual(1L, node["b"]!.GetValue<long>());
        Assert.AreEqual(2L, node["a"]!.GetValue<long>());
    }

    /// <summary>
    /// Verifies that duplicated keys collapse into the object with the last occurrence winning, the natural result
    /// of the dictionary-backed node model.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeysAllowed_ShouldCollapseLastWins()
    {
        byte[] data = "d1:ai1e1:ai2ee"u8.ToArray();

        var node = BencodeNode.Parse(data, default, new BencodeDocumentOptions { AllowDuplicateKeys = true });

        Assert.IsNotNull(node);
        Assert.AreEqual(1, node.AsObject().Count);
        Assert.AreEqual(2L, node["a"]!.GetValue<long>());
    }
}
