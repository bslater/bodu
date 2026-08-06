// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderTests.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Builders;

/// <summary>
/// Verifies <see cref="CompoundStorageBuilder" /> through its dictionary interfaces.
/// </summary>
/// <remarks>
/// The builder is declared as an <see cref="IDictionary{TKey, TValue}" /> of child name to node, and several of its
/// members are reachable only through that interface — the explicit
/// <see cref="ICollection{T}" /> implementations in particular. The convenience API (<c>AddStorage</c>,
/// <c>TryGetStream</c>, and the rest) delegates to the same child dictionary, so these tests assert that the two
/// views agree: a child added through one is visible through the other, and the single-parent rule holds either way.
/// </remarks>
public partial class CompoundStorageBuilderTests
{
    /// <summary>
    /// Gets a storage populated through the convenience API, viewed as a dictionary.
    /// </summary>
    /// <param name="root">Receives the underlying storage.</param>
    /// <returns>The dictionary view of <paramref name="root" />.</returns>
    private static IDictionary<string, CompoundEntryBuilder> CreatePopulated(out CompoundStorageBuilder root)
    {
        root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStorage("Storage 1");
        _ = root.AddStream("Stream 1", new byte[] { 1, 2, 3 });

        return root;
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorageBuilder.ContainsKey(string)" /> reports children added through the
    /// convenience API, and applies the same case-insensitive comparison as the rest of the model.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenChildAddedThroughConvenienceApi_ShouldReportPresent()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Stream 1", new byte[] { 1 });

        Assert.IsTrue(root.ContainsKey("Stream 1"));
        Assert.IsTrue(root.ContainsKey("STREAM 1"));
        Assert.IsFalse(root.ContainsKey("Stream 2"));
    }

    /// <summary>
    /// Verifies that enumerating the storage yields one key/value pair per child, keyed by the child's name.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenStorageHasChildren_ShouldYieldEachChildKeyedByName()
    {
        IDictionary<string, CompoundEntryBuilder> view = CreatePopulated(out CompoundStorageBuilder root);

        var pairs = view.ToList();

        Assert.HasCount(2, pairs);
        foreach (KeyValuePair<string, CompoundEntryBuilder> pair in pairs)
            Assert.AreEqual(pair.Key, pair.Value.Name);

        CollectionAssert.AreEquivalent(new[] { "Storage 1", "Stream 1" }, pairs.Select(p => p.Key).ToList());
        Assert.AreEqual(2, root.Count);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.IEnumerable" /> implementation yields the same
    /// children as the generic one.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessedThroughNonGenericIEnumerable_ShouldYieldSameChildren()
    {
        IDictionary<string, CompoundEntryBuilder> view = CreatePopulated(out _);

        var names = new List<string>();
        foreach (object? item in (System.Collections.IEnumerable)view)
            names.Add(((KeyValuePair<string, CompoundEntryBuilder>)item!).Key);

        CollectionAssert.AreEquivalent(new[] { "Storage 1", "Stream 1" }, names);
    }

    /// <summary>
    /// Verifies that adding through <see cref="ICollection{T}.Add(T)" /> parents the node, exactly as the
    /// <see cref="CompoundStorageBuilder.Add(string, CompoundEntryBuilder)" /> overload does.
    /// </summary>
    [TestMethod]
    public void Add_WhenCalledThroughICollectionOfKeyValuePair_ShouldParentTheNode()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        ICollection<KeyValuePair<string, CompoundEntryBuilder>> collection = root;
        var child = new CompoundStorageBuilder();

        collection.Add(new KeyValuePair<string, CompoundEntryBuilder>("Storage 1", child));

        Assert.AreEqual(1, root.Count);
        Assert.AreSame(root, child.Parent);
        Assert.AreEqual("Storage 1", child.Name);
    }

