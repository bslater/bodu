// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StructuralAssertions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu;

public static class StructuralAssertions
{

    /// <summary>
    /// Verifies that all specified instance fields in the target type are declared readonly.
    /// </summary>
    /// <param name="targetType">The type to inspect.</param>
    /// <param name="fieldNames">The names of the fields to verify.</param>
    public static void AssertFieldsAreReadOnly(Type targetType, params string[] fieldNames)
    {
        FieldInfo[] allFields = targetType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var missingFields = new List<string>();
        var mutableFields = new List<string>();

        foreach (string name in fieldNames)
        {
            FieldInfo? field = allFields.FirstOrDefault(f => f.Name == name);
            if (field == null)
            {
                missingFields.Add(name);
            }
            else if (!field.IsInitOnly)
            {
                mutableFields.Add(name);
            }
        }

        if (missingFields.Any())
        {
            Assert.Fail($"Missing expected field(s) on {targetType.Name}: {string.Join(", ", missingFields)}");
        }

        if (mutableFields.Any())
        {
            Assert.Fail($"Expected readonly field(s) but found mutable: {string.Join(", ", mutableFields)}");
        }
    }

}
