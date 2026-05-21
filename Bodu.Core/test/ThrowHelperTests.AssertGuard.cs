// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.AssertGuard.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Asserts the standard <see cref="ThrowHelper" /> contract for a single guard invocation: the action either
    /// completes silently (when <paramref name="expectedExceptionType" /> is <see langword="null" />) or throws
    /// exactly the requested exception type, optionally with a specific <see cref="ArgumentException.ParamName" />.
    /// </summary>
    /// <param name="testName">A descriptive label used in assertion failure messages so a failing row is identifiable in DataRow output.</param>
    /// <param name="act">The guard invocation under test.</param>
    /// <param name="expectedExceptionType">
    /// The exact <see cref="Exception" />-derived type that the guard must throw, or <see langword="null" /> if
    /// the invocation must complete without throwing.
    /// </param>
    /// <param name="expectedParamName">
    /// When the expected exception derives from <see cref="ArgumentException" />, the value
    /// <see cref="ArgumentException.ParamName" /> must carry; otherwise ignored.
    /// </param>
    /// <remarks>
    /// <para>
    /// The helper deliberately accepts a base <see cref="Exception" /> type rather than a generic so that data
    /// rows can mix multiple expected exception types in a single matrix.
    /// </para>
    /// </remarks>
    private static void AssertGuard(
        string testName,
        Action act,
        Type? expectedExceptionType,
        string? expectedParamName)
    {
        if (expectedExceptionType is null)
        {
            act();
            return;
        }

        Exception? captured = null;
        try
        {
            act();
        }
        catch (Exception ex)
        {
            captured = ex;
        }

        Assert.IsNotNull(captured, $"{testName}: expected {expectedExceptionType.Name} but no exception was thrown.");
        Assert.AreEqual(expectedExceptionType, captured.GetType(), $"{testName}: wrong exception type.");

        if (expectedParamName is not null)
        {
            Assert.IsInstanceOfType<ArgumentException>(
                captured,
                $"{testName}: expected an ArgumentException-derived type to validate ParamName.");

            Assert.AreEqual(
                expectedParamName,
                ((ArgumentException)captured).ParamName,
                $"{testName}: wrong ParamName.");
        }
    }

}
