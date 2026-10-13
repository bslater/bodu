// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArrayTests.RemoveAt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlArray.RemoveAt(int)" /> detaches the node it removes.
/// </summary>
public partial class YamlArrayTests
{
    /// <summary>
    /// Verifies that removing an element by index detaches it, so it can be added to another container.
    /// </summary>
    [TestMethod]
    public void RemoveAt_WhenElementRemoved_ShouldFreeItToBeAddedElsewhere()
    {
        YamlValue value = YamlValue.Create("b");
        var array = new YamlArray { YamlValue.Create("a"), value };

        array.RemoveAt(1);

        var other = new YamlArray { value };
        Assert.AreSame(value, other[0]);
    }
}
