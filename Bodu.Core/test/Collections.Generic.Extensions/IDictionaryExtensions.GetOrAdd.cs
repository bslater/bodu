// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDictionaryExtensions.GetOrAdd.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IDictionaryExtensionsTests_GetOrAdd
{
    /// <summary>
    /// Verifies that the value overload adds and returns the value when the key is absent.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetOrAdd_WhenKeyIsAbsent_ShouldAddAndReturnValue()
    {
        Dictionary<string, int> dictionary = new();

        int result = dictionary.GetOrAdd("a", 1);

        Assert.AreEqual(1, result);
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the value overload returns the existing value without overwriting it.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyIsPresent_ShouldReturnExistingValueWithoutOverwriting()
    {
        Dictionary<string, int> dictionary = new() { ["a"] = 1 };

        int result = dictionary.GetOrAdd("a", 99);

        Assert.AreEqual(1, result);
        Assert.AreEqual(1, dictionary["a"]);
    }

    /// <summary>
    /// Verifies that the factory overload invokes the factory and adds its result when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyIsAbsent_ForFactoryOverload_ShouldInvokeFactoryAndAdd()
    {
        Dictionary<string, int> dictionary = new();

        int result = dictionary.GetOrAdd("key", k => k.Length);

        Assert.AreEqual(3, result);
        Assert.AreEqual(3, dictionary["key"]);
    }

    /// <summary>
    /// Verifies that the factory overload does not invoke the factory when the key is already present.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenKeyIsPresent_ForFactoryOverload_ShouldNotInvokeFactory()
    {
        Dictionary<string, int> dictionary = new() { ["a"] = 1 };
        bool invoked = false;

        int result = dictionary.GetOrAdd("a", _ =>
        {
            invoked = true;
            return 99;
        });

        Assert.AreEqual(1, result);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that the value overload throws <see cref="ArgumentNullException" /> when the dictionary is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        IDictionary<string, int> dictionary = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.GetOrAdd("a", 1);
        });
    }

    /// <summary>
    /// Verifies that the factory overload throws <see cref="ArgumentNullException" /> when the factory is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetOrAdd_WhenFactoryIsNull_ShouldThrowExactly()
    {
        Dictionary<string, int> dictionary = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = dictionary.GetOrAdd("a", (Func<string, int>)null!);
        });
    }
}
