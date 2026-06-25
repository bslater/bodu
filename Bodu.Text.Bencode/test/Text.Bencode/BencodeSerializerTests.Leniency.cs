// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Leniency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the opt-in dictionary-key leniency of <see cref="BencodeSerializer" /> deserialization:
/// <see cref="BencodeSerializerOptions.AllowUnsortedKeys" /> and
/// <see cref="BencodeSerializerOptions.AllowDuplicateKeys" />, including last-wins member binding and the strictness
/// of writing regardless of the read options.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that the strict default rejects a document with unsorted keys and that
    /// <see cref="BencodeSerializerOptions.AllowUnsortedKeys" /> binds the same document.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeysUnsorted_ShouldRequireAllowUnsortedKeys()
    {
        byte[] data = Encoding.Latin1.GetBytes("d5:Label1:x2:Idi7ee");

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = BencodeSerializer.Deserialize<LenientModel>(data);
        });

        LenientModel model = BencodeSerializer.Deserialize<LenientModel>(
            data,
            new BencodeSerializerOptions { AllowUnsortedKeys = true });

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializerOptions.AllowDuplicateKeys" /> binds a repeated key last-wins
    /// instead of rejecting the document.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDuplicateKeysAllowed_ShouldBindLastOccurrence()
    {
        byte[] data = Encoding.Latin1.GetBytes("d2:Idi1e2:Idi2ee");

        LenientModel model = BencodeSerializer.Deserialize<LenientModel>(
            data,
            new BencodeSerializerOptions { AllowDuplicateKeys = true });

        Assert.AreEqual(2, model.Id);
    }

    /// <summary>
    /// Verifies that a dictionary value binds duplicated keys last-wins under
    /// <see cref="BencodeSerializerOptions.AllowDuplicateKeys" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDuplicateKeysAllowedForDictionary_ShouldBindLastOccurrence()
    {
        byte[] data = Encoding.Latin1.GetBytes("d1:ai1e1:ai2ee");

        Dictionary<string, long> map = BencodeSerializer.Deserialize<Dictionary<string, long>>(
            data,
            new BencodeSerializerOptions { AllowDuplicateKeys = true });

        Assert.HasCount(1, map);
        Assert.AreEqual(2L, map["a"]);
    }

    /// <summary>
    /// Verifies that the leniency options affect reading only: output is still written in canonical sorted key
    /// order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenLeniencyOptionsEnabled_ShouldStillWriteCanonicalOrder()
    {
        var options = new BencodeSerializerOptions { AllowUnsortedKeys = true, AllowDuplicateKeys = true };

        byte[] bytes = BencodeSerializer.Serialize(new LenientModel { Id = 7, Label = "x" }, options);

        Assert.AreEqual("d2:Idi7e5:Label1:xe", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// A small model used by the leniency tests.
    /// </summary>
    private sealed class LenientModel
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }

        /// <summary>Gets or sets the label.</summary>
        /// <value>The label.</value>
        public string Label { get; set; } = string.Empty;
    }
}
