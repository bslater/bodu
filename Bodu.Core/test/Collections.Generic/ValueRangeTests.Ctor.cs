// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ValueRangeTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ValueRangeTests
{

    /// <summary>
    /// Verifies that <see cref="ValueRange{TKey, TValue}" /> default-constructed via <c>default</c> exposes
    /// default state, reflecting the readonly struct semantics.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultStruct_ShouldExposeDefaultState()
    {
        ValueRange<int, string> sut = default;

        Assert.AreEqual(0, sut.StartInclusive);
        Assert.AreEqual(0, sut.EndExclusive);
        Assert.IsNull(sut.Value);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> end endpoint is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEndIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ValueRange<string, int>("a", null!, 42);
        });
    }

    // --------------------------------------------------------
    // Constructor — happy path
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor stores the supplied endpoints and value verbatim.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, "low")]
    [DataRow(-10, 10, "mid")]
    [DataRow(int.MinValue, int.MaxValue, "wide")]
    public void Ctor_WhenEndpointsAndValueAreValid_ShouldStoreAll(int start, int end, string value)
    {
        var sut = new ValueRange<int, string>(start, end, value);

        Assert.AreEqual(start, sut.StartInclusive);
        Assert.AreEqual(end, sut.EndExclusive);
        Assert.AreEqual(value, sut.Value);
    }

    /// <summary>
    /// Verifies that endpoints with <c>start == end</c> are rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStartEqualsEnd_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new ValueRange<int, string>(5, 5, "x");
        });
    }

    /// <summary>
    /// Verifies that endpoints with <c>start &gt; end</c> are rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(10, 5)]
    [DataRow(1, 0)]
    [DataRow(int.MaxValue, int.MinValue)]
    public void Ctor_WhenStartIsGreaterThanEnd_ShouldThrowExactly(int start, int end)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new ValueRange<int, string>(start, end, "x");
        });
    }
    // --------------------------------------------------------
    // Constructor — argument validation
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a <see langword="null" /> start endpoint is rejected with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStartIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ValueRange<string, int>(null!, "z", 42);
        });
    }

    /// <summary>
    /// Verifies that the constructor accepts a <see langword="null" /> reference value (which is unrelated to
    /// endpoint validation).
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueIsNull_ShouldStoreNull()
    {
        var sut = new ValueRange<int, string?>(0, 10, null);

        Assert.IsNull(sut.Value);
    }

    /// <summary>
    /// Verifies that the constructor preserves a value-typed value.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueIsValueType_ShouldStoreValue()
    {
        var sut = new ValueRange<int, int>(0, 10, 42);

        Assert.AreEqual(42, sut.Value);
    }

}
