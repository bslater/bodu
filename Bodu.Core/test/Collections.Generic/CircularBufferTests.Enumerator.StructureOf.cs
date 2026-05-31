// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Enumerator.StructureOf.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Collections.Generic;

public partial class CircularBufferTests
{

    /// <summary>
    /// Verifies that the CircularBuffer enumerator is defined as a value type (struct).
    /// </summary>
    [TestMethod]
    public void StructureOf_CircularBufferEnumerator_ShouldBeStructType()
    {
        Type enumeratorType = typeof(CircularBuffer<int>.Enumerator);
        Assert.IsTrue(enumeratorType.IsValueType, "Enumerator must be a value type (struct).");
    }

    /// <summary>
    /// Verifies that all public properties of the CircularBuffer enumerator are immutable (no public setters).
    /// </summary>
    [TestMethod]
    public void StructureOf_CircularBufferEnumerator_ShouldExposeOnlyImmutablePublicProperties()
    {
        Type enumeratorType = typeof(CircularBuffer<int>.Enumerator);

        var mutableProperties = enumeratorType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic)
            .ToList();

        Assert.IsEmpty(mutableProperties,
            $"Enumerator exposes mutable public properties: {string.Join(", ", mutableProperties.Select(p => p.Name))}");
    }

}
