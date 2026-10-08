// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlObjectTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Verifies <see cref="TomlObject.Add(string, TomlNode?)" />, including an add that is rejected.
/// </summary>
public partial class TomlObjectTests
{
    /// <summary>
    /// Verifies that an <see cref="TomlObject.Add(string, TomlNode?)" /> rejected because the key already exists leaves
    /// the value without a parent, free to be added elsewhere.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldLeaveValueWithoutParent()
    {
        var obj = new TomlObject { { "k", TomlValue.Create(1L) } };
        var rejected = TomlValue.Create(2L);

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
        var value = TomlValue.Create(1L);
        var obj = new TomlObject { { "k", value } };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            obj.Add("k", value);
        });

        Assert.AreSame(obj, value.Parent);
    }
}
