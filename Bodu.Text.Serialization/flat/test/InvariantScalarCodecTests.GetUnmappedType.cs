// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodecTests.GetUnmappedType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class InvariantScalarCodecTests
{
    /// <summary>
    /// Verifies that a value of a scalar declared type maps, whatever the value, <see langword="null" /> included.
    /// </summary>
    [TestMethod]
    public void GetUnmappedType_WhenTheDeclaredTypeIsAScalar_ShouldReturnNull()
    {
        Assert.IsNull(InvariantScalarCodec.GetUnmappedType(5, typeof(int), FlatScalarKind.ConvertibleNumber));
        Assert.IsNull(InvariantScalarCodec.GetUnmappedType(null, typeof(int?), InvariantScalarCodec.Classify(typeof(int?))));
        Assert.IsNull(InvariantScalarCodec.GetUnmappedType(null, typeof(string), FlatScalarKind.String));
    }

    /// <summary>
    /// Verifies that a declared type that is neither a scalar nor <see cref="object" /> is the unmapped type, whatever
    /// the value, <see langword="null" /> included.
    /// </summary>
    [TestMethod]
    public void GetUnmappedType_WhenTheDeclaredTypeIsNotAScalar_ShouldReturnTheDeclaredType()
    {
        Type[] declaredTypes = [typeof(Dictionary<string, int>), typeof(List<int>), typeof(Version), typeof(IComparable)];

        foreach (Type declaredType in declaredTypes)
        {
            Assert.AreEqual(declaredType, InvariantScalarCodec.GetUnmappedType(null, declaredType, FlatScalarKind.None), declaredType.Name);
        }

        Assert.AreEqual(typeof(IComparable), InvariantScalarCodec.GetUnmappedType(5, typeof(IComparable), FlatScalarKind.None));
    }

    /// <summary>
    /// Verifies that a value declared as <see cref="object" /> maps when it is <see langword="null" /> or a scalar, an
    /// enumeration included, which is written by its own type.
    /// </summary>
    [TestMethod]
    public void GetUnmappedType_WhenTheDeclaredTypeIsObjectAndTheValueIsAScalarOrNull_ShouldReturnNull()
    {
        object?[] values = [null, 5, "text", DayOfWeek.Monday, new DateOnly(2026, 1, 2), new Uri("https://example.com/")];

        foreach (object? value in values)
        {
            Assert.IsNull(InvariantScalarCodec.GetUnmappedType(value, typeof(object), FlatScalarKind.None), value?.GetType().Name ?? "null");
        }
    }

    /// <summary>
    /// Verifies that a value declared as <see cref="object" /> that is not a scalar has its own type as the unmapped
    /// type.
    /// </summary>
    [TestMethod]
    public void GetUnmappedType_WhenTheDeclaredTypeIsObjectAndTheValueIsNotAScalar_ShouldReturnTheValuesType()
    {
        object[] values = [new List<int> { 1 }, new Dictionary<string, string>(), new Version(1, 2)];

        foreach (object value in values)
        {
            Assert.AreEqual(value.GetType(), InvariantScalarCodec.GetUnmappedType(value, typeof(object), FlatScalarKind.None), value.GetType().Name);
        }
    }
}
