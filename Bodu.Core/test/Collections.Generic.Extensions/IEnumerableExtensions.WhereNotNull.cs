// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.WhereNotNull.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_WhereNotNull
{
    /// <summary>
    /// Verifies that <c>WhereNotNull</c> removes <see langword="null" /> references from a reference-type sequence.
    /// </summary>
    [TestMethod]
    public void WhereNotNull_WhenSourceIsReferenceType_ShouldRemoveNulls()
    {
        string?[] source = ["a", null, "b", null, "c"];

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, source.WhereNotNull().ToList());
    }

    /// <summary>
    /// Verifies that <c>WhereNotNull</c> removes <see langword="null" /> elements from a nullable value-type sequence
    /// and unwraps the remaining values.
    /// </summary>
    [TestMethod]
    public void WhereNotNull_WhenSourceIsNullableValueType_ShouldRemoveNullsAndUnwrap()
    {
        int?[] source = [1, null, 2, null, 3];

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, source.WhereNotNull().ToList());
    }

    /// <summary>
    /// Verifies that <c>WhereNotNull</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void WhereNotNull_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<string?> Source()
        {
            enumerated = true;
            yield return "a";
        }

        _ = Source().WhereNotNull();

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that the reference-type overload throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WhereNotNull_WhenReferenceSourceIsNull_ShouldThrowArgumentNullException()
    {
        IEnumerable<string?> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.WhereNotNull();
        });
    }

    /// <summary>
    /// Verifies that the nullable value-type overload throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WhereNotNull_WhenValueSourceIsNull_ShouldThrowArgumentNullException()
    {
        IEnumerable<int?> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.WhereNotNull();
        });
    }
}
