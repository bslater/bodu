// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObjectTests.DeepClone.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Contains the member tests for <see cref="IniNode.DeepClone" /> on an <see cref="IniObject" /> tree.
/// </summary>
public partial class IniObjectTests
{
    /// <summary>
    /// Verifies that <see cref="IniNode.DeepClone" /> produces an independent copy including trivia.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenMutatingClone_ShouldNotAffectOriginal()
    {
        IniObject root = IniNode.Parse("; note\n[s]\nk=1\n"u8);
        var clone = (IniObject)root.DeepClone();

        clone["s"].AsObject()["k"] = new IniValue("2");
        clone["s"].AsObject().LeadingComments.Add(" extra");

        Assert.AreEqual("1", root["s"].AsObject()["k"].AsValue().Value);
        Assert.AreEqual("2", clone["s"].AsObject()["k"].AsValue().Value);
        CollectionAssert.AreEqual(new List<string> { " note" }, root["s"].AsObject().LeadingComments.ToList());
    }
}
