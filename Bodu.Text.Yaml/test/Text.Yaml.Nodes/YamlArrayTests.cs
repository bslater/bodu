// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlArrayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies <see cref="YamlArray" /> through its <see cref="IList{T}" /> surface and
/// <see cref="YamlValue" /> across every scalar kind.
/// </summary>
/// <remarks>
/// The mutable DOM is the editing surface: a caller loads a document, changes part of it, and writes it back. The
/// sequence node is declared as a list of nodes, but nothing exercised it as one - so positional editing, removal and
/// enumeration were all untested, as was writing a sequence back out after an edit.
/// </remarks>
[TestClass]
public class YamlArrayTests
{
    /// <summary>
    /// Builds a sequence of scalar strings.
    /// </summary>
    /// <param name="values">The scalar values.</param>
    /// <returns>The sequence node.</returns>
    private static YamlArray Sequence(params string[] values)
    {
        var array = new YamlArray();
        foreach (string value in values)
            array.Add(YamlValue.Create(value));

        return array;
    }

    /// <summary>
    /// Reads the scalar strings of a sequence.
    /// </summary>
    /// <param name="array">The sequence node.</param>
    /// <returns>The scalar values.</returns>
    private static List<string> Values(YamlArray array) =>
        [.. array.Select(node => ((YamlValue)node!).GetValue<string>())];

    /// <summary>
    /// Verifies that a sequence reports itself as mutable, so a caller dispatching on
    /// <see cref="ICollection{T}.IsReadOnly" /> does not skip the editing members.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_ShouldBeFalse()
    {
        Assert.IsFalse(new YamlArray().IsReadOnly);
    }

    /// <summary>
    /// Verifies that the indexer replaces an element in place, leaving the sequence length unchanged.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAssigned_ShouldReplaceInPlace()
    {
        YamlArray array = Sequence("a", "b", "c");

        array[1] = YamlValue.Create("B");

        CollectionAssert.AreEqual(new[] { "a", "B", "c" }, Values(array));
    }

    /// <summary>
    /// Verifies that <see cref="IList{T}.IndexOf(T)" /> and <see cref="ICollection{T}.Contains(T)" /> locate an
    /// element by reference, and report absence rather than throwing.
    /// </summary>
    [TestMethod]
    public void IndexOfAndContains_ShouldLocateByReference()
    {
        YamlValue first = YamlValue.Create("a");
        YamlValue second = YamlValue.Create("b");
        var array = new YamlArray { first, second };

        Assert.AreEqual(0, array.IndexOf(first));
        Assert.AreEqual(1, array.IndexOf(second));
        Assert.IsTrue(array.Contains(second));

        Assert.AreEqual(-1, array.IndexOf(YamlValue.Create("a")));
        Assert.IsFalse(array.Contains(YamlValue.Create("a")));
    }

