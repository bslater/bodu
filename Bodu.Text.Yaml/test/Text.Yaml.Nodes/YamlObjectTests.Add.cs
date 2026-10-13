// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlObjectTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlObject.Add(string, YamlNode)" />, including the rule that a node appears at most once in a
/// tree.
/// </summary>
public sealed partial class YamlObjectTests
{
    /// <summary>
    /// Verifies that adding a value that already belongs to another container throws
    /// <see cref="InvalidOperationException" /> with the resource message, and leaves the mapping unchanged.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueBelongsToAnotherContainer_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create("Ada");
        var owner = new YamlArray { value };
        var sut = new YamlObject();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Add("name", value);
        });

        Assert.AreEqual(
            YamlResourceStrings.ResourceManager.GetString("Op_Invalid_YamlNodeAlreadyHasParent", YamlResourceStrings.Culture),
            ex.Message);
        Assert.AreEqual(0, sut.Count, "The mapping's count after the rejected add.");
        Assert.IsFalse(sut.ContainsKey("name"), "Whether the rejected key was added.");
    }

    /// <summary>
    /// Verifies that adding a value that the mapping already holds under another key throws
    /// <see cref="InvalidOperationException" /> rather than holding the node twice.
    /// </summary>
    [TestMethod]
    public void Add_WhenValueIsHeldUnderAnotherKey_ShouldThrowInvalidOperationException()
    {
        YamlValue value = YamlValue.Create("Ada");
        var sut = new YamlObject();
        sut.Add("name", value);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Add("alias", value);
        });

        CollectionAssert.AreEqual(new[] { "name" }, sut.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that an add rejected because the key already exists throws <see cref="ArgumentException" /> for the
    /// key and leaves the value unattached, free to be added elsewhere.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldLeaveValueFreeToAddElsewhere()
    {
        var sut = new YamlObject();
        sut.Add("name", YamlValue.Create("Ada"));
        YamlValue rejected = YamlValue.Create("Grace");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Add("name", rejected);
        });

        Assert.AreEqual("key", ex.ParamName, "The parameter the duplicate key is reported against.");
        var other = new YamlArray { rejected };
        Assert.AreSame(rejected, other[0], "The rejected value once added elsewhere.");
    }
}
