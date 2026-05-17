// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.Enumerator.StructureOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentCircularBufferTests
{

    /// <summary>
    /// Verifies that the ConcurrentCircularBuffer enumerator is defined as a value type (struct).
    /// </summary>
    [TestMethod]
    public void StructureOf_ConcurrentCircularBufferEnumerator_ShouldBeStructType()
    {
        Type enumeratorType = typeof(ConcurrentCircularBuffer<TestItem>.Enumerator);
        Assert.IsTrue(enumeratorType.IsValueType, "Enumerator must be a value type (struct).");
    }

    /// <summary>
    /// Verifies that all public instance properties of the ConcurrentCircularBuffer enumerator are
    /// immutable, meaning none expose a public setter.
    /// </summary>
    [TestMethod]
    public void StructureOf_ConcurrentCircularBufferEnumerator_ShouldExposeOnlyImmutablePublicProperties()
    {
        Type enumeratorType = typeof(ConcurrentCircularBuffer<TestItem>.Enumerator);

        var mutableProperties = enumeratorType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
            .ToList();

        Assert.IsEmpty(mutableProperties,
            $"Enumerator exposes mutable public properties: {string.Join(", ", mutableProperties.Select(p => p.Name))}");
    }

}
