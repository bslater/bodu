// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.ItemEvicting.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that calling Add from an ItemEvicting handler throws InvalidOperationException to prevent
    /// re-entrant mutation from corrupting internal state mid-eviction.
    /// </summary>
    [TestMethod]
    public void Add_WhenCalledFromItemEvictingHandler_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        Exception? captured = null;
        dictionary.ItemEvicting += (_, _) =>
        {
            try { dictionary.Add("Z", 99); }
            catch (Exception ex) { captured = ex; }
        };

        dictionary.Add("C", 3); // triggers eviction → handler attempts re-entrant Add.

        Assert.IsInstanceOfType<InvalidOperationException>(captured);
    }

    /// <summary>
    /// Verifies that calling <see cref="EvictingDictionary{TKey, TValue}.Clear" /> from an ItemEvicted handler throws InvalidOperationException.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCalledFromItemEvictedHandler_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        Exception? captured = null;
        dictionary.ItemEvicted += (_, _) =>
        {
            try { dictionary.Clear(); }
            catch (Exception ex) { captured = ex; }
        };

        dictionary.Add("C", 3);

        Assert.IsInstanceOfType<InvalidOperationException>(captured);
    }
    /// <summary>
    /// Verifies that the ItemEvicting event is not triggered when the capacity is not exceeded.
    /// </summary>
    [TestMethod]
    public void ItemEvicting_WhenCapacityNotExceeded_ShouldNotTriggerEvent()
    {
        var anyEventFired = false;
        var dictionary = new EvictingDictionary<string, int>(3);

        dictionary.ItemEvicting += (_, _) => anyEventFired = true;
        dictionary.ItemEvicted += (_, _) => anyEventFired = true;

        dictionary.Add("one", 1);
        dictionary.Add("two", 2);

        Assert.IsFalse(anyEventFired);
    }

    /// <summary>
    /// Verifies that the ItemEvicting event is not triggered by Remove or Clear operations.
    /// </summary>
    [TestMethod]
    public void ItemEvicting_WhenUsingRemoveOrClear_ShouldNotTriggerEvent()
    {
        var eventFired = false;
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        dictionary.ItemEvicted += (_, _) => eventFired = true;
        dictionary.ItemEvicting += (_, _) => eventFired = true;

        dictionary.Remove("A");
        Assert.IsFalse(eventFired);

        dictionary.Add("B", 2);
        dictionary.Add("C", 3);
        dictionary.Clear();
        Assert.IsFalse(eventFired);
    }

    /// <summary>
    /// Verifies that calling <see cref="EvictingDictionary{TKey, TValue}.Remove" /> from an ItemEvicting handler throws InvalidOperationException.
    /// </summary>
    [TestMethod]
    public void Remove_WhenCalledFromItemEvictingHandler_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(2);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        Exception? captured = null;
        dictionary.ItemEvicting += (_, _) =>
        {
            try { dictionary.Remove("B"); }
            catch (Exception ex) { captured = ex; }
        };

        dictionary.Add("C", 3);

        Assert.IsInstanceOfType<InvalidOperationException>(captured);
    }

}
