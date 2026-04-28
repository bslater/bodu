// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayDequeTests.Enumerator.StructureOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Collections.Generic;

public partial class ArrayDequeTests
{
    /// <summary>
    /// Verifies that the <see cref="ArrayDeque{T}.Enumerator"/> is defined as a value type (struct).
    /// </summary>
    [TestMethod]
    [TestCategory("Structural")]
    public void StructureOf_ArrayDequeEnumerator_ShouldBeStructType()
    {
        var enumeratorType = typeof(ArrayDeque<int>.Enumerator);
        Assert.IsTrue(enumeratorType.IsValueType, "Enumerator must be a value type (struct).");
    }

    /// <summary>
    /// Verifies that all public properties of the <see cref="ArrayDeque{T}.Enumerator"/> are immutable (no public setters).
    /// </summary>
    [TestMethod]
    [TestCategory("Structural")]
    public void StructureOf_ArrayDequeEnumerator_ShouldExposeOnlyImmutablePublicProperties()
    {
        var enumeratorType = typeof(ArrayDeque<int>.Enumerator);

        var mutableProperties = enumeratorType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
            .ToList();

        Assert.AreEqual(0, mutableProperties.Count,
            $"Enumerator exposes mutable public properties: {string.Join(", ", mutableProperties.Select(p => p.Name))}");
    }
}
