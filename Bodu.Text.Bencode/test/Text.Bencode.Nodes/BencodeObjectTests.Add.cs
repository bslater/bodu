// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeObjectTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies <see cref="BencodeObject.Add(string, BencodeNode?)" />, including an add that is rejected.
/// </summary>
public partial class BencodeObjectTests
{
    /// <summary>
    /// Verifies that an <see cref="BencodeObject.Add(string, BencodeNode?)" /> rejected because the key already exists
    /// leaves the value without a parent, free to be added elsewhere.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldLeaveValueWithoutParent()
    {
        var obj = new BencodeObject { { "k", BencodeValue.Create(1L) } };
        var rejected = BencodeValue.Create(2L);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            obj.Add("k", rejected);
        });

        Assert.IsNull(rejected.Parent);
    }

    /// <summary>
    /// Verifies that adding a value under the key that already holds it is rejected without detaching the value from
    /// that entry.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyHoldsTheValue_ShouldKeepItsParent()
    {
        var value = BencodeValue.Create(1L);
        var obj = new BencodeObject { { "k", value } };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            obj.Add("k", value);
        });

        Assert.AreSame(obj, value.Parent);
    }
}
