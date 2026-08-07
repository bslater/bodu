// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedElementTests.Enumeration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Verifies the enumeration surface of <see cref="DelimitedElement" />, including the explicit
/// <see cref="IEnumerable" /> and <see cref="IEnumerator" /> implementations its struct enumerators expose.
/// </summary>
/// <remarks>
/// The explicit interface members exist so the enumerators work in LINQ and non-generic contexts. Nothing in the
/// serializer path reaches them, so they are only covered when a test uses the enumerator as an interface rather
/// than through <c>foreach</c>.
/// </remarks>
[TestClass]
public sealed partial class DelimitedElementTests
{
    /// <summary>
    /// Verifies that <see cref="DelimitedElement.EnumerateArray" /> yields every record of the root array.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenRootIsArray_ShouldYieldEveryRecord()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("name\nAda\nGrace\n"u8);

        var names = new List<string>();
        foreach (DelimitedElement record in document.RootElement.EnumerateArray())
        {
            names.Add(record.GetProperty("name").GetString());
        }

        CollectionAssert.AreEqual(new[] { "Ada", "Grace" }, names);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedElement.EnumerateArray" /> throws when the element is not an array.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenElementIsNotArray_ShouldThrowExactly()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("name\nAda\n"u8);
        DelimitedElement record = document.RootElement[0];

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = record.EnumerateArray();
        });
    }

    /// <summary>
    /// Verifies that the array enumerator can be rewound with <c>Reset</c> and replayed through the non-generic
    /// <see cref="IEnumerator" /> surface.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenResetThroughInterface_ShouldReplayFromStart()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("name\nAda\nGrace\n"u8);

        DelimitedElement.ArrayEnumerator enumerator = document.RootElement.EnumerateArray();
        IEnumerator sequence = enumerator;

        Assert.IsTrue(sequence.MoveNext());
        string first = ((DelimitedElement)sequence.Current!).GetProperty("name").GetString();

        sequence.Reset();

        Assert.IsTrue(sequence.MoveNext());
        string afterReset = ((DelimitedElement)sequence.Current!).GetProperty("name").GetString();

        Assert.AreEqual("Ada", first);
        Assert.AreEqual(first, afterReset);
    }

    /// <summary>
    /// Verifies that the array enumerator is reachable through the generic and non-generic
    /// <see cref="IEnumerable" /> surfaces, which is what lets it be consumed by LINQ.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenUsedAsEnumerable_ShouldYieldEveryRecord()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("name\nAda\nGrace\n"u8);

        IEnumerable<DelimitedElement> generic = document.RootElement.EnumerateArray();
        Assert.AreEqual(2, generic.Count());

        IEnumerable nonGeneric = document.RootElement.EnumerateArray();
        var count = 0;
        foreach (object? _ in nonGeneric)
        {
            count++;
        }

        Assert.AreEqual(2, count);
    }

    /// <summary>
    /// Verifies that the object enumerator is reachable through the generic and non-generic
    /// <see cref="IEnumerable" /> surfaces and can be rewound.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenUsedAsEnumerableAndReset_ShouldReplayEveryField()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("a,b\n1,2\n"u8);
        DelimitedElement record = document.RootElement[0];

        IEnumerable<DelimitedProperty> generic = record.EnumerateObject();
        Assert.AreEqual(2, generic.Count());

        IEnumerable nonGeneric = record.EnumerateObject();
        var count = 0;
        foreach (object? _ in nonGeneric)
        {
            count++;
        }

        Assert.AreEqual(2, count);

        IEnumerator sequence = record.EnumerateObject();
        Assert.IsTrue(sequence.MoveNext());
        sequence.Reset();
        Assert.IsTrue(sequence.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedElement.EnumerateObject" /> throws when the element is not an object.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenElementIsNotObject_ShouldThrowExactly()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("name\nAda\n"u8);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = document.RootElement.EnumerateObject();
        });
    }
}
