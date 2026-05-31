// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDictionaryExtensions.AddOrUpdate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IDictionaryExtensionsTests_AddOrUpdate
{
    /// <summary>
    /// Verifies that <c>AddOrUpdate</c> inserts the value when the key is absent.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenKeyIsAbsent_ShouldInsertValue()
    {
        Dictionary<string, int> dictionary = new();

        dictionary.AddOrUpdate("a", 1);

        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that <c>AddOrUpdate</c> replaces the value when the key is already present.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenKeyIsPresent_ShouldReplaceValue()
    {
        Dictionary<string, int> dictionary = new() { ["a"] = 1 };

        dictionary.AddOrUpdate("a", 2);

        Assert.AreEqual(2, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that <c>AddOrUpdate</c> throws <see cref="ArgumentNullException" /> when the dictionary is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddOrUpdate_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        IDictionary<string, int> dictionary = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            dictionary.AddOrUpdate("a", 1);
        });
    }
}
