// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpanExtensionsTests.AsReadOnly.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class SpanExtensionsTests
{

    /// <summary>
    /// Verifies that <c>AsReadOnly</c> works for a reference-type element span without copying element references.
    /// </summary>
    [TestMethod]
    public void AsReadOnly_WhenCalled_ForReferenceTypeSpan_ShouldPreserveElementIdentity()
    {
        object a = new();
        object b = new();
        object[] buffer = [a, b];

        Span<object> source = buffer;
        ReadOnlySpan<object> result = source.AsReadOnly();

        Assert.AreSame(a, result[0]);
        Assert.AreSame(b, result[1]);
    }

    /// <summary>
    /// Verifies that <c>AsReadOnly</c> returns a view whose elements match the source span element-by-element.
    /// </summary>
    [TestMethod]
    public void AsReadOnly_WhenCalled_ShouldReturnReadOnlySpanWithSameElements()
    {
        int[] buffer = [10, 20, 30, 40];
        Span<int> source = buffer;

        ReadOnlySpan<int> result = source.AsReadOnly();

        CollectionAssert.AreEqual(buffer, result.ToArray());
    }
    /// <summary>
    /// Verifies that <c>AsReadOnly</c> returns a <see cref="ReadOnlySpan{T}"/> whose length matches the source span.
    /// </summary>
    [TestMethod]
    public void AsReadOnly_WhenCalled_ShouldReturnReadOnlySpanWithSameLength()
    {
        int[] buffer = [1, 2, 3, 4, 5];
        Span<int> source = buffer;

        ReadOnlySpan<int> result = source.AsReadOnly();

        Assert.AreEqual(source.Length, result.Length);
    }

    /// <summary>
    /// Verifies that calling <c>AsReadOnly</c> on an empty span returns a read-only span of length zero.
    /// </summary>
    [TestMethod]
    public void AsReadOnly_WhenSourceIsEmpty_ShouldReturnEmptyReadOnlySpan()
    {
        Span<int> source = [];

        ReadOnlySpan<int> result = source.AsReadOnly();

        Assert.AreEqual(0, result.Length);
    }

    /// <summary>
    /// Verifies that mutating the original backing array after <c>AsReadOnly</c> is observable through the returned view,
    /// confirming the result aliases the same memory.
    /// </summary>
    [TestMethod]
    public void AsReadOnly_WhenSourceIsMutated_ShouldReflectChangesInReadOnlyView()
    {
        int[] buffer = [1, 2, 3];
        Span<int> source = buffer;

        ReadOnlySpan<int> view = source.AsReadOnly();
        buffer[0] = 99;

        Assert.AreEqual(99, view[0]);
    }

}
