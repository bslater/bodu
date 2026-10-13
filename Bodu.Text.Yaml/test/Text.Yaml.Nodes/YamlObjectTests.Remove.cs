// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlObjectTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlObject.Remove(string)" /> detaches the value it removes.
/// </summary>
public sealed partial class YamlObjectTests
{
    /// <summary>
    /// Verifies that removing an entry detaches its value, so the value can be added to another container.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyRemoved_ShouldFreeTheValueToBeAddedElsewhere()
    {
        YamlValue value = YamlValue.Create("Ada");
        var sut = new YamlObject { ["name"] = value };

        Assert.IsTrue(sut.Remove("name"), "The result of removing the entry.");

        var other = new YamlObject { ["name"] = value };
        Assert.AreSame(value, other["name"], "The value once added elsewhere.");
    }
}
