// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnfoldedTestData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Bodu.Test;

/// <summary>
/// Finds the data-driven test methods that ask MSTest to give each row its own test, over rows MSTest cannot serialize.
/// </summary>
/// <remarks>
/// <para>
/// MSTest gives each row its own test only when it can write the row's arguments with
/// <see cref="DataContractJsonSerializer" />. When it cannot, it folds the rows into one test without a warning, even
/// under <see cref="TestDataSourceUnfoldingStrategy.Unfold" />, and once a folded row times out it cancels every row
/// after it.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </para>
/// </remarks>
public static class UnfoldedTestData
{
    /// <summary>
    /// Returns a problem for each test method in an assembly that asks to unfold rows MSTest cannot serialize.
    /// </summary>
    /// <param name="assembly">The test assembly to read.</param>
    /// <returns>One message per such method, naming the method and the argument type MSTest cannot write.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// Reads the public, concrete <see cref="TestClassAttribute">test classes</see> MSTest discovers, with the methods
    /// they declare or inherit; a method several test classes inherit is reported once.
    /// </remarks>
    public static IReadOnlyList<string> FindRowsThatCannotUnfold(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return FindRowsThatCannotUnfold(
            assembly.GetTypes().Where(type => type.IsPublic && !type.IsAbstract && type.GetCustomAttribute<TestClassAttribute>() is not null));
    }

    /// <summary>
    /// Returns a problem for each method of the given types that asks to unfold rows MSTest cannot serialize.
    /// </summary>
    /// <param name="testClasses">The types whose public instance methods are read.</param>
    /// <returns>One message per such method, naming the method and the argument type MSTest cannot write.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="testClasses" /> is <see langword="null" />.</exception>
    public static IReadOnlyList<string> FindRowsThatCannotUnfold(IEnumerable<Type> testClasses)
    {
        ArgumentNullException.ThrowIfNull(testClasses);

        var problems = new SortedSet<string>(StringComparer.Ordinal);
        foreach (Type type in testClasses)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.GetCustomAttribute<TestMethodAttribute>()?.UnfoldingStrategy != TestDataSourceUnfoldingStrategy.Unfold)
                    continue;

                foreach (ITestDataSource source in method.GetCustomAttributes().OfType<ITestDataSource>())
                {
                    string? problem = FindArgumentThatCannotRoundTrip(source.GetData(method));
                    if (problem is not null)
                        problems.Add($"{method.DeclaringType!.Name.Split('`')[0]}.{method.Name}: {problem}");
                }
            }
        }

        return [.. problems];
    }

    /// <summary>
    /// Finds the first argument of any row that does not survive a round trip through
    /// <see cref="DataContractJsonSerializer" />.
    /// </summary>
    /// <param name="rows">The rows a data source supplies.</param>
    /// <returns>A description of the argument and the failure, or <see langword="null" /> when every argument survives.</returns>
    /// <remarks>
    /// An argument fails when the serializer throws, or when its type defines value equality and the copy read back is
    /// not equal to it, since MSTest would then run the test on the copy.
    /// </remarks>
    private static string? FindArgumentThatCannotRoundTrip(IEnumerable<object?[]> rows)
    {
        foreach (object?[] row in rows)
        {
            foreach (object? argument in row)
            {
                if (argument is null)
                    continue;

                try
                {
                    var serializer = new DataContractJsonSerializer(argument.GetType());
                    using var stream = new MemoryStream();
                    serializer.WriteObject(stream, argument);
                    stream.Position = 0;
                    object? copy = serializer.ReadObject(stream);
                    if (DefinesValueEquality(argument.GetType()) && !argument.Equals(copy))
                        return $"MSTest reads its {argument.GetType().Name} rows back changed, so it would run the test on different values";
                }
                catch (Exception exception) when (exception is InvalidDataContractException or SerializationException)
                {
                    return $"MSTest cannot serialize its {argument.GetType().Name} rows, so it folds them into one test ({exception.GetType().Name})";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a type compares its instances by value, so that a copy can be compared with its original.
    /// </summary>
    /// <param name="type">The argument's runtime type.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="type" /> is a value type or overrides
    /// <see cref="object.Equals(object)" />; <see langword="false" /> when it compares by reference.
    /// </returns>
    private static bool DefinesValueEquality(Type type) =>
        type.GetMethod(nameof(Equals), [typeof(object)])?.DeclaringType != typeof(object);
}
