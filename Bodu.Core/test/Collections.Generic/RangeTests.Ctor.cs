// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeTests
{

    /// <summary>
    /// Verifies that <see cref="Range{T}" /> default-constructed via <c>default</c> exposes default endpoints,
    /// reflecting the readonly struct semantics.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultStruct_ShouldExposeDefaultEndpoints()
    {
        Range<int> sut = default;

        Assert.AreEqual(0, sut.StartInclusive);
        Assert.AreEqual(0, sut.EndExclusive);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> end endpoint is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEndIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Range<string>("a", null!);
        });
    }

    /// <summary>
    /// Verifies that the constructor accepts reference-type comparable endpoints.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEndpointsAreStrings_ShouldStoreEndpoints()
    {
        var sut = new Range<string>("alpha", "omega");

        Assert.AreEqual("alpha", sut.StartInclusive);
        Assert.AreEqual("omega", sut.EndExclusive);
    }

    // --------------------------------------------------------
    // Constructor — happy path
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor stores the supplied endpoints verbatim.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(-10, 10)]
    [DataRow(int.MinValue, int.MaxValue)]
    [DataRow(5, 6)]
    public void Ctor_WhenEndpointsAreValid_ShouldStoreEndpoints(int start, int end)
    {
        var sut = new Range<int>(start, end);

        Assert.AreEqual(start, sut.StartInclusive);
        Assert.AreEqual(end, sut.EndExclusive);
    }

    /// <summary>
    /// Verifies that endpoints with <c>start == end</c> are rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStartEqualsEnd_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Range<int>(5, 5);
        });
    }

    /// <summary>
    /// Verifies that endpoints with <c>start &gt; end</c> are rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(10, 5)]
    [DataRow(1, 0)]
    [DataRow(int.MaxValue, int.MinValue)]
    public void Ctor_WhenStartIsGreaterThanEnd_ShouldThrowArgumentException(int start, int end)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Range<int>(start, end);
        });
    }
    // --------------------------------------------------------
    // Constructor — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a <see langword="null" /> start endpoint is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStartIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Range<string>(null!, "z");
        });
    }

}