    /// <summary>
    /// Verifies that <see cref="IList{T}.Insert(int, T)" /> places an element at the requested position and shifts
    /// the rest, including at both ends.
    /// </summary>
    [TestMethod]
    public void Insert_ShouldPlaceAtTheRequestedPosition()
    {
        YamlArray array = Sequence("b");

        array.Insert(0, YamlValue.Create("a"));
        array.Insert(2, YamlValue.Create("c"));

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Values(array));
    }

    /// <summary>
    /// Verifies that removal by reference removes only the matching element and reports whether one was found.
    /// </summary>
    [TestMethod]
    public void Remove_ShouldRemoveOnlyTheMatchingElement()
    {
        YamlValue target = YamlValue.Create("b");
        var array = new YamlArray { YamlValue.Create("a"), target, YamlValue.Create("c") };

        Assert.IsTrue(array.Remove(target));
        CollectionAssert.AreEqual(new[] { "a", "c" }, Values(array));

        Assert.IsFalse(array.Remove(YamlValue.Create("a")));
        Assert.AreEqual(2, array.Count);
    }

    /// <summary>
    /// Verifies that removal by index removes the element at that position.
    /// </summary>
    [TestMethod]
    public void RemoveAt_ShouldRemoveTheElementAtThatIndex()
    {
        YamlArray array = Sequence("a", "b", "c");

        array.RemoveAt(1);

        CollectionAssert.AreEqual(new[] { "a", "c" }, Values(array));
    }

    /// <summary>
    /// Verifies that clearing empties the sequence.
    /// </summary>
    [TestMethod]
    public void Clear_ShouldEmptyTheSequence()
    {
        YamlArray array = Sequence("a", "b");

        array.Clear();

        Assert.AreEqual(0, array.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.CopyTo(T[], int)" /> writes from the requested offset, leaving earlier
    /// slots untouched.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenOffsetIsNonZero_ShouldWriteFromThatIndex()
    {
        YamlArray array = Sequence("a", "b");
        YamlNode?[] target = new YamlNode?[3];

        array.CopyTo(target, 1);

        Assert.IsNull(target[0]);
        Assert.AreEqual("a", ((YamlValue)target[1]!).GetValue<string>());
        Assert.AreEqual("b", ((YamlValue)target[2]!).GetValue<string>());
    }

    /// <summary>
    /// Verifies that the non-generic enumerator yields the same elements as the generic one.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessedThroughNonGenericIEnumerable_ShouldYieldSameElements()
    {
        YamlArray array = Sequence("a", "b");

        var seen = new List<string>();
        foreach (object? item in (System.Collections.IEnumerable)array)
            seen.Add(((YamlValue)item!).GetValue<string>());

        CollectionAssert.AreEqual(new[] { "a", "b" }, seen);
    }

    /// <summary>
    /// Verifies that a sequence holding a <see langword="null" /> element writes it as the YAML null scalar rather
    /// than faulting, since a sequence may legitimately contain an empty entry.
    /// </summary>
    [TestMethod]
    public void ToYamlString_WhenSequenceContainsNull_ShouldWriteTheNullScalar()
    {
        var array = new YamlArray { YamlValue.Create("a"), null };

        Assert.Contains("null", array.ToYamlString());
    }

    /// <summary>
    /// Verifies that each scalar kind writes its own YAML form, so a value's declared kind survives a round trip
    /// through the writer rather than collapsing to a string.
    /// </summary>
    [TestMethod]
    public void ToYamlString_WhenScalarKindsVary_ShouldWriteEachInItsOwnForm()
    {
        string yaml = new YamlArray
        {
            YamlValue.Create("text"),
            YamlValue.Create(42L),
            YamlValue.Create(1.5d),
            YamlValue.Create(true),
            YamlValue.Create(false),
        }.ToYamlString();

        Assert.Contains("text", yaml);
        Assert.Contains("42", yaml);
        Assert.Contains("1.5", yaml);
        Assert.Contains("true", yaml);
        Assert.Contains("false", yaml);
    }

    /// <summary>
    /// Verifies that each factory records the matching value kind, which is what selects the write form above.
    /// </summary>
    [TestMethod]
    public void ValueKind_ShouldReflectTheFactoryUsed()
    {
        Assert.AreEqual(YamlValueKind.String, YamlValue.Create("a").ValueKind);
        Assert.AreEqual(YamlValueKind.Integer, YamlValue.Create(1L).ValueKind);
        Assert.AreEqual(YamlValueKind.Float, YamlValue.Create(1.5d).ValueKind);
        Assert.AreEqual(YamlValueKind.Boolean, YamlValue.Create(true).ValueKind);
    }

    /// <summary>
    /// Verifies that a scalar converts to its canonical string form regardless of the kind it was created as, using
    /// invariant formatting so the text does not vary with the machine's locale.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenRequestingString_ShouldUseInvariantCanonicalForm()
    {
        Assert.AreEqual("text", YamlValue.Create("text").GetValue<string>());
        Assert.AreEqual("42", YamlValue.Create(42L).GetValue<string>());
        Assert.AreEqual("1.5", YamlValue.Create(1.5d).GetValue<string>());
        Assert.AreEqual("true", YamlValue.Create(true).GetValue<string>());
        Assert.AreEqual("false", YamlValue.Create(false).GetValue<string>());
    }

    /// <summary>
    /// Verifies that a scalar widens to a compatible numeric type, so an integer scalar can be read as a
    /// <see cref="double" /> without the caller knowing how the document spelled it.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenRequestingACompatibleNumericType_ShouldConvert()
    {
        Assert.AreEqual(42d, YamlValue.Create(42L).GetValue<double>());
        Assert.AreEqual(42, YamlValue.Create(42L).GetValue<int>());
    }

    /// <summary>
    /// Verifies that an impossible conversion is reported as an invalid operation rather than leaking the underlying
    /// cast or format failure.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenConversionIsImpossible_ShouldThrowInvalidOperationException()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = YamlValue.Create("not a number").GetValue<int>();
        });

        Assert.IsNotNull(ex.InnerException);
    }
}
