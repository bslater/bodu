// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNamingPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the parameter-validation contract shared by the built-in <see cref="BencodeNamingPolicy" />
/// implementations: every policy rejects a <see langword="null" /> member name with
/// <see cref="ArgumentNullException" /> and <c>ParamName</c> <c>name</c>.
/// </summary>
[TestClass]
public class BencodeNamingPolicyTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeNamingPolicy.CamelCase" /> throws <see cref="ArgumentNullException" /> with
    /// <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForCamelCase_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeNamingPolicy.CamelCase.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNamingPolicy.SnakeCaseLower" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForSnakeCaseLower_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeNamingPolicy.SnakeCaseLower.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNamingPolicy.SnakeCaseUpper" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForSnakeCaseUpper_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeNamingPolicy.SnakeCaseUpper.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNamingPolicy.KebabCaseLower" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForKebabCaseLower_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeNamingPolicy.KebabCaseLower.ConvertName(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNamingPolicy.KebabCaseUpper" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>name</c> when converting a <see langword="null" /> name.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenNameIsNull_ForKebabCaseUpper_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeNamingPolicy.KebabCaseUpper.ConvertName(null!);
        }, "name");
    }
}