    /// <summary>
    /// Verifies that the collection reports itself as mutable, so a caller that dispatches on
    /// <see cref="ICollection{T}.IsReadOnly" /> does not skip the mutating members.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_ShouldBeFalse()
    {
        ICollection<KeyValuePair<string, CompoundEntryBuilder>> collection = CompoundStorageBuilder.CreateRoot();

        Assert.IsFalse(collection.IsReadOnly);
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Contains(T)" /> matches on both the name and the node instance, so a
    /// pair naming a different node is not reported as present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenPairNamesADifferentNode_ShouldReturnFalse()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Stream 1", new byte[] { 1 });
        ICollection<KeyValuePair<string, CompoundEntryBuilder>> collection = root;

        Assert.IsTrue(collection.Contains(new KeyValuePair<string, CompoundEntryBuilder>("Stream 1", stream)));
        Assert.IsFalse(collection.Contains(
            new KeyValuePair<string, CompoundEntryBuilder>("Stream 1", new CompoundStorageBuilder())));
        Assert.IsFalse(collection.Contains(new KeyValuePair<string, CompoundEntryBuilder>("Stream 2", stream)));
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.CopyTo(T[], int)" /> writes every child from the requested offset,
    /// leaving earlier slots untouched.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenOffsetIsNonZero_ShouldWriteFromThatIndex()
    {
        ICollection<KeyValuePair<string, CompoundEntryBuilder>> collection = CreatePopulated(out _);
        var target = new KeyValuePair<string, CompoundEntryBuilder>[3];

        collection.CopyTo(target, 1);

        Assert.IsNull(target[0].Key);
        CollectionAssert.AreEquivalent(
            new[] { "Storage 1", "Stream 1" },
            new[] { target[1].Key, target[2].Key });
    }

    /// <summary>
    /// Verifies that <see cref="ICollection{T}.Remove(T)" /> removes the child and detaches it, and that a pair
    /// naming a different node leaves the storage untouched.
    /// </summary>
    [TestMethod]
    public void Remove_WhenCalledThroughICollectionOfKeyValuePair_ShouldRemoveOnlyTheMatchingPair()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Stream 1", new byte[] { 1 });
        ICollection<KeyValuePair<string, CompoundEntryBuilder>> collection = root;

        Assert.IsFalse(collection.Remove(
            new KeyValuePair<string, CompoundEntryBuilder>("Stream 1", new CompoundStorageBuilder())));
        Assert.AreEqual(1, root.Count);

        Assert.IsTrue(collection.Remove(new KeyValuePair<string, CompoundEntryBuilder>("Stream 1", stream)));
        Assert.AreEqual(0, root.Count);
        Assert.IsNull(stream.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorageBuilder.Remove(string)" /> reports <see langword="false" /> for a name
    /// that is not present, rather than throwing.
    /// </summary>
    [TestMethod]
    public void Remove_WhenNameIsAbsent_ShouldReturnFalse()
    {
        var root = CompoundStorageBuilder.CreateRoot();

        Assert.IsFalse(root.Remove("Stream 1"));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorageBuilder.TryGetStorage(string, out CompoundStorageBuilder)" /> reports
    /// <see langword="false" /> for an absent name and for a name that resolves to a stream, so a caller cannot
    /// mistake one entry kind for the other.
    /// </summary>
    [TestMethod]
    public void TryGetStorage_WhenNameIsAbsentOrNamesAStream_ShouldReturnFalse()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Stream 1", new byte[] { 1 });

        Assert.IsFalse(root.TryGetStorage("Storage 1", out CompoundStorageBuilder? missing));
        Assert.IsNull(missing);

        Assert.IsFalse(root.TryGetStorage("Stream 1", out CompoundStorageBuilder? mistyped));
        Assert.IsNull(mistyped);
    }

    /// <summary>
    /// Verifies that renaming a child that does not exist throws
    /// <see cref="CompoundFileSerializationException" /> rather than silently succeeding.
    /// </summary>
    [TestMethod]
    public void Rename_WhenOldNameIsAbsent_ShouldThrowExactly()
    {
        var root = CompoundStorageBuilder.CreateRoot();

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            root.Rename("Stream 1", "Stream 2");
        });
    }

    /// <summary>
    /// Verifies that renaming onto an existing sibling's name throws, because the serialized container could not
    /// carry both.
    /// </summary>
    [TestMethod]
    public void Rename_WhenNewNameIsAlreadyPresent_ShouldThrowExactly()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Stream 1", new byte[] { 1 });
        _ = root.AddStream("Stream 2", new byte[] { 2 });

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            root.Rename("Stream 1", "Stream 2");
        });
    }

    /// <summary>
    /// Verifies that renaming a child to a case-variant of its own name succeeds. The duplicate check must exempt the
    /// entry being renamed, or a case-only rename would collide with itself under the default comparison.
    /// </summary>
    [TestMethod]
    public void Rename_WhenNewNameIsACaseVariantOfTheOldName_ShouldSucceed()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Stream 1", new byte[] { 1 });

        root.Rename("Stream 1", "STREAM 1");

        Assert.AreEqual(1, root.Count);
        Assert.AreEqual("STREAM 1", stream.Name);
        Assert.IsTrue(root.TryGetStream("STREAM 1", out _));
    }

    /// <summary>
    /// Verifies that a root created with case-sensitive comparison treats names differing only in case as distinct
    /// children, where the default comparison would reject the second as a duplicate.
    /// </summary>
    [TestMethod]
    public void CreateRoot_WhenNameComparisonIsCaseSensitive_ShouldTreatCaseVariantsAsDistinct()
    {
        var root = CompoundStorageBuilder.CreateRoot(new CompoundStorageBuilderOptions
        {
            NameComparisonCaseSensitive = true,
        });

        _ = root.AddStream("a", new byte[] { 1 });
        _ = root.AddStream("A", new byte[] { 2 });

        Assert.AreEqual(2, root.Count);
        Assert.IsTrue(root.ContainsKey("a"));
        Assert.IsTrue(root.ContainsKey("A"));
    }

    /// <summary>
    /// Verifies that the default comparison rejects a case-variant sibling, pinning the contrast with the
    /// case-sensitive option above.
    /// </summary>
    [TestMethod]
    public void CreateRoot_WhenNameComparisonIsDefault_ShouldRejectCaseVariantAsDuplicate()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("a", new byte[] { 1 });

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            _ = root.AddStream("A", new byte[] { 2 });
        });
    }

    /// <summary>
    /// Verifies that the case-sensitivity option propagates to child storages, so a nested storage keys its own
    /// children the same way as the root it was created under.
    /// </summary>
    [TestMethod]
    public void AddStorage_WhenRootIsCaseSensitive_ShouldPropagateComparisonToChildStorage()
    {
        var root = CompoundStorageBuilder.CreateRoot(new CompoundStorageBuilderOptions
        {
            NameComparisonCaseSensitive = true,
        });

        CompoundStorageBuilder child = root.AddStorage("Storage 1");
        _ = child.AddStream("a", new byte[] { 1 });
        _ = child.AddStream("A", new byte[] { 2 });

        Assert.AreEqual(2, child.Count);
    }
}
