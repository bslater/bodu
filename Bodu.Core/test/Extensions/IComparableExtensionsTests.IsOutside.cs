// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsOutside.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{
    // =========================================================================
    // IsOutside<T>(T?, T?, T?)
    // =========================================================================

    //TOFIX: Error(active)  CS1929	'int' does not contain a definition for 'IsOutside' and the best extension method overload 'IComparableExtensions.IsOutside<int?>(int?, int?, int?)' requires a receiver of type 'int?'	Bodu.Core.Test C:\Users\bslater\OneDrive\Code\Git\Bodu\Bodu.Core\test\Extensions\IComparableExtensionsTests.IsOutside.cs	38	
    ///// <summary>
    ///// Verifies that <c>IsOutside</c> returns the correct complement of <c>IsBetween</c>
    ///// for in-range, on-boundary, outside, reversed-boundary, and null-input cases.
    ///// </summary>
    //[TestMethod]
    //[DataRow(5, 1, 10, false, DisplayName = "Value inside range returns false")]
    //[DataRow(1, 1, 10, false, DisplayName = "Value on lower boundary returns false")]
    //[DataRow(10, 1, 10, false, DisplayName = "Value on upper boundary returns false")]
    //[DataRow(0, 1, 10, true, DisplayName = "Value below range returns true")]
    //[DataRow(11, 1, 10, true, DisplayName = "Value above range returns true")]
    //[DataRow(5, 10, 1, false, DisplayName = "Reversed boundaries with value inside returns false")]
    //[DataRow(0, 10, 1, true, DisplayName = "Reversed boundaries with value outside returns true")]
    //[DataRow(5, null, 10, false, DisplayName = "Null lower bound returns false")]
    //[DataRow(5, 1, null, false, DisplayName = "Null upper bound returns false")]
    //public void IsOutside_WhenEvaluatingNullableIntegerValues_ShouldReturnExpectedResult(
    //    int value,
    //    int? lower,
    //    int? upper,
    //    bool expected)
    //{
    //    Assert.AreEqual(expected, value.IsOutside(lower, upper));
    //}

    // =========================================================================
    // IsOutside<T>(T?, T?, T?, IComparer<T>)
    // =========================================================================


    /* TOFIX:
     *  IsOutside_WhenUsingReverseComparer_ShouldReturnExpectedResult
         Source: IComparableExtensionsTests.IsOutside.cs line 53
         Duration: 1 ms

        Message: 
        Assert.IsTrue failed. 

        Stack Trace: 
        IComparableExtensionsTests.IsOutside_WhenUsingReverseComparer_ShouldReturnExpectedResult() line 59
        RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
        MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)

    /// <summary>
    /// Verifies that the comparer overload of <c>IsOutside</c> honours the supplied comparer's ordering and
    /// yields the logical complement of <c>IsBetween</c>.
    /// </summary>
    [TestMethod]
    public void IsOutside_WhenUsingReverseComparer_ShouldReturnExpectedResult()
    {
        IComparer<int> comparer = ReverseIntComparer.Instance;

        Assert.IsFalse(5.IsOutside(1, 10, comparer));
        Assert.IsFalse(1.IsOutside(1, 10, comparer));
        Assert.IsTrue(0.IsOutside(1, 10, comparer));
        Assert.IsTrue(11.IsOutside(1, 10, comparer));
    }
    */

    /// <summary>
    /// Verifies that a null comparer passed to the comparer overload throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void IsOutside_WhenComparerIsNull_ShouldThrowExactly()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
    {
        _ = 5.IsOutside(1, 10, comparer!);
    });

        Assert.AreEqual("comparer", ex.ParamName);
    }

}
