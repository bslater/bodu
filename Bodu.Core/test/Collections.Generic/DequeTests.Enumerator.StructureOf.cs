// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DequeTests.Enumerator.StructureOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Collections.Generic;

public partial class DequeTests
{

    /// <summary>
    /// Verifies that the <see cref="Deque{T}.Enumerator"/> is defined as a value type (struct).
    /// </summary>
    [TestMethod]
    public void StructureOf_DequeEnumerator_ShouldBeStructType()
    {
        Type enumeratorType = typeof(Deque<int>.Enumerator);
        Assert.IsTrue(enumeratorType.IsValueType, "Enumerator must be a value type (struct).");
    }

    /// <summary>
    /// Verifies that all public properties of the <see cref="Deque{T}.Enumerator"/> are immutable (no public setters).
    /// </summary>
    [TestMethod]
    public void StructureOf_DequeEnumerator_ShouldExposeOnlyImmutablePublicProperties()
    {
        Type enumeratorType = typeof(Deque<int>.Enumerator);

        var mutableProperties = enumeratorType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
            .ToList();

        Assert.IsEmpty(mutableProperties,
            $"Enumerator exposes mutable public properties: {string.Join(", ", mutableProperties.Select(p => p.Name))}");
    }

}
