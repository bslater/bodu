// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.SetOperations.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{
    // --------------------------------------------------------
    // Union
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Union"/> throws <see cref="ArgumentNullException"/> when other is null.
    /// </summary>
    [TestMethod]
    public void Union_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        Multiset<int> sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Union(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Union"/> returns max count for each element.
    /// </summary>
    [TestMethod]
    public void Union_WhenCalled_ShouldReturnMaxCountForEachElement()
    {
        Multiset<string> a = new Multiset<string>(["x", "x", "y"]);
        Multiset<string> b = new Multiset<string>(["x", "y", "y", "z"]);

        Multiset<string> result = a.Union(b);

        Assert.AreEqual(2, result.CountOf("x"));
        Assert.AreEqual(2, result.CountOf("y"));
        Assert.AreEqual(1, result.CountOf("z"));
        Assert.AreEqual(5, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Union"/> does not mutate either operand.
    /// </summary>
    [TestMethod]
    public void Union_WhenCalled_ShouldNotMutateOperands()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 1, 2 });
        Multiset<int> b = new Multiset<int>(new[] { 2, 3 });

        _ = a.Union(b);

        Assert.AreEqual(3, a.Count);
        Assert.AreEqual(2, b.Count);
    }

    // --------------------------------------------------------
    // Intersect
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Intersect"/> throws <see cref="ArgumentNullException"/> when other is null.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        Multiset<int> sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Intersect(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Intersect"/> returns min count for each shared element.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenCalled_ShouldReturnMinCountForSharedElements()
    {
        Multiset<string> a = new Multiset<string>(["x", "x", "y"]);
        Multiset<string> b = new Multiset<string>(["x", "y", "y", "z"]);

        Multiset<string> result = a.Intersect(b);

        Assert.AreEqual(1, result.CountOf("x"));
        Assert.AreEqual(1, result.CountOf("y"));
        Assert.AreEqual(0, result.CountOf("z"));
        Assert.AreEqual(2, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Intersect"/> returns an empty multiset when no elements are shared.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenNoSharedElements_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 2 });
        Multiset<int> b = new Multiset<int>(new[] { 3, 4 });

        Multiset<int> result = a.Intersect(b);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, result.DistinctCount);
    }

    // --------------------------------------------------------
    // Except
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Except"/> throws <see cref="ArgumentNullException"/> when other is null.
    /// </summary>
    [TestMethod]
    public void Except_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        Multiset<int> sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Except(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Except"/> subtracts counts, flooring at zero.
    /// </summary>
    [TestMethod]
    public void Except_WhenCalled_ShouldSubtractCountsFlooringAtZero()
    {
        Multiset<string> a = new Multiset<string>(["x", "x", "x", "y", "y"]);
        Multiset<string> b = new Multiset<string>(["x", "x", "y", "y", "y", "z"]);

        Multiset<string> result = a.Except(b);

        Assert.AreEqual(1, result.CountOf("x"));
        Assert.AreEqual(0, result.CountOf("y"));
        Assert.AreEqual(0, result.CountOf("z"));
        Assert.AreEqual(1, result.Count);
    }

    // --------------------------------------------------------
    // Sum
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Sum"/> throws <see cref="ArgumentNullException"/> when other is null.
    /// </summary>
    [TestMethod]
    public void Sum_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        Multiset<int> sut = new Multiset<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Sum(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Sum"/> returns the combined count for each element.
    /// </summary>
    [TestMethod]
    public void Sum_WhenCalled_ShouldReturnSummedCountsForEachElement()
    {
        Multiset<string> a = new Multiset<string>(["x", "x", "y"]);
        Multiset<string> b = new Multiset<string>(["x", "y", "z"]);

        Multiset<string> result = a.Sum(b);

        Assert.AreEqual(3, result.CountOf("x"));
        Assert.AreEqual(2, result.CountOf("y"));
        Assert.AreEqual(1, result.CountOf("z"));
        Assert.AreEqual(6, result.Count);
    }

    /// <summary>
    /// Verifies that set-operation results do not share state with either operand.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SetOperations_WhenResultMutated_ShouldNotAffectOperands()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 2 });
        Multiset<int> b = new Multiset<int>(new[] { 2, 3 });

        Multiset<int> union = a.Union(b);
        Multiset<int> intersect = a.Intersect(b);
        Multiset<int> except = a.Except(b);
        Multiset<int> sum = a.Sum(b);

        union.Add(99);
        intersect.Add(99);
        except.Add(99);
        sum.Add(99);

        Assert.AreEqual(2, a.Count);
        Assert.AreEqual(2, b.Count);
    }

    // --------------------------------------------------------
    // Union — empty operands
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Union"/> returns an empty multiset when both operands are empty.
    /// </summary>
    [TestMethod]
    public void Union_WhenBothEmpty_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>();

        Multiset<int> result = a.Union(b);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, result.DistinctCount);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Union"/> returns a copy of the right operand when the left operand is empty.
    /// </summary>
    [TestMethod]
    public void Union_WhenLeftIsEmpty_ShouldReturnCopyOfRight()
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>(new[] { 1, 1, 2 });

        Multiset<int> result = a.Union(b);

        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(2, result.CountOf(1));
        Assert.AreEqual(1, result.CountOf(2));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Union"/> returns a copy of the left operand when the right operand is empty.
    /// </summary>
    [TestMethod]
    public void Union_WhenRightIsEmpty_ShouldReturnCopyOfLeft()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 1, 2 });
        Multiset<int> b = new Multiset<int>();

        Multiset<int> result = a.Union(b);

        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(2, result.CountOf(1));
        Assert.AreEqual(1, result.CountOf(2));
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Union"/> with itself returns a multiset with the same element counts.
    /// </summary>
    [TestMethod]
    public void Union_WithSelf_ShouldReturnSameCounts()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 1, 1, 2, 2 });

        Multiset<int> result = a.Union(a);

        Assert.AreEqual(3, result.CountOf(1));
        Assert.AreEqual(2, result.CountOf(2));
        Assert.AreEqual(5, result.Count);
    }

    // --------------------------------------------------------
    // Intersect — empty operands
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Intersect"/> returns an empty multiset when both operands are empty.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenBothEmpty_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>();

        Multiset<int> result = a.Intersect(b);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Intersect"/> returns an empty multiset when the left operand is empty.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenLeftIsEmpty_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>(new[] { 1, 2, 3 });

        Multiset<int> result = a.Intersect(b);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Intersect"/> returns an empty multiset when the right operand is empty.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenRightIsEmpty_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 2, 3 });
        Multiset<int> b = new Multiset<int>();

        Multiset<int> result = a.Intersect(b);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Intersect"/> with itself returns a multiset with the same element counts.
    /// </summary>
    [TestMethod]
    public void Intersect_WithSelf_ShouldReturnSameCounts()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 1, 1, 2, 2 });

        Multiset<int> result = a.Intersect(a);

        Assert.AreEqual(3, result.CountOf(1));
        Assert.AreEqual(2, result.CountOf(2));
        Assert.AreEqual(5, result.Count);
    }

    // --------------------------------------------------------
    // Except — empty operands
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Except"/> returns an empty multiset when both operands are empty.
    /// </summary>
    [TestMethod]
    public void Except_WhenBothEmpty_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>();

        Multiset<int> result = a.Except(b);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Except"/> returns an empty multiset when the left operand is empty.
    /// </summary>
    [TestMethod]
    public void Except_WhenLeftIsEmpty_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>(new[] { 1, 2, 3 });

        Multiset<int> result = a.Except(b);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Except"/> returns a copy of the left operand when the right operand is empty.
    /// </summary>
    [TestMethod]
    public void Except_WhenRightIsEmpty_ShouldReturnCopyOfLeft()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 1, 2 });
        Multiset<int> b = new Multiset<int>();

        Multiset<int> result = a.Except(b);

        Assert.AreEqual(2, result.CountOf(1));
        Assert.AreEqual(1, result.CountOf(2));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Except"/> with itself returns an empty multiset.
    /// </summary>
    [TestMethod]
    public void Except_WithSelf_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 1, 1, 2, 2 });

        Multiset<int> result = a.Except(a);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, result.DistinctCount);
    }

    // --------------------------------------------------------
    // Sum — empty operands
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Sum"/> returns an empty multiset when both operands are empty.
    /// </summary>
    [TestMethod]
    public void Sum_WhenBothEmpty_ShouldReturnEmpty()
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>();

        Multiset<int> result = a.Sum(b);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Sum"/> returns a copy of the right operand when the left operand is empty.
    /// </summary>
    [TestMethod]
    public void Sum_WhenLeftIsEmpty_ShouldReturnCopyOfRight()
    {
        Multiset<int> a = new Multiset<int>();
        Multiset<int> b = new Multiset<int>(new[] { 1, 1, 2 });

        Multiset<int> result = a.Sum(b);

        Assert.AreEqual(2, result.CountOf(1));
        Assert.AreEqual(1, result.CountOf(2));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Sum"/> returns a copy of the left operand when the right operand is empty.
    /// </summary>
    [TestMethod]
    public void Sum_WhenRightIsEmpty_ShouldReturnCopyOfLeft()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 1, 2 });
        Multiset<int> b = new Multiset<int>();

        Multiset<int> result = a.Sum(b);

        Assert.AreEqual(2, result.CountOf(1));
        Assert.AreEqual(1, result.CountOf(2));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Multiset{T}.Sum"/> with itself returns a multiset with doubled element counts.
    /// </summary>
    [TestMethod]
    public void Sum_WithSelf_ShouldReturnDoubledCounts()
    {
        Multiset<int> a = new Multiset<int>(new[] { 1, 1, 1, 2, 2 });

        Multiset<int> result = a.Sum(a);

        Assert.AreEqual(6, result.CountOf(1));
        Assert.AreEqual(4, result.CountOf(2));
        Assert.AreEqual(10, result.Count);
    }
}
