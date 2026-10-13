// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBindingTests.TryGetStringKeyedDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatBindingTests
{
    /// <summary>
    /// Verifies that a dictionary keyed by <see cref="string" />, declared as a class, a sorted list or the dictionary
    /// interface, is found with its value type.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="valueType">The value type expected.</param>
    [TestMethod]
    [DataRow(typeof(Dictionary<string, int>), typeof(int))]
    [DataRow(typeof(SortedList<string, double>), typeof(double))]
    [DataRow(typeof(IDictionary<string, string>), typeof(string))]
    public void TryGetStringKeyedDictionary_WhenTheTypeIsAStringKeyedDictionary_ShouldReturnItsValueType(Type type, Type valueType)
    {
        bool found = FlatBinding.TryGetStringKeyedDictionary(type, out Type? actual);

        Assert.IsTrue(found);
        Assert.AreEqual(valueType, actual);
    }

    /// <summary>
    /// Verifies that a dictionary keyed by another type, a read-only dictionary interface and a non-dictionary type are
    /// not found.
    /// </summary>
    /// <param name="type">The type to test.</param>
    [TestMethod]
    [DataRow(typeof(Dictionary<int, string>))]
    [DataRow(typeof(IReadOnlyDictionary<string, int>))]
    [DataRow(typeof(string))]
    public void TryGetStringKeyedDictionary_WhenTheTypeIsNotAStringKeyedDictionary_ShouldReturnFalse(Type type)
    {
        bool found = FlatBinding.TryGetStringKeyedDictionary(type, out Type? actual);

        Assert.IsFalse(found);
        Assert.IsNull(actual);
    }
}
