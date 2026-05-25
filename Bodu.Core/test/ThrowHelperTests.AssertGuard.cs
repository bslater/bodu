// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.AssertGuard.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Delegates to <see cref="ExceptionAssert.AssertGuard(string, Action, Type?, string?)" /> so that the
    /// existing <see cref="ThrowHelper" /> contract test files continue to compile against the same local
    /// name after the helper was promoted to <c>Bodu.Test</c>.
    /// </summary>
    /// <param name="testName">A descriptive label used in assertion failure messages so a failing row is identifiable in <c>[DynamicData]</c> or <c>[DataRow]</c> output.</param>
    /// <param name="act">The guard invocation under test.</param>
    /// <param name="expectedExceptionType">The exact <see cref="Exception" />-derived type that the guard must throw, or <see langword="null" /> if the invocation must complete without throwing.</param>
    /// <param name="expectedParamName">When the expected exception derives from <see cref="ArgumentException" />, the value <see cref="ArgumentException.ParamName" /> must carry; otherwise ignored.</param>
    private static void AssertGuard(
        string testName,
        Action act,
        Type? expectedExceptionType,
        string? expectedParamName) =>
        ExceptionAssert.AssertGuard(testName, act, expectedExceptionType, expectedParamName);

}
