// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArrayTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlArray.Clear" /> detaches every node it removes.
/// </summary>
public partial class YamlArrayTests
{
    /// <summary>
    /// Verifies that clearing a sequence detaches every element, so each can be added to another container.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCleared_ShouldFreeEveryElementToBeAddedElsewhere()
    {
        YamlValue first = YamlValue.Create("a");
        YamlValue second = YamlValue.Create("b");
        var array = new YamlArray { first, null, second };

        array.Clear();

        var other = new YamlArray { first, second };
        Assert.AreEqual(2, other.Count);
    }
}
